using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CustomerManagementAPI.Common;
using CustomerManagementAPI.Infrastructure.Messaging;
using CustomerManagementAPI.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Serilog;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CustomerManagementAPI.Services
{
    /// <summary>
    /// Running Background service by using BackgroundService class
    /// </summary>
    public class MessageHandlerBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly ILogger<MessageHandlerBackgroundService> _logger;
        private readonly ICustomerService _customerService;
        private readonly IMessagePublisher _messagePublisher;

        public MessageHandlerBackgroundService(
            ILogger<MessageHandlerBackgroundService> logger, 
            ICustomerService customerService, 
            IMessagePublisher messagePublisher)
        {
            _logger = logger;
            _customerService = customerService;
            _messagePublisher = messagePublisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            IConnection connection = _messagePublisher.GetIConnectionForDispatchConsumer();
            IModel channel = connection.CreateModel();

            channel.ExchangeDeclare("SAGA-GMS-fanout-exchange", ExchangeType.Fanout);
            channel.QueueDeclare(queue: "SAGA-GMS-CustomerManagementAPI-fanout-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            
            channel.QueueBind(exchange: "SAGA-GMS-fanout-exchange", queue: "SAGA-GMS-CustomerManagementAPI-fanout-queue", routingKey: String.Empty);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (bc, ea) =>
            {
                string message = Encoding.UTF8.GetString(ea.Body.ToArray());
                string messageType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"]);
                _logger.LogInformation($"message received - messageType:{messageType} - messageBody {message}");
                try
                {
                    await HandleEvent(ea);
                    await Task.Delay(1 * 1000, stoppingToken);
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

            channel.BasicConsume(queue: "SAGA-GMS-CustomerManagementAPI-fanout-queue", autoAck: true, consumer: consumer);

            await Task.CompletedTask;
        }

        private Task<bool> HandleEvent(BasicDeliverEventArgs ea)
        {
            string messageType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"]);
            string body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var serviceResponseMessageType = EnumUtil.ParseEnum<ServiceResponseMessageType>(messageType);
            return HandleMessageAsync(serviceResponseMessageType, body);
        }
        public async Task<bool> HandleMessageAsync(ServiceResponseMessageType messageType, string message)
        {
            try
            {
                switch (messageType)
                {
                    case ServiceResponseMessageType.RegisterCustomer:
                        await HandleAsync(JsonSerializer.Deserialize<Customer>(message));
                        break;
                    case ServiceResponseMessageType.UndoRegisterCustomer:
                        await HandleAsync(emailAddress: message);
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

        private async Task<bool> HandleAsync(Customer inputCustomer)
        {
            bool result = false; 
            
            Log.Information("Register Customer: {EmailAddress}, {Name}, {TelephoneNumber}",
                inputCustomer.EmailAddress, inputCustomer.Name, inputCustomer.TelephoneNumber);

            try
            {
                var customer = new Customer
                {
                    EmailAddress = inputCustomer.EmailAddress,
                    Name = inputCustomer.Name,
                    TelephoneNumber = inputCustomer.TelephoneNumber
                };

                result = await _customerService.RegisterAsync(customer);
                
                _messagePublisher
                    .PublishToFanoutExchange(
                        result
                            ? PublishExternalMessageType.RegisterCustomerSucceed
                            : PublishExternalMessageType.RegisterCustomerFailed, customer.EmailAddress);
            }
            catch (Exception)
            {
                Log.Warning("Skipped adding customer with customer id {EmailAddress}.", inputCustomer.EmailAddress);
            }
            return result;
        }

        private async Task<bool> HandleAsync(string emailAddress)
        {
            bool result = false; 

            emailAddress = JsonConvert.DeserializeObject(emailAddress)?.ToString();
            Log.Information($"UndoRegister Customer EmailAddress : {emailAddress} ");

            try
            {
                result = await _customerService.UndoRegisterAsync(emailAddress);
                
                _messagePublisher
                    .PublishToFanoutExchange(
                        result
                            ? PublishExternalMessageType.UndoRegisterCustomerSucceed
                            : PublishExternalMessageType.UndoRegisterCustomerFailed,
                        emailAddress);
            }
            catch (Exception)
            {
                Log.Warning($"UndoRegisterAsync failed for {emailAddress}");
            }
            return result;
        }

    }
}