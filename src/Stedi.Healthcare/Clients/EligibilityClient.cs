using System.Net.Http.Headers;
using System.Text;
using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>Real-time, SOAP, batch, and PDF eligibility operations.</summary>
public interface IEligibilityClient
{
    /// <summary>Submit a real-time JSON eligibility check (270/271).</summary>
    Task<EligibilityCheckResponse> CheckAsync(EligibilityCheckRequest request, CancellationToken cancellationToken = default);

    /// <summary>Submit a real-time JSON eligibility check with extra headers such as <c>X-Forwarded-For</c>.</summary>
    Task<EligibilityCheckResponse> CheckAsync(EligibilityCheckRequest request, StediCallOptions? options, CancellationToken cancellationToken = default);

    /// <summary>Submit a real-time eligibility check from raw X12 270.</summary>
    Task<EligibilityRawX12CheckResponse> CheckRawX12Async(string x12, CancellationToken cancellationToken = default);

    /// <summary>Submit a real-time eligibility check from a typed raw X12 request.</summary>
    Task<EligibilityRawX12CheckResponse> CheckRawX12Async(EligibilityRawX12CheckRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Submit a CAQH CORE SOAP eligibility check. Credentials must already be present in the SOAP header.</summary>
    Task<SoapEligibilityResponse> CheckSoapAsync(string soapXml, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Build and submit a CAQH CORE SOAP eligibility check using <see cref="StediHealthcareOptions.AccountId"/> and the configured API key.</summary>
    Task<SoapEligibilityResponse> CheckSoapAsync(SoapEligibilityRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Submit an asynchronous batch of eligibility checks.</summary>
    Task<BatchEligibilityChecksResponse> SubmitBatchAsync(BatchEligibilityChecksRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retrieve batch processing status.</summary>
    Task<GetBatchResponse> GetBatchStatusAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>Retrieve per-check statuses for a batch.</summary>
    Task<GetBatchItemsResponse> GetBatchItemsAsync(string batchId, GetBatchItemsRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Poll completed batch eligibility results.</summary>
    Task<BatchEligibilityPollingResponse> PollBatchAsync(PollBatchEligibilityRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Enumerate completed batch eligibility results.</summary>
    IAsyncEnumerable<BatchEligibilityResultItem> GetAllBatchResultsAsync(PollBatchEligibilityRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Download the 271 eligibility PDF as binary PDF content.</summary>
    Task<StediFileResponse> GetPdfAsync(string eligibilityCheckId, CancellationToken cancellationToken = default);
}

internal sealed class EligibilityClient : ClientBase, IEligibilityClient
{
    public EligibilityClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<EligibilityCheckResponse> CheckAsync(EligibilityCheckRequest request, CancellationToken cancellationToken = default)
        => CheckAsync(request, null, cancellationToken);

    public Task<EligibilityCheckResponse> CheckAsync(EligibilityCheckRequest request, StediCallOptions? options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/change/medicalnetwork/eligibility/v3");
        return Pipeline.PostJsonAsync<EligibilityCheckResponse>(url, request, options, cancellationToken);
    }

    public Task<EligibilityRawX12CheckResponse> CheckRawX12Async(string x12, CancellationToken cancellationToken = default)
    {
        Ensure(x12, nameof(x12));
        return CheckRawX12Async(new EligibilityRawX12CheckRequest { X12 = x12 }, null, cancellationToken);
    }

    public Task<EligibilityRawX12CheckResponse> CheckRawX12Async(EligibilityRawX12CheckRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Ensure(request.X12, nameof(request.X12));
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/change/medicalnetwork/eligibility/v3/raw-x12");
        return Pipeline.PostJsonAsync<EligibilityRawX12CheckResponse>(url, request, options, cancellationToken);
    }

    public async Task<SoapEligibilityResponse> CheckSoapAsync(string soapXml, StediCallOptions? options = null, CancellationToken cancellationToken = default)
    {
        Ensure(soapXml, nameof(soapXml));
        var url = StediUri.Combine(Options.SoapEligibilityBaseUrl, "/protocols/caqh-core");
        using var response = await Pipeline.SendSoapRawAsync(url, soapXml, options?.ForwardedFor, cancellationToken).ConfigureAwait(false);
        var xml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return new SoapEligibilityResponse(
            xml,
            GetHeader(response.Headers, "stedi-id"),
            GetHeader(response.Headers, "stedi-eligibility-search-id"),
            response.Headers.ToDictionary(h => h.Key, h => (IEnumerable<string>)h.Value, StringComparer.OrdinalIgnoreCase));
    }

    public Task<SoapEligibilityResponse> CheckSoapAsync(SoapEligibilityRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Ensure(request.X12, nameof(request.X12));
        Ensure(request.SenderId, nameof(request.SenderId));
        Ensure(request.ReceiverId, nameof(request.ReceiverId));
        if (string.IsNullOrWhiteSpace(Options.AccountId))
        {
            throw new InvalidOperationException("StediHealthcareOptions.AccountId is required to build SOAP eligibility envelopes.");
        }

        var xml = SoapEligibilityEnvelope.Build(Options.AccountId, Options.ApiKey, request);
        return CheckSoapAsync(xml, options, cancellationToken);
    }

    public Task<BatchEligibilityChecksResponse> SubmitBatchAsync(BatchEligibilityChecksRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.ManagerBaseUrl, "/eligibility-manager/batch-eligibility");
        return Pipeline.PostJsonAsync<BatchEligibilityChecksResponse>(url, request, null, cancellationToken);
    }

    public Task<GetBatchResponse> GetBatchStatusAsync(string batchId, CancellationToken cancellationToken = default)
    {
        Ensure(batchId, nameof(batchId));
        var url = StediUri.Combine(Options.ManagerBaseUrl, $"/eligibility-manager/batch/{StediUri.Escape(batchId)}");
        return Pipeline.GetJsonAsync<GetBatchResponse>(url, null, cancellationToken);
    }

    public Task<GetBatchItemsResponse> GetBatchItemsAsync(string batchId, GetBatchItemsRequest? request = null, CancellationToken cancellationToken = default)
    {
        Ensure(batchId, nameof(batchId));
        var query = QueryStringBuilder.Build(
            ("pageSize", request?.PageSize),
            ("pageToken", request?.PageToken),
            ("state", request?.State),
            ("eligibilityCheckResult", request?.EligibilityCheckResult));
        var url = StediUri.Combine(Options.ManagerBaseUrl, $"/eligibility-manager/batch/{StediUri.Escape(batchId)}/items", query);
        return Pipeline.GetJsonAsync<GetBatchItemsResponse>(url, null, cancellationToken);
    }

    public Task<BatchEligibilityPollingResponse> PollBatchAsync(PollBatchEligibilityRequest? request = null, CancellationToken cancellationToken = default)
    {
        var query = QueryStringBuilder.Build(
            ("pageSize", request?.PageSize),
            ("pageToken", request?.PageToken),
            ("batchId", request?.BatchId),
            ("startDateTime", request?.StartDateTime));
        var url = StediUri.Combine(Options.ManagerBaseUrl, "/eligibility-manager/polling/batch-eligibility", query);
        return Pipeline.GetJsonAsync<BatchEligibilityPollingResponse>(url, null, cancellationToken);
    }

    public async IAsyncEnumerable<BatchEligibilityResultItem> GetAllBatchResultsAsync(PollBatchEligibilityRequest? request = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageRequest = request ?? new PollBatchEligibilityRequest();
        while (true)
        {
            var page = await PollBatchAsync(pageRequest, cancellationToken).ConfigureAwait(false);
            if (page.Items is not null)
            {
                foreach (var item in page.Items)
                {
                    if (item is not null)
                    {
                        yield return item;
                    }
                }
            }

            if (string.IsNullOrEmpty(page.NextPageToken))
            {
                yield break;
            }

            pageRequest.PageToken = page.NextPageToken;
        }
    }

    public Task<StediFileResponse> GetPdfAsync(string eligibilityCheckId, CancellationToken cancellationToken = default)
    {
        Ensure(eligibilityCheckId, nameof(eligibilityCheckId));
        var url = StediUri.Combine(Options.ManagerBaseUrl, $"/eligibility-manager/eligibility-checks/{StediUri.Escape(eligibilityCheckId)}/pdf");
        return Pipeline.SendFileAsync(HttpMethod.Get, url, null, null, "application/pdf", null, cancellationToken);
    }

    private static string? GetHeader(HttpResponseHeaders headers, string name)
        => headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
}

internal static class SoapEligibilityEnvelope
{
    public static string Build(string accountId, string apiKey, SoapEligibilityRequest request)
    {
        var payloadId = string.IsNullOrWhiteSpace(request.PayloadId) ? Guid.NewGuid().ToString() : request.PayloadId;
        var timestamp = (request.Timestamp ?? DateTimeOffset.UtcNow).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var x12 = request.X12.Contains("]]>") ? throw new ArgumentException("X12 payload cannot contain the CDATA terminator.", nameof(request)) : request.X12;

        var sb = new StringBuilder();
        sb.Append("<soapenv:Envelope xmlns:soapenv=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:cor=\"http://www.caqh.org/SOAP/WSDL/CORERule2.2.0.xsd\">");
        sb.Append("<soapenv:Header>");
        sb.Append("<wsse:Security soapenv:mustUnderstand=\"true\" xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\" xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\">");
        sb.Append("<wsse:UsernameToken>");
        sb.Append("<wsse:Username>").Append(XmlEscape(accountId)).Append("</wsse:Username>");
        sb.Append("<wsse:Password>").Append(XmlEscape(apiKey)).Append("</wsse:Password>");
        sb.Append("</wsse:UsernameToken></wsse:Security></soapenv:Header>");
        sb.Append("<soapenv:Body><cor:COREEnvelopeRealTimeRequest>");
        sb.Append("<PayloadType>X12_270_Request_005010X279A1</PayloadType>");
        sb.Append("<ProcessingMode>RealTime</ProcessingMode>");
        sb.Append("<PayloadID>").Append(XmlEscape(payloadId)).Append("</PayloadID>");
        sb.Append("<TimeStamp>").Append(XmlEscape(timestamp)).Append("</TimeStamp>");
        sb.Append("<SenderID>").Append(XmlEscape(request.SenderId)).Append("</SenderID>");
        sb.Append("<ReceiverID>").Append(XmlEscape(request.ReceiverId)).Append("</ReceiverID>");
        sb.Append("<CORERuleVersion>2.2.0</CORERuleVersion>");
        sb.Append("<Payload><![CDATA[").Append(x12).Append("]]></Payload>");
        sb.Append("</cor:COREEnvelopeRealTimeRequest></soapenv:Body></soapenv:Envelope>");
        return sb.ToString();
    }

    private static string XmlEscape(string value)
        => value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
}
