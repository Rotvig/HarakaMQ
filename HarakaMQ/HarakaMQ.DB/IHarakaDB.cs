using System.Collections.Generic;

namespace HarakaMQ.DB
{
    public interface IHarakaDb
    {
        List<T> StoreObject<T>(string fileName, List<T> obj);

        List<T> GetObjects<T>(string fileName);

        List<T> TryGetObjects<T>(string fileName);

        List<string> CreatedFiles();

        void CreateFiles(params string[] fileNames);

        object GetLock(string fileName);
    }
}