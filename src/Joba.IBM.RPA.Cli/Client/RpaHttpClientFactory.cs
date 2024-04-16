using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using System.Net.Security;

namespace Joba.IBM.RPA.Cli
{
    internal interface IRpaHttpClientFactory
    {
        HttpClient Create(Uri address);
        HttpClient Create(Uri address, IRenewExpiredSession sessionRenewal);
    }

    internal class RpaHttpClientFactory : IRpaHttpClientFactory
    {
        private const int MaxParallelism = 10;
        private readonly ILogger logger;

        public RpaHttpClientFactory(ILogger logger) => this.logger = logger;

        public HttpClient Create(Uri address)
        {
            var handler = new ThrottlingHttpMessageHandler(MaxParallelism, CreateUserAgentHandler(logger));
            var client = new HttpClient(handler) { BaseAddress = address };
            return client;
        }

        public HttpClient Create(Uri address, IRenewExpiredSession sessionRenewal)
        {
            var refreshTokenHandler = new RefreshTokenHttpMessageHandler(sessionRenewal, CreateUserAgentHandler(logger));
            var handler = new ThrottlingHttpMessageHandler(MaxParallelism, refreshTokenHandler);
            var client = new HttpClient(handler) { BaseAddress = address };
            return client;
        }

        private static HttpMessageHandler CreatePolicyHandler(ILogger logger)
        {
            var policy = HttpPolicyExtensions.HandleTransientHttpError()
                .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(2), 5));

            return new PolicyHttpMessageHandler(policy) { InnerHandler = CreateLogHandler(logger) };
        }

        private static HttpClientHandler CreateCoreHandler(ILogger logger)
        {
            var coreHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (requestMessage, certificate, chain, sslPolicyErrors) =>
                {
                    var isValid = sslPolicyErrors == SslPolicyErrors.None;
                    if (!isValid)
                        logger.LogDebug($"INVALID server certificate: {sslPolicyErrors}\n" +
                            "Requested URI: {URI}\n" +
                            "Effective date: {EffectiveDate}\n" +
                            "Expiration date: {ExpirationDate}\n" +
                            "Issuer: {certificate?.Issuer}\n" +
                            "Subject: {certificate?.Subject}",
                            requestMessage.RequestUri, certificate?.GetEffectiveDateString(), certificate?.GetExpirationDateString(), certificate?.Issuer, certificate?.Subject);

                    return true; //TODO: add an option to allow users to opt-in to disregard certificate issues.
                }
            };
            return coreHandler;
        }

        private static HttpMessageHandler CreateUserAgentHandler(ILogger logger)
        {
            var pollyHandler = CreatePolicyHandler(logger);
            return new UserAgentHttpMessageHandler(pollyHandler);
        }

        private static HttpMessageHandler CreateLogHandler(ILogger logger) => new LogMessageHandler(logger, CreateCoreHandler(logger));
    }
}
