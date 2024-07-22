using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillIssue.ThirdParty.API.Osu.Authorization;
using SkillIssue.ThirdParty.API.Osu.Client;
using SkillIssue.ThirdParty.API.Osu.Configuration;

namespace SkillIssue.ThirdParty.API.Osu;

public static class OsuRegistrar
{
    public static void RegisterOsu(this IServiceCollection services, IConfiguration configuration)
    {
        var osuOptions = configuration.Get<OsuSecretsOption>();
        if (osuOptions?.OsuSecrets is null) throw new Exception("Failed to find 'OsuSecrets' section");

        services.Configure<OsuSecretsOption>(configuration);
        RegisterOsuClients(services, osuOptions.OsuSecrets);

        services.AddSingleton<TokenStore>();
        services.AddTransient<OsuAuthorizationHandler>();
        services.AddTransient<RateLimiterHandler>();
        services.AddTransient<IOsuClientFactory, OsuClientFactory>();
    }

    private static void RegisterOsuClients(IServiceCollection services, Dictionary<string, OsuSecret> secrets)
    {
        foreach (var secret in secrets)
        {
            if (!OsuClientType.AllowedClients.Contains(secret.Key))
            {
                throw new Exception($"Unknown osu secret. key: {secret.Key}");
            }

            services.AddHttpClient(secret.Key, client =>
                {
                    client.BaseAddress = new Uri("https://osu.ppy.sh/api/v2/");
                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
                .AddHttpMessageHandler<OsuAuthorizationHandler>()
                .AddHttpMessageHandler<RateLimiterHandler>()
                .AddStandardResilienceHandler();

            RateLimiterHandler.SetRateLimiterForClient(secret.Key, 60, TimeSpan.FromMinutes(1));
        }

        RegisterBNOClient(services);
        services.AddHttpClient<OsuAuthorizationHandler>(client => { client.Timeout = Timeout.InfiniteTimeSpan; })
            .AddStandardResilienceHandler();
    }

    private static void RegisterBNOClient(IServiceCollection services)
    {
        var clientKey = OsuClientType.Types.BNO_CLIENT.GetName();
        services.AddHttpClient(clientKey, client =>
            {
                client.BaseAddress = new Uri("https://osu.ppy.sh/");
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddHttpMessageHandler<RateLimiterHandler>()
            .AddStandardResilienceHandler();

        RateLimiterHandler.SetRateLimiterForClient(clientKey, 2, TimeSpan.FromSeconds(1));
    }
}