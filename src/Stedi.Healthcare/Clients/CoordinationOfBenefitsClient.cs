using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>Coordination of benefits operations.</summary>
public interface ICoordinationOfBenefitsClient
{
    /// <summary>Submit a coordination of benefits check.</summary>
    Task<CoordinationOfBenefitsResponse> CheckAsync(CoordinationOfBenefitsRequest request, CancellationToken cancellationToken = default);
}

internal sealed class CoordinationOfBenefitsClient : ClientBase, ICoordinationOfBenefitsClient
{
    public CoordinationOfBenefitsClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<CoordinationOfBenefitsResponse> CheckAsync(CoordinationOfBenefitsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/coordination-of-benefits");
        return Pipeline.PostJsonAsync<CoordinationOfBenefitsResponse>(url, request, null, cancellationToken);
    }
}
