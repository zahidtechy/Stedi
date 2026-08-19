using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>835 ERA remittance operations.</summary>
public interface IRemittancesClient
{
    /// <summary>Retrieve an 835 ERA report as JSON.</summary>
    Task<ConvertReport835Response> Get835Async(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>Retrieve an 835 ERA PDF.</summary>
    Task<StediFileResponse> Get835PdfAsync(string transactionId, bool? logo = null, CancellationToken cancellationToken = default);
}

internal sealed class RemittancesClient : ClientBase, IRemittancesClient
{
    public RemittancesClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<ConvertReport835Response> Get835Async(string transactionId, CancellationToken cancellationToken = default)
    {
        Ensure(transactionId, nameof(transactionId));
        var url = StediUri.Combine(Options.HealthcareBaseUrl, $"/change/medicalnetwork/reports/v2/{StediUri.Escape(transactionId)}/835");
        return Pipeline.GetJsonAsync<ConvertReport835Response>(url, null, cancellationToken);
    }

    public Task<StediFileResponse> Get835PdfAsync(string transactionId, bool? logo = null, CancellationToken cancellationToken = default)
    {
        Ensure(transactionId, nameof(transactionId));
        var query = QueryStringBuilder.Build(("logo", logo));
        var url = StediUri.Combine(Options.HealthcareBaseUrl, $"/electronic-remittance-advice/{StediUri.Escape(transactionId)}/pdf", query);
        return Pipeline.SendFileAsync(HttpMethod.Get, url, null, null, "application/pdf", null, cancellationToken);
    }
}
