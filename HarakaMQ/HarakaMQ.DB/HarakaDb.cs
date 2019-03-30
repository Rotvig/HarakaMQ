using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarakaMQ.Shared;

namespace HarakaMQ.DB
{
    public class HarakaDb : IHarakaDb
    {
        private readonly ISerializer _serializer;
        private readonly ConcurrentDictionary<string, object> Mutexes = new ConcurrentDictionary<string, object>();
        private string _mnesiaPath = GetExecutingDirectoryName() + @"\mnesia";

        public HarakaDb(ISerializer serializer, params string[] fileNames)
        {
            _serializer = serializer;
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
                File.WriteAllBytes(_mnesiaPath + @"\" + fileName + ".db", _serializer.Serialize(obj));
            }
            catch (Exception)
            {
                File.WriteAllBytes(_mnesiaPath + @"\" + fileName + ".db", _serializer.Serialize(obj));
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
            var output = File.ReadAllBytes(_mnesiaPath + @"\" + fileName + ".db");
            return output.Length == 0 ? (List<T>) Activator.CreateInstance(typeof(List<T>)) : _serializer.Deserialize<List<T>>(output);
        }

        public List<T> TryGetObjects<T>(string fileName)
        {
            var mutex = GetLock(fileName);
            byte[] output;
            lock (mutex)
            {
                output = File.ReadAllBytes(_mnesiaPath + @"\" + fileName + ".db");
            }

            return output.Length == 0 ? (List<T>) Activator.CreateInstance(typeof(List<T>)) : _serializer.Deserialize<List<T>>(output);
        }

        public List<string> CreatedFiles()
        {
            return Mutexes.Keys.ToList();
        }

        public void CreateFiles(params string[] fileNames)
        {
            if(!Directory.Exists(_mnesiaPath))
            {
                Directory.CreateDirectory(_mnesiaPath);
            }
            foreach (var filename in fileNames)
            {
                Mutexes.TryAdd(filename, new object());
                if (!File.Exists(_mnesiaPath + @"\" + filename + ".db"))
                    File.Create(_mnesiaPath + @"\" + filename + ".db").Dispose();
            }
        }

        public object GetLock(string fileName)
        {
            if (!Mutexes.TryGetValue(fileName, out var mutex))
                throw new ArgumentException("No Mutex has been allocated for this file");
            return mutex;
        }
        
        public static string GetExecutingDirectoryName() 
        { 
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase); 
            return new FileInfo(location.AbsolutePath).Directory.FullName; 
        }
    }
}