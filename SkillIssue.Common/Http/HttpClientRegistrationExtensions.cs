using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SkillIssue.Common.Http;

public static class HttpClientRegistrationExtensions
{
    private const string AUTHORIZATION_CLIENT_NAME = "AUTH";

    public static IServiceCollection ConfigureHttpClients(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<OsuAuthorizationCredentials>(configuration.GetSection("OsuAuthorizationCredentials"));
        serviceCollection.AddHttpClient(AUTHORIZATION_CLIENT_NAME)
            .AddStandardResilienceHandler();

        return serviceCollection;
    }

    public static IServiceCollection CreateStandardHttpClient(this IServiceCollection serviceCollection, string name, string baseAddress, int requests, TimeSpan interval)
    {
        serviceCollection.AddHttpClient(name, x =>
            {
                x.Timeout = Timeout.InfiniteTimeSpan;
                x.BaseAddress = new Uri(baseAddress);
            })
            .AddHttpMessageHandler(_ => new RateLimitingHandler(name, requests, interval))
            .AddStandardResilienceHandler();

        return serviceCollection;
    }

    public static IServiceCollection RegisterOsuAPIv2Client(this IServiceCollection serviceCollection, string name)
    {
        serviceCollection.AddHttpClient(name, x =>
            {
                x.Timeout = Timeout.InfiniteTimeSpan;
                x.BaseAddress = new Uri("https://osu.ppy.sh/api/v2/");
            })
            .AddHttpMessageHandler(x =>
            {
                var httpFactory = x.GetRequiredService<IHttpClientFactory>();
                var authorizationClient = httpFactory.CreateClient(AUTHORIZATION_CLIENT_NAME);

                return new OsuAuthorizationHandler(authorizationClient,
                    x.GetRequiredService<IOptionsMonitor<OsuAuthorizationCredentials>>(),
                    x.GetRequiredService<ILogger<OsuAuthorizationHandler>>(),
                    name
                );
            })
            .AddHttpMessageHandler(_ => new RateLimitingHandler(name, 60, TimeSpan.FromMinutes(1)))
            .AddStandardResilienceHandler();

        return serviceCollection;
    }
}