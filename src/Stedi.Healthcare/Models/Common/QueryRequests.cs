using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Models;

/// <summary>Query parameters for listing payers.</summary>
public sealed class ListPayersRequest
{
    /// <summary>Maximum number of payers to return. If omitted, Stedi may return all payers in one response.</summary>
    public int? PageSize { get; set; }

    /// <summary>Token from a previous <c>nextPageToken</c>.</summary>
    public string? PageToken { get; set; }
}

/// <summary>Query parameters for searching payers.</summary>
public sealed class SearchPayersRequest
{
    /// <summary>Free-text query.</summary>
    public string? Query { get; set; }

    /// <summary>Maximum results per page.</summary>
    public int? PageSize { get; set; }

    /// <summary>Token from a previous <c>nextPageToken</c>.</summary>
    public string? PageToken { get; set; }

    /// <summary>Filter by eligibility-check support.</summary>
    public TransactionFilterValue? EligibilityCheck { get; set; }

    /// <summary>Filter by claim-status support.</summary>
    public TransactionFilterValue? ClaimStatus { get; set; }

    /// <summary>Filter by professional claim submission support.</summary>
    public TransactionFilterValue? ProfessionalClaimSubmission { get; set; }

    /// <summary>Filter by dental claim submission support.</summary>
    public TransactionFilterValue? DentalClaimSubmission { get; set; }

    /// <summary>Filter by institutional claim submission support.</summary>
    public TransactionFilterValue? InstitutionalClaimSubmission { get; set; }

    /// <summary>Filter by claim payment (835) support.</summary>
    public TransactionFilterValue? ClaimPayment { get; set; }

    /// <summary>Filter by coordination of benefits support.</summary>
    public TransactionFilterValue? CoordinationOfBenefits { get; set; }

    /// <summary>Filter by unsolicited claim attachment support.</summary>
    public TransactionFilterValue? UnsolicitedClaimAttachment { get; set; }

    /// <summary>Filter by coverage types. Repeatable.</summary>
    public IReadOnlyList<CoverageType>? CoverageTypes { get; set; }

    /// <summary>Filter by operating states. Repeatable.</summary>
    public IReadOnlyList<string>? OperatingStates { get; set; }

    /// <summary>Filter by programs. Repeatable. Present on the dedicated Payers API.</summary>
    public IReadOnlyList<Program>? Programs { get; set; }
}

/// <summary>Query parameters for listing providers.</summary>
public sealed class ListProvidersRequest
{
    public decimal? PageSize { get; set; }
    public string? PageToken { get; set; }
    public string? Filter { get; set; }
    public IReadOnlyList<string>? ProviderNpis { get; set; }
    public IReadOnlyList<string>? ProviderTaxIds { get; set; }
}

/// <summary>Query parameters for listing enrollments.</summary>
public sealed class ListEnrollmentsRequest
{
    public decimal? PageSize { get; set; }
    public string? PageToken { get; set; }
    public string? Filter { get; set; }
    public IReadOnlyList<EnrollmentStatus>? Status { get; set; }
    public IReadOnlyList<string>? ProviderNpis { get; set; }
    public IReadOnlyList<string>? ProviderTaxIds { get; set; }
    public IReadOnlyList<string>? ProviderNames { get; set; }
    public IReadOnlyList<string>? ProviderIds { get; set; }
    public IReadOnlyList<string>? PayerIds { get; set; }
    public IReadOnlyList<EnrollmentSource>? Sources { get; set; }
    public IReadOnlyList<string>? Transactions { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
    public DateTimeOffset? StatusUpdatedFrom { get; set; }
    public DateTimeOffset? StatusUpdatedTo { get; set; }
    public string? ImportId { get; set; }
    public string? RequestedEffectiveDateFrom { get; set; }
    public string? RequestedEffectiveDateTo { get; set; }
    public DateTimeOffset? LastEraReceivedFrom { get; set; }
    public DateTimeOffset? LastEraReceivedTo { get; set; }
    public IReadOnlyList<string>? UserEmails { get; set; }
    public IReadOnlyList<string>? SortBy { get; set; }
}

/// <summary>Query parameters for polling or listing transactions.</summary>
public sealed class ListTransactionsRequest
{
    public int? PageSize { get; set; }
    public string? PageToken { get; set; }
    public string? BusinessIdentifier { get; set; }
    public string? TransactionSetId { get; set; }
    public string? Sender { get; set; }
    public string? Receiver { get; set; }
    public string? Direction { get; set; }
    public string? Mode { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? ElementId { get; set; }
    public string? PartnershipId { get; set; }
}

/// <summary>Query parameters for polling transactions from a start timestamp.</summary>
public sealed class PollTransactionsRequest
{
    public int? PageSize { get; set; }
    public string? PageToken { get; set; }
    public DateTimeOffset? StartDateTime { get; set; }
}

/// <summary>Query parameters for listing events.</summary>
public sealed class ListEventsRequest
{
    public decimal? PageSize { get; set; }
    public string? PageToken { get; set; }
    public string? EventId { get; set; }
    public string? EventType { get; set; }
    public IReadOnlyList<string>? Status { get; set; }

    /// <summary>
    /// Filter by created timestamp using Stedi's <c>operator:ISO-8601</c> format, for example <c>gt:2026-01-01T00:00:00Z</c>.
    /// Repeatable.
    /// </summary>
    public IReadOnlyList<string>? Created { get; set; }
}

/// <summary>Query parameters for polling batch eligibility results.</summary>
public sealed class PollBatchEligibilityRequest
{
    public string? BatchId { get; set; }
    public int? PageSize { get; set; }
    public string? PageToken { get; set; }
    public DateTimeOffset? StartDateTime { get; set; }
}

/// <summary>Query parameters for retrieving batch eligibility item statuses.</summary>
public sealed class GetBatchItemsRequest
{
    public int? PageSize { get; set; }
    public string? PageToken { get; set; }
    public string? State { get; set; }
    public string? EligibilityCheckResult { get; set; }
}

/// <summary>CAQH CORE SOAP eligibility response, including Stedi tracking headers.</summary>
public sealed class SoapEligibilityResponse
{
    public SoapEligibilityResponse(string xml, string? stediId, string? eligibilitySearchId, IReadOnlyDictionary<string, IEnumerable<string>> headers)
    {
        Xml = xml;
        StediId = stediId;
        EligibilitySearchId = eligibilitySearchId;
        Headers = headers;
    }

    /// <summary>SOAP XML response body.</summary>
    public string Xml { get; }

    /// <summary>Value of the <c>stedi-id</c> response header when present.</summary>
    public string? StediId { get; }

    /// <summary>Value of the <c>stedi-eligibility-search-id</c> response header when present.</summary>
    public string? EligibilitySearchId { get; }

    /// <summary>Response headers.</summary>
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }
}

/// <summary>Values used to construct a CAQH CORE vC2.2.0 SOAP eligibility request.</summary>
public sealed class SoapEligibilityRequest
{
    /// <summary>X12 270 payload placed in the SOAP <c>Payload</c> element.</summary>
    public string X12 { get; set; } = string.Empty;

    /// <summary>Unique payload ID. Stedi requires a UUID. Generated when omitted.</summary>
    public string? PayloadId { get; set; }

    /// <summary>Sender ID mapped to ISA06.</summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>Receiver ID mapped to ISA08.</summary>
    public string ReceiverId { get; set; } = string.Empty;

    /// <summary>Request timestamp. Defaults to UTC now.</summary>
    public DateTimeOffset? Timestamp { get; set; }
}
