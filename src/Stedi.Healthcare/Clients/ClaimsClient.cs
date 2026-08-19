using System.Text;
using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>Claim submission and CMS-1500 PDF operations.</summary>
public interface IClaimsClient
{
    /// <summary>Submit a professional (837P) claim as JSON.</summary>
    Task<ClaimsSubmissionResponse> SubmitProfessionalAsync(ClaimsSubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Submit a professional (837P) claim as raw X12.</summary>
    Task<ClaimsRawX12SubmissionResponse> SubmitProfessionalX12Async(string x12, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Submit a professional (837P) claim as a typed raw X12 request.</summary>
    Task<ClaimsRawX12SubmissionResponse> SubmitProfessionalX12Async(ClaimsRawX12SubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Submit a dental (837D) claim as JSON.</summary>
    Task<DentalClaimsSubmissionResponse> SubmitDentalAsync(DentalClaimsSubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Submit a dental (837D) claim as raw X12.</summary>
    Task<DentalClaimsRawX12SubmissionResponse> SubmitDentalX12Async(string x12, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Submit a dental (837D) claim as a typed raw X12 request.</summary>
    Task<DentalClaimsRawX12SubmissionResponse> SubmitDentalX12Async(DentalClaimsRawX12SubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Submit an institutional (837I) claim as JSON.</summary>
    Task<InstitutionalClaimsSubmissionResponse> SubmitInstitutionalAsync(InstitutionalClaimsSubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Submit an institutional (837I) claim as raw X12.</summary>
    Task<InstitutionalClaimsRawX12SubmissionResponse> SubmitInstitutionalX12Async(string x12, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Submit an institutional (837I) claim as a typed raw X12 request.</summary>
    Task<InstitutionalClaimsRawX12SubmissionResponse> SubmitInstitutionalX12Async(InstitutionalClaimsRawX12SubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Retrieve CMS-1500 PDFs by business identifier (claim correlation ID). PDFs are base64-encoded in JSON.</summary>
    Task<ExportPDFResponse> GetCms1500PdfByBusinessIdAsync(string businessId, bool? background = null, CancellationToken cancellationToken = default);

    /// <summary>Retrieve a CMS-1500 PDF by transaction ID. The API returns a base64 string which this method decodes into a PDF stream.</summary>
    Task<StediFileResponse> GetCms1500PdfByTransactionIdAsync(string transactionId, bool? background = null, CancellationToken cancellationToken = default);
}

internal sealed class ClaimsClient : ClientBase, IClaimsClient
{
    public ClaimsClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<ClaimsSubmissionResponse> SubmitProfessionalAsync(ClaimsSubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/change/medicalnetwork/professionalclaims/v3/submission");
        return Pipeline.PostJsonAsync<ClaimsSubmissionResponse>(url, request, options, cancellationToken);
    }

    public Task<ClaimsRawX12SubmissionResponse> SubmitProfessionalX12Async(string x12, StediCallOptions? options = null, CancellationToken cancellationToken = default)
        => SubmitProfessionalX12Async(new ClaimsRawX12SubmissionRequest { X12 = x12 }, options, cancellationToken);

    public Task<ClaimsRawX12SubmissionResponse> SubmitProfessionalX12Async(ClaimsRawX12SubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Ensure(request.X12, nameof(request.X12));
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/change/medicalnetwork/professionalclaims/v3/raw-x12-submission");
        return Pipeline.PostJsonAsync<ClaimsRawX12SubmissionResponse>(url, request, options, cancellationToken);
    }

    public Task<DentalClaimsSubmissionResponse> SubmitDentalAsync(DentalClaimsSubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/dental-claims/submission");
        return Pipeline.PostJsonAsync<DentalClaimsSubmissionResponse>(url, request, options, cancellationToken);
    }

    public Task<DentalClaimsRawX12SubmissionResponse> SubmitDentalX12Async(string x12, StediCallOptions? options = null, CancellationToken cancellationToken = default)
        => SubmitDentalX12Async(new DentalClaimsRawX12SubmissionRequest { X12 = x12 }, options, cancellationToken);

    public Task<DentalClaimsRawX12SubmissionResponse> SubmitDentalX12Async(DentalClaimsRawX12SubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Ensure(request.X12, nameof(request.X12));
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/dental-claims/raw-x12-submission");
        return Pipeline.PostJsonAsync<DentalClaimsRawX12SubmissionResponse>(url, request, options, cancellationToken);
    }

    public Task<InstitutionalClaimsSubmissionResponse> SubmitInstitutionalAsync(InstitutionalClaimsSubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/change/medicalnetwork/institutionalclaims/v1/submission");
        return Pipeline.PostJsonAsync<InstitutionalClaimsSubmissionResponse>(url, request, options, cancellationToken);
    }

    public Task<InstitutionalClaimsRawX12SubmissionResponse> SubmitInstitutionalX12Async(string x12, StediCallOptions? options = null, CancellationToken cancellationToken = default)
        => SubmitInstitutionalX12Async(new InstitutionalClaimsRawX12SubmissionRequest { X12 = x12 }, options, cancellationToken);

    public Task<InstitutionalClaimsRawX12SubmissionResponse> SubmitInstitutionalX12Async(InstitutionalClaimsRawX12SubmissionRequest request, StediCallOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Ensure(request.X12, nameof(request.X12));
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/change/medicalnetwork/institutionalclaims/v1/raw-x12-submission");
        return Pipeline.PostJsonAsync<InstitutionalClaimsRawX12SubmissionResponse>(url, request, options, cancellationToken);
    }

    public Task<ExportPDFResponse> GetCms1500PdfByBusinessIdAsync(string businessId, bool? background = null, CancellationToken cancellationToken = default)
    {
        Ensure(businessId, nameof(businessId));
        var query = QueryStringBuilder.Build(("businessId", businessId), ("background", background));
        var url = StediUri.Combine(Options.HealthcareBaseUrl, "/export/pdf", query);
        return Pipeline.GetJsonAsync<ExportPDFResponse>(url, null, cancellationToken);
    }

    public async Task<StediFileResponse> GetCms1500PdfByTransactionIdAsync(string transactionId, bool? background = null, CancellationToken cancellationToken = default)
    {
        Ensure(transactionId, nameof(transactionId));
        var query = QueryStringBuilder.Build(("background", background));
        var url = StediUri.Combine(Options.HealthcareBaseUrl, $"/export/{StediUri.Escape(transactionId)}/1500/pdf", query);
        var payload = await Pipeline.SendTextAsync(HttpMethod.Get, url, null, null, null, cancellationToken).ConfigureAwait(false);
        var pdf = DecodePdfPayload(payload);
        var stream = new MemoryStream(pdf, writable: false);
        return new StediFileResponse(stream, "application/pdf", $"{transactionId}.pdf", headers: null, stream);
    }

    internal static byte[] DecodePdfPayload(string payload)
    {
        var trimmed = payload.Trim();
        if (trimmed.StartsWith("\"", StringComparison.Ordinal) && trimmed.EndsWith("\"", StringComparison.Ordinal))
        {
            trimmed = System.Text.Json.JsonSerializer.Deserialize<string>(trimmed) ?? trimmed;
        }

        var utf8 = Encoding.UTF8.GetBytes(trimmed);
        if (utf8.Length >= 4 && utf8[0] == (byte)'%' && utf8[1] == (byte)'P' && utf8[2] == (byte)'D' && utf8[3] == (byte)'F')
        {
            return utf8;
        }

        return Convert.FromBase64String(trimmed);
    }
}
