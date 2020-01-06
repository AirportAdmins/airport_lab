using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace TimetableComponent
{
    class Program
    {
        const string HOST_NAME = "v174153.hosted-by-vdsina.ru";
        const string QUEUE_NAME = "schedule-cashbox";
        const string USERNAME = "schedule";
        const string PASSWORD = "schedule";
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory()
            {
                HostName = HOST_NAME,
                UserName = USERNAME,
                Password = PASSWORD
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: QUEUE_NAME,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                string message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(message);

                // Send 10 messages
                for (int i = 0; i < 10; i++)
                {
                    // Alter first char
                    body[0]++;
                    // Send altered
                    channel.BasicPublish(exchange: "",
                                         routingKey: QUEUE_NAME,
                                         basicProperties: null,
                                         body: body);
                }
                Console.WriteLine(" [x] Sent \"{0}\" but altered", message);

                // Create listener
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var respBody = ea.Body;
                    var respMessage = Encoding.UTF8.GetString(respBody);
                    Console.WriteLine(" [x] Received {0}", respMessage);
                };
                // Start listening
                channel.BasicConsume(queue: QUEUE_NAME,
                                     autoAck: true,
                                     consumer: consumer);

                Console.ReadLine();
            }
        }
    }
}
