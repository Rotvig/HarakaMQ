using System;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface ISchedular
    {
        void CancelTask(Guid taskId);
        void ScheduleTask(int miliseconds, Guid taskId, Action func);
        void ScheduleRecurringResend(int miliseconds, Guid messageId, Action<Guid> func);
        void ScheduleRecurringResend(int miliseconds, string ip, int port, int seqNo, Action<string, int, int> func);
        void TryScheduleDelayedAck(int miliseconds, string clientId, Action<string> func);
        void CancelTask(Guid messageId, string clientId, int seqNo);
    }
}