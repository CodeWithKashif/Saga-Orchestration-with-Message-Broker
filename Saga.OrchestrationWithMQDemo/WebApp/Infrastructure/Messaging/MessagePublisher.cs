using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace WebApp.Infrastructure.Messaging
{
    public class MessagePublisher : IMessagePublisher
    {
        public void PublishToFanoutExchange(string messageType, object message)
        {
            using IConnection connection = GetIConnection();
            using IModel channel = connection.CreateModel();
            channel.ExchangeDeclare("SAGA-GMS-fanout-exchange", ExchangeType.Fanout);
            byte[] body = GetObjectBytes(message);
            IBasicProperties properties = GetBasicProperties(messageType, channel);
            channel.BasicPublish(exchange: "SAGA-GMS-fanout-exchange", routingKey: String.Empty,
                basicProperties: properties, body: body);
        }
        public void PublishToFanoutExchange(string exchangeName, string messageType, object message)
        {
            using IConnection connection = GetIConnection();
            using IModel channel = connection.CreateModel();
            channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout);
            byte[] body = GetObjectBytes(message);
            IBasicProperties properties = GetBasicProperties(messageType, channel);
            channel.BasicPublish(exchange: exchangeName, routingKey: String.Empty,
                basicProperties: properties, body: body);
        }

        public IConnection GetIConnection()
        {
            return new ConnectionFactory
            {
                Uri = new Uri("amqp://guest:guest@localhost:5672")
                //HostName = "localhost"
            }
            .CreateConnection();
        }

        public IConnection GetIConnectionForDispatchConsumer()
        {
            return new ConnectionFactory
            {
                HostName = "localhost",
                
                //##This is important 
                //This DispatchConsumersAsync property needs to be true. Otherwise,
                //if we run the program as is, the async event handler will not fire.
                //In other words, we won’t see anything from the Console,
                //even though the messages are published.
                DispatchConsumersAsync = true   
            }
                .CreateConnection();
        }


        private static byte[] GetObjectBytes(object message)
        {
            string serializeObject = JsonConvert.SerializeObject(message);
            byte[] body = Encoding.UTF8.GetBytes(serializeObject);
            return body;
        }

        private static IBasicProperties GetBasicProperties(string messageType, IModel channel)
        {
            IBasicProperties properties = channel.CreateBasicProperties();
            properties.Headers = new Dictionary<string, object>
            {
                {"MessageType", messageType}
            };
            return properties;
        }

    }
}