using RabbitMQ.Client;

namespace WorkshopManagementAPI.Infrastructure.Messaging
{
    public interface IMessagePublisher
    {
        /// <summary>
        /// Most recommended approach
        /// Publishing to fanout exchange is most easy pizzy approach
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        void PublishToFanoutExchange(string messageType, object message);

        IConnection GetIConnectionForDispatchConsumer();
    }
}