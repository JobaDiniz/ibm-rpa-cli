using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Joba.IBM.RPA.Cli
{
    internal class LogMessageHandler : DelegatingHandler
    {
        private readonly ILogger logger;

        public LogMessageHandler(ILogger logger, HttpMessageHandler innerHandler) : base(innerHandler) => this.logger = logger;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            logger.LogTrace("Request started [{METHOD}] {URI}", request.Method.Method, request.RequestUri);
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();
            logger.LogTrace("Request ended [{METHOD}] {URI} ({TIME})", stopwatch.Elapsed, request.Method.Method, request.RequestUri);
            return response;
        }
    }
}
