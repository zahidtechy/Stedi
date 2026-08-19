using System.Net;

namespace Stedi.Healthcare;

/// <summary>
/// Per-call options such as idempotency keys and CMS traceability headers.
/// </summary>
public sealed class StediCallOptions
{
    /// <summary>
    /// Value for the <c>Idempotency-Key</c> header. Supported by claim submission endpoints.
    /// Reusing the same key with a different body within 24 hours returns HTTP 422.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Value for the <c>X-Forwarded-For</c> header. Required by CMS for some eligibility traffic.
    /// </summary>
    public string? ForwardedFor { get; set; }

    /// <summary>Additional headers to send with the request. Authorization cannot be overridden.</summary>
    public IDictionary<string, string>? AdditionalHeaders { get; set; }
}

/// <summary>
/// A parsed Stedi API error object.
/// </summary>
public sealed class StediApiError
{
    /// <summary>Error classification code when present.</summary>
    public string? Code { get; init; }

    /// <summary>Human-readable message when present.</summary>
    public string? Message { get; init; }

    /// <summary>Raw JSON for this error item.</summary>
    public string? Raw { get; init; }
}

/// <summary>
/// Base exception thrown when a Stedi API request fails.
/// </summary>
public class StediApiException : Exception
{
    /// <summary>Initializes a new exception.</summary>
    public StediApiException(string message, HttpStatusCode statusCode, string? errorCode = null, string? responseBody = null, IReadOnlyList<StediApiError>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorMessage = message;
        ResponseBody = responseBody;
        Errors = errors ?? Array.Empty<StediApiError>();
    }

    /// <summary>HTTP status code returned by Stedi.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Stedi error code from <c>error</c> or <c>code</c>.</summary>
    public string? ErrorCode { get; }

    /// <summary>Human-readable error message.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Raw response body. May contain PHI if the caller logs it; the SDK does not log it by default.</summary>
    public string? ResponseBody { get; }

    /// <summary>Parsed error objects when the response contains multiple errors.</summary>
    public IReadOnlyList<StediApiError> Errors { get; }
}

/// <summary>Thrown for HTTP 401 or 403 responses.</summary>
public sealed class StediAuthenticationException : StediApiException
{
    /// <summary>Initializes a new authentication exception.</summary>
    public StediAuthenticationException(string message, HttpStatusCode statusCode, string? errorCode = null, string? responseBody = null, IReadOnlyList<StediApiError>? errors = null)
        : base(message, statusCode, errorCode, responseBody, errors)
    {
    }
}

/// <summary>Thrown for HTTP 400 or 422 validation failures.</summary>
public sealed class StediValidationException : StediApiException
{
    /// <summary>Initializes a new validation exception.</summary>
    public StediValidationException(string message, HttpStatusCode statusCode, string? errorCode = null, string? responseBody = null, IReadOnlyList<StediApiError>? errors = null)
        : base(message, statusCode, errorCode, responseBody, errors)
    {
    }
}

/// <summary>Thrown for HTTP 429 rate or concurrency limit responses.</summary>
public sealed class StediRateLimitException : StediApiException
{
    /// <summary>Initializes a new rate-limit exception.</summary>
    public StediRateLimitException(
        string message,
        HttpStatusCode statusCode,
        string? errorCode = null,
        string? responseBody = null,
        IReadOnlyList<StediApiError>? errors = null,
        TimeSpan? retryAfter = null)
        : base(message, statusCode, errorCode, responseBody, errors)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>Parsed <c>Retry-After</c> header when present.</summary>
    public TimeSpan? RetryAfter { get; }
}
