using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;
using Stedi.Healthcare.Pagination;

namespace Stedi.Healthcare.Clients;

/// <summary>Payer network operations.</summary>
public interface IPayersClient
{
    /// <summary>Retrieve a payer by Stedi payer ID.</summary>
    Task<GetPayerRecordResponse> GetAsync(string stediId, CancellationToken cancellationToken = default);

    /// <summary>List payers. Pass <see cref="ListPayersRequest.PageSize"/> to enable pagination.</summary>
    Task<ListPayerRecordsResponse> ListAsync(ListPayersRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Enumerate every payer by following <c>nextPageToken</c>.</summary>
    IAsyncEnumerable<PayerRecord> GetAllAsync(ListPayersRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Download the full payer list as CSV.</summary>
    Task<StediFileResponse> ListCsvAsync(CancellationToken cancellationToken = default);

    /// <summary>Search payers.</summary>
    Task<SearchPayersResponse> SearchAsync(SearchPayersRequest? request = null, CancellationToken cancellationToken = default);
}

internal sealed class PayersClient : ClientBase, IPayersClient
{
    public PayersClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<GetPayerRecordResponse> GetAsync(string stediId, CancellationToken cancellationToken = default)
    {
        Ensure(stediId, nameof(stediId));
        var url = StediUri.Combine(Options.PayersBaseUrl, $"/payer/{StediUri.Escape(stediId)}");
        return Pipeline.GetJsonAsync<GetPayerRecordResponse>(url, null, cancellationToken);
    }

    public Task<ListPayerRecordsResponse> ListAsync(ListPayersRequest? request = null, CancellationToken cancellationToken = default)
    {
        var query = QueryStringBuilder.Build(
            ("pageSize", request?.PageSize),
            ("pageToken", request?.PageToken));
        var url = StediUri.Combine(Options.PayersBaseUrl, "/payers", query);
        return Pipeline.GetJsonAsync<ListPayerRecordsResponse>(url, null, cancellationToken);
    }

    public async IAsyncEnumerable<PayerRecord> GetAllAsync(ListPayersRequest? request = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageRequest = request ?? new ListPayersRequest();
        if (pageRequest.PageSize is null)
        {
            pageRequest.PageSize = 100;
        }

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

    public Task<StediFileResponse> ListCsvAsync(CancellationToken cancellationToken = default)
    {
        var url = StediUri.Combine(Options.PayersBaseUrl, "/payers/csv");
        return Pipeline.SendFileAsync(HttpMethod.Get, url, null, null, "text/plain", null, cancellationToken);
    }

    public Task<SearchPayersResponse> SearchAsync(SearchPayersRequest? request = null, CancellationToken cancellationToken = default)
    {
        var query = QueryStringBuilder.Build(
            ("pageSize", request?.PageSize),
            ("pageToken", request?.PageToken),
            ("query", request?.Query),
            ("eligibilityCheck", request?.EligibilityCheck),
            ("claimStatus", request?.ClaimStatus),
            ("professionalClaimSubmission", request?.ProfessionalClaimSubmission),
            ("dentalClaimSubmission", request?.DentalClaimSubmission),
            ("institutionalClaimSubmission", request?.InstitutionalClaimSubmission),
            ("claimPayment", request?.ClaimPayment),
            ("coordinationOfBenefits", request?.CoordinationOfBenefits),
            ("unsolicitedClaimAttachment", request?.UnsolicitedClaimAttachment),
            ("coverageTypes", request?.CoverageTypes),
            ("operatingStates", request?.OperatingStates),
            ("programs", request?.Programs));
        var url = StediUri.Combine(Options.PayersBaseUrl, "/payers/search", query);
        return Pipeline.GetJsonAsync<SearchPayersResponse>(url, null, cancellationToken);
    }
}
