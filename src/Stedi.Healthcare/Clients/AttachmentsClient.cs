using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>Claim attachment operations.</summary>
public interface IAttachmentsClient
{
    /// <summary>Request a pre-signed URL for uploading a claim attachment file.</summary>
    Task<CreateClaimAttachmentFileResponse> CreateFileAsync(CreateClaimAttachmentFileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Request a pre-signed URL and upload the attachment stream.
    /// Stedi does not use multipart/form-data for this API; the file is sent with HTTP <c>PUT</c>.
    /// </summary>
    Task<CreateClaimAttachmentFileResponse> UploadFileAsync(string contentType, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Submit a claim attachment as raw X12 275.</summary>
    Task<SubmitClaimAttachmentRawX12Response> SubmitRawX12Async(string x12, CancellationToken cancellationToken = default);

    /// <summary>Submit a claim attachment as a typed raw X12 request.</summary>
    Task<SubmitClaimAttachmentRawX12Response> SubmitRawX12Async(SubmitClaimAttachmentRawX12Request request, CancellationToken cancellationToken = default);
}

internal sealed class AttachmentsClient : ClientBase, IAttachmentsClient
{
    public AttachmentsClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<CreateClaimAttachmentFileResponse> CreateFileAsync(CreateClaimAttachmentFileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.ClaimsBaseUrl, "/claim-attachments/file");
        return Pipeline.PostJsonAsync<CreateClaimAttachmentFileResponse>(url, request, null, cancellationToken);
    }

    public async Task<CreateClaimAttachmentFileResponse> UploadFileAsync(string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        Ensure(contentType, nameof(contentType));
        ArgumentNullException.ThrowIfNull(content);
        var created = await CreateFileAsync(new CreateClaimAttachmentFileRequest { ContentType = contentType }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(created.UploadUrl))
        {
            throw new StediApiException("The claim attachment upload URL was missing.", System.Net.HttpStatusCode.OK);
        }

        await Pipeline.PutStreamAsync(new Uri(created.UploadUrl, UriKind.Absolute), content, contentType, cancellationToken).ConfigureAwait(false);
        return created;
    }

    public Task<SubmitClaimAttachmentRawX12Response> SubmitRawX12Async(string x12, CancellationToken cancellationToken = default)
        => SubmitRawX12Async(new SubmitClaimAttachmentRawX12Request { X12 = x12 }, cancellationToken);

    public Task<SubmitClaimAttachmentRawX12Response> SubmitRawX12Async(SubmitClaimAttachmentRawX12Request request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Ensure(request.X12, nameof(request.X12));
        var url = StediUri.Combine(Options.ClaimsBaseUrl, "/claim-attachments/raw-x12-submission");
        return Pipeline.PostJsonAsync<SubmitClaimAttachmentRawX12Response>(url, request, null, cancellationToken);
    }
}
