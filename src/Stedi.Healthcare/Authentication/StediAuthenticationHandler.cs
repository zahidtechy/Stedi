using Microsoft.Extensions.Logging;
using Stedi.Healthcare.Http;

namespace Stedi.Healthcare.Authentication;

/// <summary>
/// Adds the Stedi API key to the <c>Authorization</c> header. The key is never logged.
/// </summary>
internal sealed class StediAuthenticationHandler : DelegatingHandler
{
    internal static readonly HttpRequestOptionsKey<bool> SkipAuthorization = new("Stedi.SkipAuthorization");

    private readonly StediHealthcareOptions _options;
    private readonly ILogger? _logger;

    public StediAuthenticationHandler(StediHealthcareOptions options, ILogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var skip = request.Options.TryGetValue(SkipAuthorization, out var skipped) && skipped;
        if (!skip)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Stedi API key is missing. Set StediHealthcareOptions.ApiKey.");
            }

            if (!request.Headers.Contains("Authorization"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", _options.ApiKey);
            }
        }

        if (_logger is not null && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Sending {Method} {Url}", request.Method, SanitizeUrl(request.RequestUri));
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static string SanitizeUrl(Uri? uri)
    {
        if (uri is null)
        {
            return string.Empty;
        }

        // Never include userinfo; Stedi keys are not placed in URLs by this SDK.
        return uri.GetLeftPart(UriPartial.Path);
    }
}
