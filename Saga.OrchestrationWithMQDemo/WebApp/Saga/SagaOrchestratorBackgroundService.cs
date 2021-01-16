using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Serilog;
using WebApp.Common;
using WebApp.Infrastructure.Messaging;
using WebApp.Models;
using WebApp.ViewModels;
using JsonException = System.Text.Json.JsonException;

namespace WebApp.Saga
{
    /// <summary>
    /// Running Background service by using BackgroundService class
    /// </summary>
    public class SagaOrchestratorBackgroundService : Microsoft.Extensions.Hosting.BackgroundService, ISagaOrchestratorBackgroundService
    {
        private readonly ILogger<SagaOrchestratorBackgroundService> _logger;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ISagaMemoryStorage _sagaMemoryStorage;

        public SagaOrchestratorBackgroundService(
            ILogger<SagaOrchestratorBackgroundService> logger,
            IMessagePublisher messagePublisher,
            ISagaMemoryStorage memorySagaStorage)
        {
            _logger = logger;
            _messagePublisher = messagePublisher;
           _sagaMemoryStorage = memorySagaStorage;
        }

        
        public async Task StartProcessing(WorkShopManagementNewVM inputModel)
        {
            Guid jobId = Guid.NewGuid();
            
            //First add in memory for saga stat tracking purpose
            _sagaMemoryStorage.Add(new RegisterAndPlanJobSagaModel
            {
                EmailAddress = inputModel.Customer.EmailAddress,
                LicenseNumber = inputModel.Vehicle.LicenseNumber,
                JobId = jobId.ToString(),
                SagaStartTimeStamp = DateTime.Now,
            });

            //Here pushing message to all three micro-services
            var customer = Mapper.Map<Customer>(inputModel.Customer);
            _messagePublisher.PublishToFanoutExchange(PublishExternalMessageType.RegisterCustomer, customer);

            if (!await RegisterCustomerSuccess(inputModel.Customer.EmailAddress)) 
                return;
            
            var vehicle = Mapper.Map<Vehicle>(inputModel.Vehicle);
            vehicle.OwnerId = customer.EmailAddress;
            _messagePublisher.PublishToFanoutExchange(PublishExternalMessageType.RegisterVehicle, vehicle);

            if (!await RegisterVehicleSuccess(inputModel.Vehicle.LicenseNumber)) 
                return;

            var maintenanceJob = Mapper.Map<MaintenanceJob>(inputModel.MaintenanceJob);
            maintenanceJob.JobId = jobId;
            maintenanceJob.OwnerId = customer.EmailAddress;
            maintenanceJob.LicenseNumber = vehicle.LicenseNumber;
            maintenanceJob.PlanningDate = DateTime.Now;
            _messagePublisher.PublishToFanoutExchange(PublishExternalMessageType.PlanMaintenanceJob, maintenanceJob);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            IConnection connection = _messagePublisher.GetIConnectionForDispatchConsumer();
            IModel channel = connection.CreateModel();

            //TODO:Change fanout type exchange
            channel.ExchangeDeclare("SAGA-GMS-fanout-exchange", ExchangeType.Fanout);
            channel.QueueDeclare(queue: "SAGA-GMS-AllAPI-fanout-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            channel.QueueBind(exchange: "SAGA-GMS-fanout-exchange", queue: "SAGA-GMS-AllAPI-fanout-queue", routingKey: String.Empty);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (bc, ea) =>
            {
                string message = Encoding.UTF8.GetString(ea.Body.ToArray());
                string messageType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"]);
                
                //TODO: Will be removed once exchange type fanout will be changed
                if (messageType == PublishExternalMessageType.RegisterCustomer || messageType == PublishExternalMessageType.RegisterVehicle ||
                    messageType == PublishExternalMessageType.PlanMaintenanceJob || messageType == PublishExternalMessageType.UndoRegisterCustomer ||
                    messageType == PublishExternalMessageType.UndoRegisterVehicle || messageType == PublishExternalMessageType.UndoPlanMaintenanceJob)
                    return;

                _logger.LogInformation($"message received in webapp- messageType:{messageType} - messageBody {message}");
                
                try
                {
                    await HandleEvent(ea);
                    await Task.Delay(2 * 1000, stoppingToken);
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

            channel.BasicConsume(queue: "SAGA-GMS-AllAPI-fanout-queue", autoAck: true, consumer: consumer);

            await Task.CompletedTask;
        }

        private Task HandleEvent(BasicDeliverEventArgs ea)
        {
            string body = Encoding.UTF8.GetString(ea.Body.ToArray());
            string messageType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"]);
            var responseMessageType = EnumUtil.ParseEnum<ServiceResponseMessageType>(messageType);

            switch (responseMessageType)
            {
                case ServiceResponseMessageType.UndoRegisterCustomerSucceed:
                case ServiceResponseMessageType.UndoRegisterVehicleSucceed:
                case ServiceResponseMessageType.UndoPlanMaintenanceJobSucceed: 
                    return HandleMessageForCompensatingResult(responseMessageType, body);

                case ServiceResponseMessageType.UndoRegisterCustomerFailed:
                case ServiceResponseMessageType.UndoRegisterVehicleFailed:
                case ServiceResponseMessageType.UndoPlanMaintenanceJobFailed:
                    return HandleMessageForCompensatingResult(responseMessageType, body);
            }

            return HandleMessageAsync(responseMessageType, body);
        }

        public async Task HandleMessageAsync(ServiceResponseMessageType messageType, string message)
        {
            try
            {
                string id = JsonConvert.DeserializeObject(message)?.ToString();
                RegisterAndPlanJobSagaModel sagaModel = _sagaMemoryStorage.Get()
                    .FirstOrDefault(r => r.EmailAddress == id || r.LicenseNumber == id || r.JobId == id);

                Log.Information($"webapp - {messageType} for message: {message}");
                
                if (sagaModel == null) return;

                if (HandleRegisterCustomerResponse(messageType, sagaModel)) return;
                if (HandleRegisterVehicleResponse(messageType, sagaModel)) return;
                if (HandlePlanMaintenanceJobResponse(messageType, sagaModel)) return;
            }
            catch (Exception ex)
            {
                //string messageId = messageObject.Property("MessageId") != null ? messageObject.Property("MessageId").Value<string>() : "[unknown]";
                //Log.Error(ex, "Error while handling {MessageType} message with id {MessageId}.", messageType, messageId);
            }

            // always acknowledge message - any errors need to be dealt with locally.
        }

        private bool HandleRegisterCustomerResponse(ServiceResponseMessageType messageType, RegisterAndPlanJobSagaModel sagaModel)
        {
            switch (messageType)
            {
                case ServiceResponseMessageType.RegisterCustomerSucceed:
                    sagaModel.RegisterCustomerSucceed = true;
                    return true;
                case ServiceResponseMessageType.RegisterCustomerFailed:
                    sagaModel.RegisterCustomerSucceed = false;
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleRegisterVehicleResponse(ServiceResponseMessageType messageType, RegisterAndPlanJobSagaModel sagaModel)
        {
            switch (messageType)
            {
                case ServiceResponseMessageType.RegisterVehicleSucceed:
                    sagaModel.RegisterVehicleSucceed = true;
                    return true;
                case ServiceResponseMessageType.RegisterVehicleFailed:
                    sagaModel.RegisterVehicleSucceed = false;
                    _messagePublisher.PublishToFanoutExchange(PublishExternalMessageType.UndoRegisterCustomer, sagaModel.EmailAddress);
                    return true;
                default:
                    return false;
            }
        }

        private bool HandlePlanMaintenanceJobResponse(ServiceResponseMessageType messageType, RegisterAndPlanJobSagaModel sagaModel)
        {
            switch (messageType)
            {
                case ServiceResponseMessageType.PlanMaintenanceJobSucceed:
                    sagaModel.PlanMaintenanceJobSucceed = true;
                    return true;
                case ServiceResponseMessageType.PlanMaintenanceJobFailed:
                    sagaModel.PlanMaintenanceJobSucceed = false;
                    _messagePublisher.PublishToFanoutExchange(PublishExternalMessageType.UndoRegisterCustomer, sagaModel.EmailAddress);
                    _messagePublisher.PublishToFanoutExchange(PublishExternalMessageType.UndoRegisterVehicle, sagaModel.LicenseNumber);
                    _messagePublisher.PublishToFanoutExchange(PublishExternalMessageType.UndoPlanMaintenanceJob, sagaModel.JobId);
                    return true;
                default:
                    return false;
            }
        }

        public async Task HandleMessageForCompensatingResult(ServiceResponseMessageType messageType, string message)
        {
            //TODO:To be completed with retry approach
            message = JsonConvert.DeserializeObject(message)?.ToString();
            return;
        }

        private async Task<bool> RegisterCustomerSuccess(string emailAddress)
        {
            RegisterAndPlanJobSagaModel sagaModel = _sagaMemoryStorage.GetByEmailAddress(emailAddress);
            while (sagaModel.RegisterCustomerSucceed==null)
            {
                _logger.LogInformation($"......In loop {sagaModel.RegisterCustomerSucceed}");
                await Task.Delay(1 * 1000);
            }

            return sagaModel.RegisterCustomerSucceed != null && (bool) sagaModel.RegisterCustomerSucceed;
        }
        
        private async Task<bool> RegisterVehicleSuccess(string licenseNumber)
        {
            RegisterAndPlanJobSagaModel sagaModel = _sagaMemoryStorage.GetByLicenseNumber(licenseNumber);
            while (sagaModel.RegisterVehicleSucceed == null)
            {
                _logger.LogInformation($"......In loop {sagaModel.RegisterVehicleSucceed}");
                await Task.Delay(1 * 1000);
            }

            return sagaModel.RegisterVehicleSucceed != null && (bool) sagaModel.RegisterVehicleSucceed;
        }

        public async Task<RegisterAndPlanJobSagaModel> GetDetailOnSagaComplete(string emailAddress)
        {
            RegisterAndPlanJobSagaModel sagaModel = _sagaMemoryStorage.GetByEmailAddress(emailAddress);
            
            while (!sagaModel.IsSagaCompleted)
            {
                _logger.LogInformation($"......In loop {sagaModel.IsSagaSuccessful}");
                await Task.Delay(1 * 1000);
            }
            sagaModel.SagaCompleteTimeStamp = DateTime.Now;
            RegisterAndPlanJobSagaModel result = sagaModel;
            
            //SAGA Completed hence removing in-memory object as it is of no use anymore
            _sagaMemoryStorage.Remove(sagaModel);
            
            return result;
        }

    }
}