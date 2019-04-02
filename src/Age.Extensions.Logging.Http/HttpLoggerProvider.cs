using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace Age.Extensions.Logging.Http
{
    [ProviderAlias("Http")]
    public class HttpLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly HttpOptions options;
        private readonly HttpLogClient logClient;
        private readonly HttpMessageProcessor messageProcessor;
        private IExternalScopeProvider externalScopeProvider;
        private readonly ConcurrentDictionary<string, HttpLogger> _loggers = new ConcurrentDictionary<string, HttpLogger>();
        public HttpLoggerProvider(HttpOptions httpOptions)
        {
            options = httpOptions;
            if (options.Certificate == null && !string.IsNullOrWhiteSpace(options.CertificatePath) && !string.IsNullOrWhiteSpace(options.CertificatePassword))
            {
                options.Certificate = new X509Certificate2(options.CertificatePath, options.CertificatePassword, X509KeyStorageFlags.MachineKeySet);
            }

            if (string.IsNullOrEmpty(options.Url?.AbsoluteUri) || (options.Certificate == null && string.IsNullOrWhiteSpace(options.Username) && string.IsNullOrWhiteSpace(options.Password)))
            {
                throw new ArgumentException("Url, Certificate or Credentials are missing.", nameof(options));
            }

            logClient = new HttpLogClient(options);
            messageProcessor = new HttpMessageProcessor(logClient, options.ErrorLogger);
        }

        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, newName => new HttpLogger(newName, options, messageProcessor)
            {
                ExternalScopeProvider = externalScopeProvider,
            });
        }

        public void Dispose()
        {
            messageProcessor.Dispose();
            logClient.Dispose();
        }

        public void SetScopeProvider(IExternalScopeProvider externalScopeProvider)
        {
            this.externalScopeProvider = externalScopeProvider;
        }
    }
}
