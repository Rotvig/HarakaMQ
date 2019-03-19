using HarakaMQ.Client;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace HarakaMQ.Test.Publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.db");

            foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.db"))
            {
                File.Delete(file);
            }

            var expectedMessages = 10000;
            var numberOfRounds = 100;
            var waitTime = 1000;

            var factory = new ConnectionFactory();
            using (var connection = factory.CreateConnection(new HarakaMQUDPConfiguration() { ListenPort = 11800, Brokers = new List<Broker> { new Broker { IpAdress = "127.0.0.1", Port = 11100 } } }))
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare("hello");

                Console.WriteLine("Start performance test by clicking [enter]");
                Console.WriteLine("Metrics: Publishing: " + expectedMessages + " messages over: " + numberOfRounds + " rounds");

                Console.ReadLine();
                for (var j = 0; j < numberOfRounds; j++)
                {
                    for (var i = 0; i < expectedMessages; i++)
                        channel.BasicPublish("hello", Encoding.UTF8.GetBytes("Hello world from (Client 1) - " + i));
                    Thread.Sleep(waitTime);
                }

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}
