using System;

namespace HarakaMQ.MessageBroker.Interfaces
{
    public interface ITimeSyncProtocol : IDisposable
    {
        DateTime GetTime();
        void StartTimeSync();
    }
}