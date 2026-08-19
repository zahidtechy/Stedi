using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>Real-time claim status (276/277) operations.</summary>
public interface IClaimStatusClient
{
    /// <summary>Submit a real-time claim status request as JSON.</summary>
    Task<ClaimStatusResponse> CheckAsync(ClaimStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>Submit a real-time claim status request as raw X12.</summary>
    Task<ClaimStatusRawX12Response> CheckRawX12Async(string x12, CancellationToken cancellationToken = default);

    /// <summary>Submit a real-time claim status request as a typed raw X12 request.</summary>
    Task<ClaimStatusRawX12Response> CheckRawX12Async(ClaimStatusRawX12Request request, CancellationToken cancellationToken = default);
}

internal sealed class ClaimStatusClient : ClientBase, IClaimStatusClient
{
    public ClaimStatusClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<ClaimStatusResponse> CheckAsync(ClaimStatusRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/change/medicalnetwork/claimstatus/v2");
        return Pipeline.PostJsonAsync<ClaimStatusResponse>(url, request, null, cancellationToken);
    }

    public Task<ClaimStatusRawX12Response> CheckRawX12Async(string x12, CancellationToken cancellationToken = default)
        => CheckRawX12Async(new ClaimStatusRawX12Request { X12 = x12 }, cancellationToken);

    public Task<ClaimStatusRawX12Response> CheckRawX12Async(ClaimStatusRawX12Request request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Ensure(request.X12, nameof(request.X12));
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/change/medicalnetwork/claimstatus/v2/raw-x12");
        return Pipeline.PostJsonAsync<ClaimStatusRawX12Response>(url, request, null, cancellationToken);
    }
}
