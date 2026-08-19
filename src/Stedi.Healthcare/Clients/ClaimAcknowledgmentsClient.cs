using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>277 claim acknowledgment operations.</summary>
public interface IClaimAcknowledgmentsClient
{
    /// <summary>Retrieve a 277CA claim acknowledgment report.</summary>
    Task<ConvertReport277Response> Get277Async(string transactionId, CancellationToken cancellationToken = default);
}

internal sealed class ClaimAcknowledgmentsClient : ClientBase, IClaimAcknowledgmentsClient
{
    public ClaimAcknowledgmentsClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<ConvertReport277Response> Get277Async(string transactionId, CancellationToken cancellationToken = default)
    {
        Ensure(transactionId, nameof(transactionId));
        var url = StediUri.Combine(Options.HealthcareBaseUrl, $"/change/medicalnetwork/reports/v2/{StediUri.Escape(transactionId)}/277");
        return Pipeline.GetJsonAsync<ConvertReport277Response>(url, null, cancellationToken);
    }
}
