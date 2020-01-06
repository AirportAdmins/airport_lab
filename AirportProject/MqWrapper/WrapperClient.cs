using System;
using RabbitMQ.Client;
using Newtonsoft.Json;
using System.Text;
using RabbitMQ.Client.Events;

namespace MqWrapper
{
    public class WrapperClient
    {
        IConnection mqConnection;
        IModel mqChannel;
        public WrapperClient(string hostName, string userName, string password)
        {
            var factory = new ConnectionFactory()
            {
                HostName = hostName,
                UserName = userName,
                Password = password
            };
            mqConnection = factory.CreateConnection();
            mqChannel = mqConnection.CreateModel();
        }

        public void DeclareQueues(params string[] queues)
        {
            foreach (var queue in queues)
            {
                mqChannel.QueueDeclare(queue: queue,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
            }
        }

        public void Send<T>(string queueName, T message)
        {
            var json = JsonConvert.SerializeObject(message);
            var rawMessage = Encoding.UTF8.GetBytes(json);

            mqChannel.BasicPublish(exchange: "",
                routingKey: queueName,
                basicProperties: null,
                body: rawMessage);
        }

        public void SubscribeTo<T>(string queueName, Action<T> messageHandler)
        {
            var consumer = new EventingBasicConsumer(mqChannel);
            consumer.Received += (model, ea) =>
            {
                var respBody = ea.Body;
                var respMessage = Encoding.UTF8.GetString(respBody);
                var obj = JsonConvert.DeserializeObject<T>(respMessage);
                messageHandler(obj);
            };
            
            mqChannel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);
        }

        public void Dispose()
        {
            mqChannel.Dispose();
            mqConnection.Dispose();
        }
    }
}
