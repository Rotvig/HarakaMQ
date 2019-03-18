using HarakaMQ.Client;
using HarakaMQ.UDPCommunication.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarakaMQ.Test.Subscriber
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

            var factory = new ConnectionFactory();
            using (var connection = factory.CreateConnection(new HarakaMQUDPConfiguration() { ListenPort = 11700 }))
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare("hello");

                var consumer = new DefaultBasicConsumer(channel);
                var stopWatch = new Stopwatch();
                ;
                var msgNumb = 0;
                var data = new List<string>();
                consumer.Received += (model, ea) =>
                {
                    if (msgNumb == 0)
                        stopWatch.Start();
                    msgNumb++;
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    if (msgNumb == expectedMessages)
                    {
                        Console.WriteLine(msgNumb + " Received {0}", message);
                        stopWatch.Stop();
                        // Get the elapsed time as a TimeSpan value.
                        var ts = stopWatch.Elapsed;

                        // Format and display the TimeSpan value.
                        var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:00}";
                        Console.WriteLine("RunTime " + elapsedTime);
                        msgNumb = 0;
                        stopWatch = new Stopwatch();
                        data.Add(elapsedTime);
                        Task.Factory.StartNew(() => File.WriteAllText("./results.txt", JsonConvert.SerializeObject(data)));
                    }
                    else
                    {
                        Console.WriteLine(msgNumb + " Received {0}", message);
                    }
                };
                channel.BasicConsume("hello", consumer);


                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}
