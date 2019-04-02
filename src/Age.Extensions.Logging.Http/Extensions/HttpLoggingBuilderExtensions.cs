using Microsoft.Extensions.Logging;
using System;

namespace Age.Extensions.Logging.Http
{
    public static class HttpLoggingBuilderExtensions
    {
        public static ILoggingBuilder AddHttp(this ILoggingBuilder builder, Func<HttpOptions> options)
        {
            builder.AddProvider(new HttpLoggerProvider(options.Invoke()));
            return builder;
        }
    }
}
 