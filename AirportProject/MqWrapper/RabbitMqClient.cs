using System;
using RabbitMQ.Client;
using Newtonsoft.Json;
using System.Text;
using RabbitMQ.Client.Events;
using System.Configuration;

namespace RabbitMqWrapper
{
    public class RabbitMqClient
    {
        IConnection mqConnection;
        IModel mqChannel;

        // In order to use this constructor you need to have App.config
        // file in root of the project with the following parameters
        public RabbitMqClient() 
            : this(ConfigurationManager.AppSettings["mqHostName"],
                  ConfigurationManager.AppSettings["mqUserName"],
                  ConfigurationManager.AppSettings["mqPassword"]) { }
        public RabbitMqClient(string hostName, string userName, string password)
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
        public void PurgeQueues(params string[] queues)
        {
            foreach (var queue in queues)
            {
                mqChannel.QueuePurge(queue);
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
