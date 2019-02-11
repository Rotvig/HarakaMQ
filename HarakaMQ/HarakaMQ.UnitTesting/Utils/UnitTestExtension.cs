using System;
using System.IO;
using HarakaMQ.DB;

namespace HarakaMQ.UnitTests.Utils
{
    public abstract class UnitTestExtension : IDisposable
    {
        public IHarakaDb HarakaDb;

        protected UnitTestExtension()
        {
            HarakaDb = new HarakaDb("Test" + Guid.NewGuid());
        }

        public void Dispose()
        {
            foreach (var fileName in HarakaDb.CreatedFiles())
                File.Delete(fileName + ".db");
        }
    }
}