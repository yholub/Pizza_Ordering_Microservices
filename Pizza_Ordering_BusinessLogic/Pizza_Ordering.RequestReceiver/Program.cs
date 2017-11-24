using Newtonsoft.Json;
using Pizza_Ordering.DataProvider.UnitOfWork;
using Pizza_Ordering.Services.BLs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pizza_Ordering.RequestReceiver
{
    class Program
    {
        private static PizzasBL _pizzaBL = new PizzasBL(new UnitOfWorkFactory());

        private const string SERVER_TO_CLIENT = "serverToClient";
        private const string CLIENT_TO_SERVER = "clientToServer";

        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: SERVER_TO_CLIENT,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    channel.QueueDeclare(queue: CLIENT_TO_SERVER,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var fixPizzas = _pizzaBL.GetFixPizzas();
                        var json = JsonConvert.SerializeObject(fixPizzas);
                        var body = Encoding.UTF8.GetBytes(json);

                        var properties = channel.CreateBasicProperties();
                        properties.Persistent = true;
                        channel.BasicPublish(exchange: "",
                                             routingKey: SERVER_TO_CLIENT,
                                             basicProperties: properties,
                                             body: body);

                        Console.WriteLine($" [x] Sent {json} {Environment.NewLine}");
                    };

                    channel.BasicConsume(
                        queue: CLIENT_TO_SERVER, 
                        autoAck: true, 
                        consumer: consumer);
                }
            }

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadKey();
        }
    }
}