using System;

namespace HarakaMQ.MessageBroker.NET461.Interfaces
{
    public interface ITimeSyncProtocol : IDisposable
    {
        DateTime GetTime();
        void StartTimeSync();
    }
}