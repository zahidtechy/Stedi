using Microsoft.Extensions.Logging;
using Stedi.Healthcare.Authentication;
using Stedi.Healthcare.Clients;
using Stedi.Healthcare.Http;

namespace Stedi.Healthcare;

/// <summary>
/// Root client for the Stedi Healthcare API.
/// </summary>
public interface IStediHealthcareClient : IDisposable
{
    /// <summary>Payer network APIs.</summary>
    IPayersClient Payers { get; }

    /// <summary>Provider APIs used by transaction enrollment.</summary>
    IProvidersClient Providers { get; }

    /// <summary>Transaction enrollment APIs.</summary>
    IEnrollmentsClient Enrollments { get; }

    /// <summary>Eligibility check APIs, including raw X12, SOAP, batch, and PDF.</summary>
    IEligibilityClient Eligibility { get; }

    /// <summary>Insurance discovery APIs.</summary>
    IInsuranceDiscoveryClient InsuranceDiscovery { get; }

    /// <summary>Coordination of benefits APIs.</summary>
    ICoordinationOfBenefitsClient CoordinationOfBenefits { get; }

    /// <summary>Claim submission and CMS-1500 PDF APIs.</summary>
    IClaimsClient Claims { get; }

    /// <summary>Claim attachment APIs.</summary>
    IAttachmentsClient Attachments { get; }

    /// <summary>277CA claim acknowledgment APIs.</summary>
    IClaimAcknowledgmentsClient ClaimAcknowledgments { get; }

    /// <summary>835 ERA APIs.</summary>
    IRemittancesClient Remittances { get; }

    /// <summary>Real-time claim status APIs.</summary>
    IClaimStatusClient ClaimStatus { get; }

    /// <summary>Transaction and file-execution APIs.</summary>
    ITransactionsClient Transactions { get; }

    /// <summary>Event APIs.</summary>
    IEventsClient Events { get; }
}

/// <summary>
/// Default Stedi Healthcare client. This type is thread-safe; reuse a single instance.
/// </summary>
public sealed class StediHealthcareClient : IStediHealthcareClient
{
    private readonly HttpClient _apiClient;
    private readonly HttpClient _downloadClient;
    private readonly bool _disposeClients;
    private bool _disposed;

    /// <summary>Creates a client that owns its <see cref="HttpClient"/> instances.</summary>
    public StediHealthcareClient(StediHealthcareOptions options, ILogger<StediHealthcareClient>? logger = null)
        : this(CreateApiClient(options, logger), new HttpClient(CreateDownloadHandler(), disposeHandler: true), options, logger, disposeClients: true)
    {
    }

    /// <summary>
    /// Creates a client from a caller-supplied API <see cref="HttpClient"/>.
    /// A separate download client is still created so pre-signed S3 URLs never receive the API key.
    /// </summary>
    public StediHealthcareClient(HttpClient httpClient, StediHealthcareOptions options, ILogger<StediHealthcareClient>? logger = null)
        : this(httpClient, new HttpClient(CreateDownloadHandler(), disposeHandler: true), options, logger, disposeClients: false)
    {
        _ownsDownloadOnly = true;
    }

    internal StediHealthcareClient(
        HttpClient apiClient,
        HttpClient downloadClient,
        StediHealthcareOptions options,
        ILogger? logger,
        bool disposeClients)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentNullException.ThrowIfNull(downloadClient);
        ArgumentNullException.ThrowIfNull(options);

        _apiClient = apiClient;
        _downloadClient = downloadClient;
        _disposeClients = disposeClients;
        Options = options;

        var pipeline = new StediHttpPipeline(apiClient, downloadClient, options, logger);
        Payers = new PayersClient(pipeline, options);
        Providers = new ProvidersClient(pipeline, options);
        Enrollments = new EnrollmentsClient(pipeline, options);
        Eligibility = new EligibilityClient(pipeline, options);
        InsuranceDiscovery = new InsuranceDiscoveryClient(pipeline, options);
        CoordinationOfBenefits = new CoordinationOfBenefitsClient(pipeline, options);
        Claims = new ClaimsClient(pipeline, options);
        Attachments = new AttachmentsClient(pipeline, options);
        ClaimAcknowledgments = new ClaimAcknowledgmentsClient(pipeline, options);
        Remittances = new RemittancesClient(pipeline, options);
        ClaimStatus = new ClaimStatusClient(pipeline, options);
        Transactions = new TransactionsClient(pipeline, options);
        Events = new EventsClient(pipeline, options);
    }

    private bool _ownsDownloadOnly;

    /// <summary>The options used to configure this client.</summary>
    public StediHealthcareOptions Options { get; }

    /// <inheritdoc />
    public IPayersClient Payers { get; }

    /// <inheritdoc />
    public IProvidersClient Providers { get; }

    /// <inheritdoc />
    public IEnrollmentsClient Enrollments { get; }

    /// <inheritdoc />
    public IEligibilityClient Eligibility { get; }

    /// <inheritdoc />
    public IInsuranceDiscoveryClient InsuranceDiscovery { get; }

    /// <inheritdoc />
    public ICoordinationOfBenefitsClient CoordinationOfBenefits { get; }

    /// <inheritdoc />
    public IClaimsClient Claims { get; }

    /// <inheritdoc />
    public IAttachmentsClient Attachments { get; }

    /// <inheritdoc />
    public IClaimAcknowledgmentsClient ClaimAcknowledgments { get; }

    /// <inheritdoc />
    public IRemittancesClient Remittances { get; }

    /// <inheritdoc />
    public IClaimStatusClient ClaimStatus { get; }

    /// <inheritdoc />
    public ITransactionsClient Transactions { get; }

    /// <inheritdoc />
    public IEventsClient Events { get; }

    internal static HttpClient CreateApiClient(StediHealthcareOptions options, ILogger? logger)
    {
        var handler = new StediAuthenticationHandler(options, logger)
        {
            InnerHandler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
            },
        };
        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = options.Timeout,
        };
    }

    internal static HttpMessageHandler CreateDownloadHandler()
        => new HttpClientHandler { AllowAutoRedirect = false };

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_disposeClients)
        {
            _apiClient.Dispose();
            _downloadClient.Dispose();
        }
        else if (_ownsDownloadOnly)
        {
            _downloadClient.Dispose();
        }
    }
}
