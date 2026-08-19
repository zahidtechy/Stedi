using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>Provider directory operations used by transaction enrollment.</summary>
public interface IProvidersClient
{
    /// <summary>Create a provider.</summary>
    Task<CreateProviderResponse> CreateAsync(CreateProviderRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retrieve a provider.</summary>
    Task<GetProviderResponse> GetAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>List providers.</summary>
    Task<ListProvidersResponse> ListAsync(ListProvidersRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Enumerate every provider by following <c>nextPageToken</c>.</summary>
    IAsyncEnumerable<ProviderSummary> GetAllAsync(ListProvidersRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Update a provider.</summary>
    Task<UpdateProviderResponse> UpdateAsync(string providerId, UpdateProviderRequest request, CancellationToken cancellationToken = default);

    /// <summary>Delete a provider.</summary>
    Task DeleteAsync(string providerId, CancellationToken cancellationToken = default);
}

internal sealed class ProvidersClient : ClientBase, IProvidersClient
{
    public ProvidersClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<CreateProviderResponse> CreateAsync(CreateProviderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, "/providers");
        return Pipeline.PostJsonAsync<CreateProviderResponse>(url, request, null, cancellationToken);
    }

    public Task<GetProviderResponse> GetAsync(string providerId, CancellationToken cancellationToken = default)
    {
        Ensure(providerId, nameof(providerId));
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, $"/providers/{StediUri.Escape(providerId)}");
        return Pipeline.GetJsonAsync<GetProviderResponse>(url, null, cancellationToken);
    }

    public Task<ListProvidersResponse> ListAsync(ListProvidersRequest? request = null, CancellationToken cancellationToken = default)
    {
        var query = QueryStringBuilder.Build(
            ("pageSize", request?.PageSize),
            ("pageToken", request?.PageToken),
            ("filter", request?.Filter),
            ("providerNpis", request?.ProviderNpis),
            ("providerTaxIds", request?.ProviderTaxIds));
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, "/providers", query);
        return Pipeline.GetJsonAsync<ListProvidersResponse>(url, null, cancellationToken);
    }

    public async IAsyncEnumerable<ProviderSummary> GetAllAsync(ListProvidersRequest? request = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageRequest = request ?? new ListProvidersRequest();
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

    public Task<UpdateProviderResponse> UpdateAsync(string providerId, UpdateProviderRequest request, CancellationToken cancellationToken = default)
    {
        Ensure(providerId, nameof(providerId));
        ArgumentNullException.ThrowIfNull(request);
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, $"/providers/{StediUri.Escape(providerId)}");
        return Pipeline.SendJsonAsync<UpdateProviderResponse>(HttpMethod.Post, url, request, null, cancellationToken);
    }

    public Task DeleteAsync(string providerId, CancellationToken cancellationToken = default)
    {
        Ensure(providerId, nameof(providerId));
        var url = StediUri.Combine(Options.EnrollmentsBaseUrl, $"/providers/{StediUri.Escape(providerId)}");
        return Pipeline.SendAsync(HttpMethod.Delete, url, null, null, cancellationToken);
    }
}
