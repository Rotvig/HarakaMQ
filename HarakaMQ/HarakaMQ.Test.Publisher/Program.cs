using HarakaMQ.Client;
using System;
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

            var ip = "127.0.0.1";
            var brokerPort = 11100;
            var expectedMessages = 10000;
            var numberOfRounds = 100;
            var waitTime = 1000;
            if (args.Length >= 2)
            {
                ip = args.First();
                brokerPort = int.Parse(args[1]);
                waitTime = int.Parse(args[2]);
                expectedMessages = int.Parse(args.Last());
                Console.WriteLine("Running with IP: " + ip + " Broker Port: " + brokerPort);
            }
            var factory = new ConnectionFactory { HostName = ip, ListenPort = 11000, Port = brokerPort };
            using (var connection = factory.CreateConnection())
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
