using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;

namespace HarakaMQ.DB
{
    public class HarakaDb : IHarakaDb
    {
        private readonly ConcurrentDictionary<string, object> Mutexes = new ConcurrentDictionary<string, object>();

        public HarakaDb()
        {
        }

        public HarakaDb(params string[] fileNames)
        {
            CreateFiles(fileNames);
        }

        /// <summary>
        ///     Remember to put a lock around
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public List<T> StoreObject<T>(string fileName, List<T> obj)
        {
            //This is a HACK ! and should be changed when a better solution is found
            //It is needed because the file is locked some times
            try
            {
                File.WriteAllBytes(fileName + ".db", MessagePackSerializer.Serialize(obj));
            }
            catch (Exception e)
            {
                File.WriteAllBytes(fileName + ".db", MessagePackSerializer.Serialize(obj));
            }
            return obj;
        }

        /// <summary>
        ///     Remember to put a lock around
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public List<T> GetObjects<T>(string fileName)
        {
            var output = File.ReadAllBytes(fileName + ".db");
            return output.Length == 0 ? (List<T>) Activator.CreateInstance(typeof(List<T>)) : MessagePackSerializer.Deserialize<List<T>>(output);
        }

        public List<T> TryGetObjects<T>(string fileName)
        {
            var mutex = GetLock(fileName);
            byte[] output;
            lock (mutex)
            {
                output = File.ReadAllBytes(fileName + ".db");
            }

            return output.Length == 0 ? (List<T>) Activator.CreateInstance(typeof(List<T>)) : MessagePackSerializer.Deserialize<List<T>>(output);
        }

        public List<string> CreatedFiles()
        {
            return Mutexes.Keys.ToList();
        }

        public void CreateFiles(params string[] fileNames)
        {
            foreach (var filename in fileNames)
            {
                Mutexes.TryAdd(filename, new object());
                if (!File.Exists(filename + ".db"))
                    File.Create(filename + ".db").Dispose();
            }
        }

        public object GetLock(string fileName)
        {
            if (!Mutexes.TryGetValue(fileName, out var mutex))
                throw new ArgumentException("No Mutex has been allocated for this file");
            return mutex;
        }
    }
}