using Stedi.Healthcare.Configuration;

namespace Stedi.Healthcare;

/// <summary>
/// Configuration for the Stedi Healthcare SDK.
/// </summary>
public sealed class StediHealthcareOptions
{
    /// <summary>
    /// Stedi API key sent in the <c>Authorization</c> header.
    /// Do not log this value. Use a test key for mock eligibility requests and a production key for live transactions.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Stedi account ID required only when the SDK builds CAQH CORE SOAP envelopes.
    /// SOAP authentication uses the account ID as the WS-Security username.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>Base URL for Healthcare clearinghouse APIs.</summary>
    public string HealthcareBaseUrl { get; set; } = StediEndpointDefaults.HealthcareBaseUrl;

    /// <summary>Base URL for the CAQH CORE SOAP eligibility endpoint.</summary>
    public string SoapEligibilityBaseUrl { get; set; } = StediEndpointDefaults.SoapEligibilityBaseUrl;

    /// <summary>Base URL for claim attachment APIs.</summary>
    public string ClaimsBaseUrl { get; set; } = StediEndpointDefaults.ClaimsBaseUrl;

    /// <summary>Base URL for enrollment and provider APIs.</summary>
    public string EnrollmentsBaseUrl { get; set; } = StediEndpointDefaults.EnrollmentsBaseUrl;

    /// <summary>Base URL for payer APIs.</summary>
    public string PayersBaseUrl { get; set; } = StediEndpointDefaults.PayersBaseUrl;

    /// <summary>Base URL for eligibility-manager APIs.</summary>
    public string ManagerBaseUrl { get; set; } = StediEndpointDefaults.ManagerBaseUrl;

    /// <summary>Base URL for core transaction APIs.</summary>
    public string CoreBaseUrl { get; set; } = StediEndpointDefaults.CoreBaseUrl;

    /// <summary>Base URL for events APIs.</summary>
    public string EventsBaseUrl { get; set; } = StediEndpointDefaults.EventsBaseUrl;

    /// <summary>HTTP request timeout. Defaults to 100 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>
    /// When true, failed HTTP calls that are safe to retry may be retried according to <see cref="MaxRetries"/>.
    /// Disabled by default because retrying claim submissions and other non-idempotent calls can create duplicates.
    /// </summary>
    public bool EnableRetries { get; set; }

    /// <summary>Maximum additional attempts when <see cref="EnableRetries"/> is true. Defaults to 2.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// When true, the SDK may log request and response bodies at the Trace level.
    /// Healthcare payloads can contain PHI/PII. Leave this disabled unless you are in a compliant debug environment.
    /// Authorization headers and API keys are never logged.
    /// </summary>
    public bool EnableSensitiveBodyLogging { get; set; }
}
