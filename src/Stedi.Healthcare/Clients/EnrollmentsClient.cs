using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>Transaction enrollment operations, including document upload and task updates.</summary>
public interface IEnrollmentsClient
{
    /// <summary>Create an enrollment request.</summary>
    Task<CreateEnrollmentResponse> CreateAsync(CreateEnrollmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retrieve an enrollment.</summary>
    Task<GetEnrollmentResponse> GetAsync(string enrollmentId, CancellationToken cancellationToken = default);

    /// <summary>List enrollments.</summary>
    Task<ListEnrollmentsResponse> ListAsync(ListEnrollmentsRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Enumerate every enrollment by following <c>nextPageToken</c>.</summary>
    IAsyncEnumerable<EnrollmentSummary> GetAllAsync(ListEnrollmentsRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Update an enrollment.</summary>
    Task<UpdateEnrollmentResponse> UpdateAsync(string enrollmentId, UpdateEnrollmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Delete an enrollment.</summary>
    Task DeleteAsync(string enrollmentId, CancellationToken cancellationToken = default);

    /// <summary>Request a pre-signed URL for downloading an enrollment document.</summary>
    Task<CreateEnrollmentDocumentDownloadResponse> GetDocumentDownloadAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>Download an enrollment document by following the pre-signed URL.</summary>
    Task<StediFileResponse> DownloadDocumentAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>Request a pre-signed URL for uploading an enrollment PDF.</summary>
    Task<CreateEnrollmentDocumentUploadResponse> CreateDocumentUploadAsync(string enrollmentId, CreateEnrollmentDocumentUploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Request a pre-signed URL and upload the PDF stream to it.
    /// Stedi's enrollment API does not use multipart/form-data; uploads are <c>PUT</c> to a pre-signed URL with <c>Content-Type: application/pdf</c>.
    /// </summary>
    Task<CreateEnrollmentDocumentUploadResponse> UploadDocumentAsync(string enrollmentId, string fileName, string taskId, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Delete an enrollment document.</summary>
    Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>Update an enrollment task.</summary>
    Task<UpdateTaskPostResponse> UpdateTaskAsync(string taskId, UpdateTaskPostRequest request, CancellationToken cancellationToken = default);

    /// <summary>Export enrollments as CSV metadata (JSON response containing export details).</summary>
    Task<ExportEnrollmentsCsvResponse> ExportCsvAsync(ExportEnrollmentsCsvRequest request, CancellationToken cancellationToken = default);
}

internal sealed class EnrollmentsClient : ClientBase, IEnrollmentsClient
{
    public EnrollmentsClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<CreateEnrollmentResponse> CreateAsync(CreateEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, "/enrollments");
        return Pipeline.PostJsonAsync<CreateEnrollmentResponse>(url, request, null, cancellationToken);
    }

    public Task<GetEnrollmentResponse> GetAsync(string enrollmentId, CancellationToken cancellationToken = default)
    {
        Ensure(enrollmentId, nameof(enrollmentId));
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, $"/enrollments/{StediUri.Escape(enrollmentId)}");
        return Pipeline.GetJsonAsync<GetEnrollmentResponse>(url, null, cancellationToken);
    }

    public Task<ListEnrollmentsResponse> ListAsync(ListEnrollmentsRequest? request = null, CancellationToken cancellationToken = default)
    {
        var query = QueryStringBuilder.Build(
            ("pageSize", request?.PageSize),
            ("pageToken", request?.PageToken),
            ("filter", request?.Filter),
            ("status", request?.Status),
            ("providerNpis", request?.ProviderNpis),
            ("providerTaxIds", request?.ProviderTaxIds),
            ("providerNames", request?.ProviderNames),
            ("providerIds", request?.ProviderIds),
            ("payerIds", request?.PayerIds),
            ("sources", request?.Sources),
            ("transactions", request?.Transactions),
            ("createdFrom", request?.CreatedFrom),
            ("createdTo", request?.CreatedTo),
            ("statusUpdatedFrom", request?.StatusUpdatedFrom),
            ("statusUpdatedTo", request?.StatusUpdatedTo),
            ("importId", request?.ImportId),
            ("requestedEffectiveDateFrom", request?.RequestedEffectiveDateFrom),
            ("requestedEffectiveDateTo", request?.RequestedEffectiveDateTo),
            ("lastEraReceivedFrom", request?.LastEraReceivedFrom),
            ("lastEraReceivedTo", request?.LastEraReceivedTo),
            ("userEmails", request?.UserEmails),
            ("sortBy", request?.SortBy));
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, "/enrollments", query);
        return Pipeline.GetJsonAsync<ListEnrollmentsResponse>(url, null, cancellationToken);
    }

    public async IAsyncEnumerable<EnrollmentSummary> GetAllAsync(ListEnrollmentsRequest? request = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageRequest = request ?? new ListEnrollmentsRequest();
        while (true)
        {
            var page = await ListAsync(pageRequest, cancellationToken).ConfigureAwait(false);
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

    public Task<UpdateEnrollmentResponse> UpdateAsync(string enrollmentId, UpdateEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        Ensure(enrollmentId, nameof(enrollmentId));
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, $"/enrollments/{StediUri.Escape(enrollmentId)}");
        return Pipeline.SendJsonAsync<UpdateEnrollmentResponse>(HttpMethod.Post, url, request, null, cancellationToken);
    }

    public Task DeleteAsync(string enrollmentId, CancellationToken cancellationToken = default)
    {
        Ensure(enrollmentId, nameof(enrollmentId));
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, $"/enrollments/{StediUri.Escape(enrollmentId)}");
        return Pipeline.SendAsync(HttpMethod.Delete, url, null, null, cancellationToken);
    }

    public Task<CreateEnrollmentDocumentDownloadResponse> GetDocumentDownloadAsync(string documentId, CancellationToken cancellationToken = default)
    {
        Ensure(documentId, nameof(documentId));
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, $"/documents/{StediUri.Escape(documentId)}/download");
        return Pipeline.GetJsonAsync<CreateEnrollmentDocumentDownloadResponse>(url, null, cancellationToken);
    }

    public async Task<StediFileResponse> DownloadDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var meta = await GetDocumentDownloadAsync(documentId, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(meta.DownloadUrl))
        {
            throw new StediApiException("The enrollment document download URL was missing.", System.Net.HttpStatusCode.OK);
        }

        return await Pipeline.GetPresignedFileAsync(new Uri(meta.DownloadUrl, UriKind.Absolute), cancellationToken).ConfigureAwait(false);
    }

    public Task<CreateEnrollmentDocumentUploadResponse> CreateDocumentUploadAsync(string enrollmentId, CreateEnrollmentDocumentUploadRequest request, CancellationToken cancellationToken = default)
    {
        Ensure(enrollmentId, nameof(enrollmentId));
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, $"/enrollments/{StediUri.Escape(enrollmentId)}/documents");
        return Pipeline.PostJsonAsync<CreateEnrollmentDocumentUploadResponse>(url, request, null, cancellationToken);
    }

