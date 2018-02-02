using System;

namespace HarakaMQ.Client
{
    public interface IConnection : NetworkConnection, IDisposable
    {
        IModel CreateModel();
        void Close();
        void Abort();
    }
}