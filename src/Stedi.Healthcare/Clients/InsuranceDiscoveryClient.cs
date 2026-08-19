using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>Insurance discovery operations.</summary>
public interface IInsuranceDiscoveryClient
{
    /// <summary>Start an insurance discovery check.</summary>
    Task<InsuranceDiscoveryCheckResponse> CheckAsync(InsuranceDiscoveryCheckRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retrieve insurance discovery results.</summary>
    Task<GetInsuranceDiscoveryCheckResponse> GetResultsAsync(string discoveryId, CancellationToken cancellationToken = default);
}

internal sealed class InsuranceDiscoveryClient : ClientBase, IInsuranceDiscoveryClient
{
    public InsuranceDiscoveryClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<InsuranceDiscoveryCheckResponse> CheckAsync(InsuranceDiscoveryCheckRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/insurance-discovery/check/v1");
        return Pipeline.PostJsonAsync<InsuranceDiscoveryCheckResponse>(url, request, null, cancellationToken);
    }

    public Task<GetInsuranceDiscoveryCheckResponse> GetResultsAsync(string discoveryId, CancellationToken cancellationToken = default)
    {
        Ensure(discoveryId, nameof(discoveryId));
        var url = StediUri.Combine(Options.HealthcareBaseUrl, $"/insurance-discovery/check/v1/{StediUri.Escape(discoveryId)}");
        return Pipeline.GetJsonAsync<GetInsuranceDiscoveryCheckResponse>(url, null, cancellationToken);
    }
}
