using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>Event retrieval operations.</summary>
public interface IEventsClient
{
    /// <summary>Retrieve an event by ID.</summary>
    Task<EventDestinationsGetEventResponse> GetAsync(string eventId, CancellationToken cancellationToken = default);

    /// <summary>List events.</summary>
    Task<EventDestinationsListEventsResponse> ListAsync(ListEventsRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Enumerate events by following <c>nextPageToken</c>.</summary>
    IAsyncEnumerable<EventSummary> GetAllAsync(ListEventsRequest? request = null, CancellationToken cancellationToken = default);
}

internal sealed class EventsClient : ClientBase, IEventsClient
{
    public EventsClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<EventDestinationsGetEventResponse> GetAsync(string eventId, CancellationToken cancellationToken = default)
    {
        Ensure(eventId, nameof(eventId));
        var url = StediUri.Combine(Options.EventsBaseUrl, $"/events/{StediUri.Escape(eventId)}");
        return Pipeline.GetJsonAsync<EventDestinationsGetEventResponse>(url, null, cancellationToken);
    }

    public Task<EventDestinationsListEventsResponse> ListAsync(ListEventsRequest? request = null, CancellationToken cancellationToken = default)
    {
        var query = QueryStringBuilder.Build(
            ("pageSize", request?.PageSize),
            ("pageToken", request?.PageToken),
            ("eventId", request?.EventId),
            ("eventType", request?.EventType),
            ("status", request?.Status),
            ("created", request?.Created));
        var url = StediUri.Combine(Options.EventsBaseUrl, "/events", query);
        return Pipeline.GetJsonAsync<EventDestinationsListEventsResponse>(url, null, cancellationToken);
    }

    public async IAsyncEnumerable<EventSummary> GetAllAsync(ListEventsRequest? request = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageRequest = request ?? new ListEventsRequest();
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
}
