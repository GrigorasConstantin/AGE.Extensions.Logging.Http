using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Age.Extensions.Logging.Http
{
    public class HttpMessageProcessor : IDisposable
    {
        private const int maxQueuedMessages = 1024;
        private readonly HttpLogClient httpClient;
        private readonly BlockingCollection<string> messageQueue;
        private readonly Thread processorThread;
        private ILogger errorLogger;
        public HttpMessageProcessor(HttpLogClient httpLogClient, ILogger errorLogger)
        {
            httpClient = httpLogClient;
            this.errorLogger = errorLogger;
            messageQueue = new BlockingCollection<string>(maxQueuedMessages);
            processorThread = new Thread(StartAsync)
            {
                IsBackground = true,
                Name = "Http logger queue processing thread"
            };
            processorThread.Start();
        }

        public virtual void EnqueueMessage(string message)
        {
            if (!messageQueue.TryAdd(message))
            {
                httpClient.RegisterEvent(message);
            }
        }

        private void StartAsync()
        {
            foreach (var message in messageQueue.GetConsumingEnumerable())
            {
                try
                {
                    httpClient.RegisterEvent(message);
                }
                catch (Exception ex)
                {
                    errorLogger?.LogCritical(ex, "Error on sending logs to Http");
                }
            }           
        }

        public void Dispose()
        {
            messageQueue.CompleteAdding();
            processorThread.Join(30000);
        }
    }
}
