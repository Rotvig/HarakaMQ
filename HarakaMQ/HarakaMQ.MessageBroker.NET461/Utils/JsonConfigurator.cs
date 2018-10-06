using System;
using System.Collections.Generic;
using System.IO;
using HarakaMQ.MessageBroker.NET461.Models;
using Newtonsoft.Json;

namespace HarakaMQ.MessageBroker.NET461.Utils
{
    internal class JsonConfigurator : IJsonConfigurator
    {
        private readonly Settings _deserializeObject;

        public JsonConfigurator()
        {
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"))
            {

                Console.WriteLine("You are missing the settings.json file. Will have it created for you ? Y/N");
                if (Console.ReadLine() == "Y" || Console.ReadLine() == "y")
                {
                    CreateDefaultSettings();
                }
                else throw new ArgumentException("Settings.json is missing");
            }
            using (var r = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"))
            {
                var json = r.ReadToEnd();
                _deserializeObject = JsonConvert.DeserializeObject<Settings>(json);
            }
        }

        public Settings GetSettings()
        {
            return _deserializeObject;
        }

        /// <summary>
        ///     Used to create default version of Json file
        /// </summary>
        public void CreateDefaultSettings()
        {
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json", 
                JsonConvert.SerializeObject(
                    new Settings {
                        TimeSyncServerAddress = "europe.pool.ntp.org",
                        BrokerPort = 11100,
                        PrimaryNumber = 1,
                        AntiEntropyMilliseonds = 200,
                        Brokers = new List<Broker> {
                            new Broker { Name = "Demo secondary cluster broker", Ipaddress = "127.0.0.1", Port = 11102, PrimaryNumber = 2 } } }));
        }
    }

    public interface IJsonConfigurator
    {
        Settings GetSettings();
        void CreateDefaultSettings();
    }
}