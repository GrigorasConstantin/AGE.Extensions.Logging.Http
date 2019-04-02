using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Age.Extensions.Logging.Http
{
    public class HttpLogClient : IDisposable
    {
        private readonly Uri registerUrl;
        private readonly X509Certificate2 certificate;
        private readonly HttpClient client;
        private bool disposed;
        public HttpLogClient(HttpOptions options)
        {
            registerUrl = options.Url;
            certificate = options.Certificate;

            var handler = new SocketsHttpHandler
            {
                MaxConnectionsPerServer = 8
            };

            if (certificate != null)
            {
                handler.SslOptions.ClientCertificates = new X509CertificateCollection(new[] { certificate });
            }

            client = new HttpClient(handler);
            if (!string.IsNullOrWhiteSpace(options.Username) && !string.IsNullOrWhiteSpace(options.Password))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(options.Username + ":" + options.Password)));
            }
            client.DefaultRequestHeaders.ConnectionClose = false; // enable connection pooling
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                client?.Dispose();
            }
            disposed = true;
        }

        public string RegisterEvent(string jsonEvent)
        {
            return RegisterEventAsync(jsonEvent).Result;
        }

        public string RegisterEventBatch(IList<string> events)
        {
            if (events == null || events.Count == 0) throw new ArgumentException("No events provided for registration", nameof(events));
            return RegisterEvent(String.Join(Environment.NewLine, events));
        }

        public Task<string> RegisterEventAsync(string jsonEvent)
        {
            if (String.IsNullOrWhiteSpace(jsonEvent)) throw new ArgumentException("Cannot register empty event", nameof(jsonEvent));
            return HttpPostAsync(jsonEvent);
        }

        public Task<string> RegisterEventBatchAsync(IList<string> jsonEvents)
        {
            if (jsonEvents == null || jsonEvents.Count == 0) throw new ArgumentException("No events provided for registration", nameof(jsonEvents));
            return RegisterEventAsync(String.Join(Environment.NewLine, jsonEvents));
        }

        private async Task<string> HttpPostAsync(string content)
        {
            using (var response = await client.PostAsync(registerUrl, new StringContent(content, Encoding.UTF8, "application/json")))
            {
                response.EnsureSuccessStatusCode();
                using (var result = response.Content)
                {
                    return await result.ReadAsStringAsync();
                }
            }
        }
    }

}
