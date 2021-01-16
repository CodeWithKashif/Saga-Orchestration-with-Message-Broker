using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Serilog;
using WorkshopManagementAPI.Common;
using WorkshopManagementAPI.Infrastructure.Messaging;
using WorkshopManagementAPI.Models;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace WorkshopManagementAPI.Services
{
    /// <summary>
    /// Running Background service by using BackgroundService class
    /// </summary>
    public class MessageHandlerBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly ILogger<MessageHandlerBackgroundService> _logger;
        private readonly IWorkshopPlanningService _workshopPlanningService;
        private readonly IMessagePublisher _messagePublisher;

        public MessageHandlerBackgroundService(
            ILogger<MessageHandlerBackgroundService> logger, 
            IWorkshopPlanningService workshopPlanningService, 
            IMessagePublisher messagePublisher)
        {
            _logger = logger;
            _workshopPlanningService = workshopPlanningService;
            _messagePublisher = messagePublisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            IConnection connection = _messagePublisher.GetIConnectionForDispatchConsumer();
            IModel channel = connection.CreateModel();

            channel.ExchangeDeclare("SAGA-GMS-fanout-exchange", ExchangeType.Fanout);
            channel.QueueDeclare(queue: "SAGA-GMS-WorkshopManagementAPI-fanout-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            
            channel.QueueBind(exchange: "SAGA-GMS-fanout-exchange", queue: "SAGA-GMS-WorkshopManagementAPI-fanout-queue", routingKey: String.Empty);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (bc, ea) =>
            {
                string message = Encoding.UTF8.GetString(ea.Body.ToArray());
                string messageType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"]);
                _logger.LogInformation($"message received - messageType:{messageType} - messageBody {message}");
                try
                {
                    await HandleEvent(ea);
                    await Task.Delay(3 * 1000, stoppingToken);
                    //channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (JsonException)
                {
                    _logger.LogError($"JSON Parse Error: '{message}'.");
                    channel.BasicNack(ea.DeliveryTag, false, false);
                }
                catch (AlreadyClosedException)
                {
                    _logger.LogInformation("RabbitMQ is closed!");
                }
                catch (Exception e)
                {
                    _logger.LogError(default, e, e.Message);
                }
            };

            channel.BasicConsume(queue: "SAGA-GMS-WorkshopManagementAPI-fanout-queue", autoAck: true, consumer: consumer);

            await Task.CompletedTask;
        }

        private Task<bool> HandleEvent(BasicDeliverEventArgs ea)
        {
            string messageType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"]);
            string body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var responseMessageType = EnumUtil.ParseEnum<ServiceResponseMessageType>(messageType);

            return HandleMessageAsync(responseMessageType, body);
        }
        public async Task<bool> HandleMessageAsync(ServiceResponseMessageType messageType, string message)
        {
            try
            {
                switch (messageType)
                {
                    case ServiceResponseMessageType.PlanMaintenanceJob:
                        await HandleAsync(JsonSerializer.Deserialize<PlanMaintenanceJob>(message));
                        break;
                    case ServiceResponseMessageType.UndoPlanMaintenanceJob:
                        await HandleAsync(message);
                        break;
                }
            }
            catch(Exception ex)
            {
                //string messageId = messageObject.Property("MessageId") != null ? messageObject.Property("MessageId").Value<string>() : "[unknown]";
                //Log.Error(ex, "Error while handling {MessageType} message with id {MessageId}.", messageType, messageId);
            }

            // always acknowledge message - any errors need to be dealt with locally.
            return true; 
        }

        private async Task<bool> HandleAsync(PlanMaintenanceJob input)
        {
            bool result = false; 
            
            Log.Information($@"PlanMaintenanceJob: {input.JobId}, {input.LicenseNumber}, {input.OwnerId}, GenerateDemoError: {input.GenerateDemoError}");

            try
            {
                var planMaintenanceJob = new PlanMaintenanceJob
                {
                    JobId = input.JobId,
                    OwnerId=input.OwnerId,
                    LicenseNumber = input.LicenseNumber,
                    PlanningDate= input.PlanningDate,
                    StartTime = input.StartTime,
                    EndTime = input.EndTime,
                    Notes = input.Notes,
                    GenerateDemoError = input.GenerateDemoError,
                };

                result = await _workshopPlanningService.RegisterAsync(planMaintenanceJob);
                _messagePublisher.PublishToFanoutExchange(
                    result
                        ? PublishExternalMessageType.PlanMaintenanceJobSucceed
                        : PublishExternalMessageType.PlanMaintenanceJobFailed, input.JobId);
            }
            catch (Exception)
            {
                Log.Warning($"Skipped planning a new maintenance job jobID {input.JobId}.");
            }
            return result;
        }

        private async Task<bool> HandleAsync(string message)
        {
            message = JsonConvert.DeserializeObject(message)?.ToString();
            Guid jobId = new Guid(message);
            bool result = false;
            Log.Information($"UndoPlanMaintenanceJob JobId: {jobId} ");
            try
            {
                result = await _workshopPlanningService.UndoPlanMaintenanceJobAsync(jobId);
                _messagePublisher
                    .PublishToFanoutExchange(
                        result
                            ? PublishExternalMessageType.UndoPlanMaintenanceJobSucceed
                            : PublishExternalMessageType.UndoPlanMaintenanceJobFailed, jobId);

            }
            catch (Exception)
            {
                Log.Warning($"UndoPlanMaintenanceJob failed for {jobId}");
            }
            return result;
        }
    }
}