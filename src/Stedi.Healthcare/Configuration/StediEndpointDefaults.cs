namespace Stedi.Healthcare.Configuration;

/// <summary>
/// Default base URLs taken from the official Stedi OpenAPI specifications.
/// </summary>
public static class StediEndpointDefaults
{
    /// <summary>Healthcare clearinghouse APIs (eligibility, claims, COB, insurance discovery, reports, PDFs).</summary>
    public const string HealthcareBaseUrl = "https://healthcare.us.stedi.com/2024-04-01";

    /// <summary>CAQH CORE SOAP eligibility endpoint host/version. Path is <c>/protocols/caqh-core</c>.</summary>
    public const string SoapEligibilityBaseUrl = "https://healthcare.us.stedi.com/2025-06-01";

    /// <summary>Claim attachments API.</summary>
    public const string ClaimsBaseUrl = "https://claims.us.stedi.com/2025-03-07";

    /// <summary>Transaction enrollment API (providers, enrollments, documents, tasks).</summary>
    public const string EnrollmentsBaseUrl = "https://enrollments.us.stedi.com/2024-09-01";

    /// <summary>Payer network API.</summary>
    public const string PayersBaseUrl = "https://payers.us.stedi.com/2024-04-01";

    /// <summary>Eligibility manager API (batch eligibility and eligibility PDFs).</summary>
    public const string ManagerBaseUrl = "https://manager.us.stedi.com/2024-04-01";

    /// <summary>Core transaction and file-execution APIs used by Healthcare workflows.</summary>
    public const string CoreBaseUrl = "https://core.us.stedi.com/2023-08-01";

    /// <summary>Events API.</summary>
    public const string EventsBaseUrl = "https://events.us.stedi.com/2026-02-01";
}