    public async Task<CreateEnrollmentDocumentUploadResponse> UploadDocumentAsync(string enrollmentId, string fileName, string taskId, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        Ensure(fileName, nameof(fileName));
        Ensure(taskId, nameof(taskId));
        var created = await CreateDocumentUploadAsync(
            enrollmentId,
            new CreateEnrollmentDocumentUploadRequest { Name = fileName, TaskId = taskId },
            cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(created.UploadUrl))
        {
            throw new StediApiException("The enrollment document upload URL was missing.", System.Net.HttpStatusCode.OK);
        }

        await Pipeline.PutStreamAsync(new Uri(created.UploadUrl, UriKind.Absolute), content, "application/pdf", cancellationToken).ConfigureAwait(false);
        return created;
    }

    public Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        Ensure(documentId, nameof(documentId));
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, $"/documents/{StediUri.Escape(documentId)}");
        return Pipeline.SendAsync(HttpMethod.Delete, url, null, null, cancellationToken);
    }

    public Task<UpdateTaskPostResponse> UpdateTaskAsync(string taskId, UpdateTaskPostRequest request, CancellationToken cancellationToken = default)
    {
        Ensure(taskId, nameof(taskId));
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, $"/tasks/{StediUri.Escape(taskId)}");
        return Pipeline.PostJsonAsync<UpdateTaskPostResponse>(url, request, null, cancellationToken);
    }

    public Task<ExportEnrollmentsCsvResponse> ExportCsvAsync(ExportEnrollmentsCsvRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, "/enrollments/export");
        return Pipeline.PostJsonAsync<ExportEnrollmentsCsvResponse>(url, request, null, cancellationToken);
    }
}
