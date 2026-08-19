using Stedi.Healthcare.Http;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Clients;

/// <summary>Transaction and file-execution document operations used by Healthcare workflows.</summary>
public interface ITransactionsClient
{
    /// <summary>List transactions.</summary>
    Task<ListTransactionsResponse> ListAsync(ListTransactionsRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Enumerate transactions by following <c>nextPageToken</c>.</summary>
    IAsyncEnumerable<TransactionSummary> GetAllAsync(ListTransactionsRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Poll transactions from a start timestamp.</summary>
    Task<ListPollingTransactionsResponse> PollAsync(PollTransactionsRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Enumerate polled transactions.</summary>
    IAsyncEnumerable<TransactionSummary> GetAllPolledAsync(PollTransactionsRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Retrieve transaction metadata.</summary>
    Task<GetTransactionResponse> GetAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>Download a transaction's input document. Follows Stedi's 302 redirect without forwarding the API key.</summary>
    Task<StediFileResponse> GetInputAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>Retrieve a temporary URL for the transaction input document.</summary>
    Task<GetTransactionInputDocumentUrlResponse> GetInputUrlAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>Download a transaction's output document.</summary>
    Task<StediFileResponse> GetOutputAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>Retrieve a temporary URL for the transaction output document.</summary>
    Task<GetTransactionOutputDocumentUrlResponse> GetOutputUrlAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>Download a file execution's input document.</summary>
    Task<StediFileResponse> GetExecutionInputAsync(string executionId, CancellationToken cancellationToken = default);

    /// <summary>Retrieve a temporary URL for a file execution input document.</summary>
    Task<GetExecutionInputDocumentUrlResponse> GetExecutionInputUrlAsync(string executionId, CancellationToken cancellationToken = default);
}

internal sealed class TransactionsClient : ClientBase, ITransactionsClient
{
    public TransactionsClient(StediHttpPipeline pipeline, StediHealthcareOptions options)
        : base(pipeline, options)
    {
    }

    public Task<ListTransactionsResponse> ListAsync(ListTransactionsRequest? request = null, CancellationToken cancellationToken = default)
    {
        var query = QueryStringBuilder.Build(
            ("pageSize", request?.PageSize),
            ("pageToken", request?.PageToken),
            ("businessIdentifier", request?.BusinessIdentifier),
            ("transactionSetId", request?.TransactionSetId),
            ("sender", request?.Sender),
            ("receiver", request?.Receiver),
            ("direction", request?.Direction),
            ("mode", request?.Mode),
            ("status", request?.Status),
            ("from", request?.From),
            ("to", request?.To),
            ("elementId", request?.ElementId),
            ("partnershipId", request?.PartnershipId));
        var url = StediUri.Combine(Options.CoreBaseUrl, "/transactions", query);
        return Pipeline.GetJsonAsync<ListTransactionsResponse>(url, null, cancellationToken);
    }

    public async IAsyncEnumerable<TransactionSummary> GetAllAsync(ListTransactionsRequest? request = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageRequest = request ?? new ListTransactionsRequest();
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

    public Task<ListPollingTransactionsResponse> PollAsync(PollTransactionsRequest? request = null, CancellationToken cancellationToken = default)
    {
        var query = QueryStringBuilder.Build(
            ("pageSize", request?.PageSize),
            ("pageToken", request?.PageToken),
            ("startDateTime", request?.StartDateTime));
        var url = StediUri.Combine(Options.CoreBaseUrl, "/polling/transactions", query);
        return Pipeline.GetJsonAsync<ListPollingTransactionsResponse>(url, null, cancellationToken);
    }

    public async IAsyncEnumerable<TransactionSummary> GetAllPolledAsync(PollTransactionsRequest? request = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageRequest = request ?? new PollTransactionsRequest();
        while (true)
        {
            var page = await PollAsync(pageRequest, cancellationToken).ConfigureAwait(false);
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

    public Task<GetTransactionResponse> GetAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        Ensure(transactionId, nameof(transactionId));
        var url = StediUri.Combine(Options.CoreBaseUrl, $"/transactions/{StediUri.Escape(transactionId)}");
        return Pipeline.GetJsonAsync<GetTransactionResponse>(url, null, cancellationToken);
    }

    public Task<StediFileResponse> GetInputAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        Ensure(transactionId, nameof(transactionId));
        var url = StediUri.Combine(Options.CoreBaseUrl, $"/transactions/{StediUri.Escape(transactionId)}/input");
        return Pipeline.SendFileAsync(HttpMethod.Get, url, null, null, null, null, cancellationToken);
    }

    public Task<GetTransactionInputDocumentUrlResponse> GetInputUrlAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        Ensure(transactionId, nameof(transactionId));
        var url = StediUri.Combine(Options.CoreBaseUrl, $"/transactions/{StediUri.Escape(transactionId)}/input-url");
        return Pipeline.GetJsonAsync<GetTransactionInputDocumentUrlResponse>(url, null, cancellationToken);
    }

    public Task<StediFileResponse> GetOutputAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        Ensure(transactionId, nameof(transactionId));
        var url = StediUri.Combine(Options.CoreBaseUrl, $"/transactions/{StediUri.Escape(transactionId)}/output");
        return Pipeline.SendFileAsync(HttpMethod.Get, url, null, null, null, null, cancellationToken);
    }

    public Task<GetTransactionOutputDocumentUrlResponse> GetOutputUrlAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        Ensure(transactionId, nameof(transactionId));
        var url = StediUri.Combine(Options.CoreBaseUrl, $"/transactions/{StediUri.Escape(transactionId)}/output-url");
        return Pipeline.GetJsonAsync<GetTransactionOutputDocumentUrlResponse>(url, null, cancellationToken);
    }

    public Task<StediFileResponse> GetExecutionInputAsync(string executionId, CancellationToken cancellationToken = default)
    {
        Ensure(executionId, nameof(executionId));
        var url = StediUri.Combine(Options.CoreBaseUrl, $"/executions/{StediUri.Escape(executionId)}/input");
        return Pipeline.SendFileAsync(HttpMethod.Get, url, null, null, null, null, cancellationToken);
    }

    public Task<GetExecutionInputDocumentUrlResponse> GetExecutionInputUrlAsync(string executionId, CancellationToken cancellationToken = default)
    {
        Ensure(executionId, nameof(executionId));
        var url = StediUri.Combine(Options.CoreBaseUrl, $"/executions/{StediUri.Escape(executionId)}/input-url");
        return Pipeline.GetJsonAsync<GetExecutionInputDocumentUrlResponse>(url, null, cancellationToken);
    }
}
