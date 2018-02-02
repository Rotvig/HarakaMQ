using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarakaMQ.UDPCommunication.Interfaces;

namespace HarakaMQ.UDPCommunication
{
    public class Schedular : ISchedular
    {
        public Schedular()
        {
            Jobs = new List<Tuple<CancellationTokenSource, Guid>>();
            JobsWithClientId = new Dictionary<string, CancellationTokenSource>();
        }

        public List<Tuple<CancellationTokenSource, Guid>> Jobs { get; set; }
        public Dictionary<string, CancellationTokenSource> JobsWithClientId { get; set; }

        public void CancelTask(Guid taskId)
        {
            var job = Jobs.Find(x => x.Item2 == taskId);
            if (job == null)
                return;

            job.Item1.Cancel();
            Jobs.Remove(job);
        }

        public void ScheduleTask(int miliseconds, Guid taskId, Action func)
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            Jobs.Add(new Tuple<CancellationTokenSource, Guid>(cts, taskId));
            Task.Delay(miliseconds).ContinueWith(t => { func(); }, token);
        }

        public void ScheduleRecurringResend(int miliseconds, Guid messageId, Action<Guid> func)
        {
            //return if the task exists
            if (Jobs.Any(x => x.Item2 == messageId))
                return;
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            Jobs.Add(new Tuple<CancellationTokenSource, Guid>(cts, messageId));
            Task.Delay(miliseconds).ContinueWith(t => { CreateDelayedRecurringResend(miliseconds, messageId, func, token); }, token);
        }

        public void ScheduleRecurringResend(int miliseconds, string ip, int port, int seqNo, Action<string, int, int> func)
        {
            var clientId = ip + port;
            //Create a unique ID with clientId and seqNo
            if (!JobsWithClientId.ContainsKey(clientId + seqNo))
            {
                var cts = new CancellationTokenSource();
                var token = cts.Token;
                JobsWithClientId.Add(clientId + seqNo, cts);
                Task.Delay(miliseconds).ContinueWith(t => { CreateDelayedRecurringResend(miliseconds, ip, port, seqNo, func, token); }, token);
            }
        }

        public void TryScheduleDelayedAck(int miliseconds, string clientId, Action<string> func)
        {
            if (JobsWithClientId.ContainsKey(clientId))
            {
                JobsWithClientId[clientId].Cancel();
                JobsWithClientId.Remove(clientId);
                var cts = new CancellationTokenSource();
                var token = cts.Token;
                JobsWithClientId.Add(clientId, cts);
                Task.Delay(miliseconds).ContinueWith(t => { func(clientId); }, token);
            }
            else
            {
                var cts = new CancellationTokenSource();
                var token = cts.Token;
                JobsWithClientId.Add(clientId, cts);
                Task.Delay(miliseconds).ContinueWith(t => { func(clientId); }, token);
            }
        }

        public void CancelTask(Guid messageId, string clientId, int seqNo)
        {
            if (JobsWithClientId.ContainsKey(clientId + seqNo))
            {
                JobsWithClientId[clientId + seqNo].Cancel();
                JobsWithClientId.Remove(clientId + seqNo);
            }
            else if (Jobs.Any(x => x.Item2 == messageId))
            {
                var job = Jobs.Find(x => x.Item2 == messageId);
                job.Item1.Cancel();
                Jobs.Remove(job);
            }
        }

        private void CreateDelayedRecurringResend(int miliseconds, string ip, int port, int seqNo, Action<string, int, int> func, CancellationToken token)
        {
            func(ip, port, seqNo);
            Task.Delay(miliseconds).ContinueWith(t => { CreateDelayedRecurringResend(miliseconds, ip, port, seqNo, func, token); }, token);
        }

        private void CreateDelayedRecurringResend(int miliseconds, Guid messageId, Action<Guid> func, CancellationToken token)
        {
            func(messageId);
            Task.Delay(miliseconds).ContinueWith(t => { CreateDelayedRecurringResend(miliseconds, messageId, func, token); }, token);
        }
    }
}