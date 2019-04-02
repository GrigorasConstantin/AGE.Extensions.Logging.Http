using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Age.Extensions.Logging.Http
{
    public class HttpLogger : ILogger
    {
        private readonly string name;
        private readonly HttpOptions options;
        private readonly HttpMessageProcessor messageProcessor;
        public HttpLogger(string name, HttpOptions options, HttpMessageProcessor messageProcessor)
        {
            this.name = name;
            this.options = options;
            this.messageProcessor = messageProcessor;
        }
        internal IExternalScopeProvider ExternalScopeProvider { get; set; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var scopeDictionary = new Dictionary<string, object>();
            PopulateScope(scopeDictionary, state);

            if (options.Certificate != null)
            {
                if(scopeDictionary.ContainsKey("event_type"))
                    messageProcessor.EnqueueMessage(scopeDictionary.ToJson());
                return;
            }
            else
            {
                messageProcessor.EnqueueMessage(scopeDictionary.ToJson());
                return;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && options.Filter?.Invoke(name, logLevel) != false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return ExternalScopeProvider != null ? ExternalScopeProvider.Push(state) : NullScope.Instance;
        }

        private void PopulateScope<TState>(Dictionary<string, object> dict, TState state)
        {
            if (options.IncludeScopes)
            {
                if (state is IReadOnlyCollection<KeyValuePair<string, object>> stateDictionary)
                {
                    if (options.LogFromScopeMapings)
                    {
                        dict.ParseCollection(stateDictionary, options.ScopeMappings);
                    }
                    else
                    {
                        foreach (KeyValuePair<string, object> item in stateDictionary)
                        {
                            if (item.Key == "{OriginalFormat}") continue;
                            dict.Add(item.Key, item.Value);
                        }
                    }
                }

                if (ExternalScopeProvider != null)
                {
                ExternalScopeProvider.ForEachScope(
                    (activeScope, builder) =>
                    {
                        if (activeScope is IReadOnlyCollection<KeyValuePair<string, object>> activeScopeDictionary)
                        {
                            if (options.LogFromScopeMapings)
                            {
                                dict.ParseCollection(activeScopeDictionary, options.ScopeMappings);
                            }
                            else
                            {
                                foreach (KeyValuePair<string, object> item in activeScopeDictionary)
                                {

                                    if (item.Key == "{OriginalFormat}") continue;
                                    dict.Add(item.Key, item.Value);
                                }
                            }
                        }
                    }, state);
                }
                if (!dict.ContainsKey("event_time") && dict.ContainsKey("event_type"))
                {
                    dict.Add("event_time", DateTime.Now);
                }
            }
        }
    }

    public static class Extensions
    {
        public static string ToJson(this Dictionary<string, object> message)
        {
            return JsonConvert.SerializeObject(message, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        public static void ParseCollection(this Dictionary<string, object> init, IReadOnlyCollection<KeyValuePair<string, object>> state, Dictionary<string, string> scopeMappings)
        {
            foreach (KeyValuePair<string, object> item in state)
            {
                if (scopeMappings.TryGetValue(item.Key, out var valueName))
                {
                    init.Add(valueName, item.Value);
                }
            }
        }
    }
}
