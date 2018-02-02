using System;
using System.Collections.Generic;
using System.IO;
using HarakaMQ.MessageBroker.Models;
using Newtonsoft.Json;

namespace HarakaMQ.MessageBroker.Utils
{
    internal class JsonConfigurator : IJsonConfigurator
    {
        private readonly Settings _deserializeObject;

        public JsonConfigurator()
        {
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
        public void WriteJson()
        {
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json", JsonConvert.SerializeObject(new Settings {BrokerPort = 11457, PrimaryNumber = 1, AntiEntropyMilliseonds = 200, Brokers = new List<Broker> {new Broker {Ipadress = "127.0.0.1", Port = 11457, PrimaryNumber = 2}}}));
        }
    }

    public interface IJsonConfigurator
    {
        Settings GetSettings();
        void WriteJson();
    }
}