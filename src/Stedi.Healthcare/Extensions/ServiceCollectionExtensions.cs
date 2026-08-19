using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stedi.Healthcare.Authentication;
using Stedi.Healthcare.Clients;

namespace Stedi.Healthcare.Extensions;

/// <summary>
/// Dependency injection helpers for the Stedi Healthcare SDK.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>The named <see cref="HttpClient"/> used for authenticated Stedi API calls.</summary>
    public const string ApiClientName = "Stedi.Healthcare";

    /// <summary>The named <see cref="HttpClient"/> used for unauthenticated pre-signed downloads and uploads.</summary>
    public const string DownloadClientName = "Stedi.Healthcare.Downloads";

    /// <summary>
    /// Registers <see cref="IStediHealthcareClient"/> and related HTTP infrastructure.
    /// </summary>
    public static IServiceCollection AddStediHealthcare(this IServiceCollection services, Action<StediHealthcareOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<StediHealthcareOptions>().Configure(configure);
        return AddStediHealthcareCore(services);
    }

    /// <summary>
    /// Registers <see cref="IStediHealthcareClient"/> using pre-configured options.
    /// </summary>
    public static IServiceCollection AddStediHealthcare(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return AddStediHealthcareCore(services);
    }

    private static IServiceCollection AddStediHealthcareCore(IServiceCollection services)
    {
        services.AddTransient<StediAuthenticationHandler>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StediHealthcareOptions>>().Value;
            var logger = sp.GetService<ILogger<StediAuthenticationHandler>>();
            return new StediAuthenticationHandler(options, logger);
        });

        services.AddHttpClient(ApiClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .AddHttpMessageHandler<StediAuthenticationHandler>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<StediHealthcareOptions>>().Value;
                client.Timeout = options.Timeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Stedi.Healthcare.DotNet/1.0.0");
            });

        services.AddHttpClient(DownloadClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<StediHealthcareOptions>>().Value;
                client.Timeout = options.Timeout;
            });

        services.AddSingleton<IStediHealthcareClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var options = sp.GetRequiredService<IOptions<StediHealthcareOptions>>().Value;
            var logger = sp.GetService<ILogger<StediHealthcareClient>>();
            return new StediHealthcareClient(
                factory.CreateClient(ApiClientName),
                factory.CreateClient(DownloadClientName),
                options,
                logger,
                disposeClients: false);
        });

        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().Payers);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().Providers);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().Enrollments);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().Eligibility);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().InsuranceDiscovery);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().CoordinationOfBenefits);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().Claims);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().Attachments);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().ClaimAcknowledgments);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().Remittances);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().ClaimStatus);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().Transactions);
        services.AddSingleton(sp => sp.GetRequiredService<IStediHealthcareClient>().Events);

        return services;
    }
}
