using HarakaMQ.MessageBroker.Interfaces;
using System;
using HarakaMQ.MessageBroker.Models;

namespace HarakaMQ.MessageBroker.Utils
{
    public class NTP : ITimeSyncProtocol
    {
        private DateTime _accurateTime;
        private readonly string _serverAddress;
        private Yort.Ntp.NtpClient _ntpClient;

        public NTP(HarakaMQMessageBrokerConfiguration harakaMqMessageBrokerConfiguration)
        {
            _serverAddress = harakaMqMessageBrokerConfiguration.TimeSyncServerAddress;
        }
        public DateTime GetTime()
        {
            return _accurateTime;
        }

        public async void StartTimeSync()
        {
            _ntpClient = new Yort.Ntp.NtpClient(_serverAddress);
            _accurateTime = await _ntpClient.RequestTimeAsync();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
