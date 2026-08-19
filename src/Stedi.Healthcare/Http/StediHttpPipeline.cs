using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Stedi.Healthcare.Authentication;
using Stedi.Healthcare.Serialization;

namespace Stedi.Healthcare.Http;

internal sealed class StediHttpPipeline
{
    private static readonly HashSet<HttpMethod> RetryableMethods = new()
    {
        HttpMethod.Get,
        HttpMethod.Head,
        HttpMethod.Options,
    };

    private readonly HttpClient _apiClient;
    private readonly HttpClient _downloadClient;
    private readonly StediHealthcareOptions _options;
    private readonly ILogger? _logger;

    public StediHttpPipeline(HttpClient apiClient, HttpClient downloadClient, StediHealthcareOptions options, ILogger? logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _downloadClient = downloadClient ?? throw new ArgumentNullException(nameof(downloadClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _apiClient.Timeout = options.Timeout;
        _downloadClient.Timeout = options.Timeout;
        if (!_apiClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _apiClient.DefaultRequestHeaders.UserAgent.ParseAdd("Stedi.Healthcare.DotNet/1.0.0");
        }
    }

    public Task<T> GetJsonAsync<T>(Uri url, StediCallOptions? callOptions, CancellationToken cancellationToken)
        => SendJsonAsync<T>(HttpMethod.Get, url, body: null, callOptions, cancellationToken);

    public Task<T> PostJsonAsync<T>(Uri url, object body, StediCallOptions? callOptions, CancellationToken cancellationToken)
        => SendJsonAsync<T>(HttpMethod.Post, url, body, callOptions, cancellationToken);

    public Task<T> SendJsonAsync<T>(HttpMethod method, Uri url, object? body, StediCallOptions? callOptions, CancellationToken cancellationToken)
        => SendAsync<T>(method, url, body, "application/json", skipAuthorization: false, cancellationToken, callOptions);

    public async Task SendAsync(HttpMethod method, Uri url, object? body, StediCallOptions? callOptions, CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(method, url, body, "application/json", skipAuthorization: false, HttpCompletionOption.ResponseContentRead, callOptions, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> SendTextAsync(HttpMethod method, Uri url, object? body, string? contentType, StediCallOptions? callOptions, CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(method, url, body, contentType, skipAuthorization: false, HttpCompletionOption.ResponseContentRead, callOptions, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    public async Task<StediFileResponse> SendFileAsync(HttpMethod method, Uri url, object? body, string? contentType, string? accept, StediCallOptions? callOptions, CancellationToken cancellationToken)
    {
        var response = await SendCoreAsync(method, url, body, contentType, skipAuthorization: false, HttpCompletionOption.ResponseHeadersRead, callOptions, cancellationToken, accept).ConfigureAwait(false);
        try
        {
            response = await FollowDocumentRedirectAsync(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }

            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return new StediFileResponse(
                stream,
                response.Content.Headers.ContentType?.ToString(),
                GetFileName(response),
                response.Headers,
                response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    public async Task<string> SendSoapAsync(Uri url, string soapXml, string? forwardedFor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(soapXml);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(soapXml, Encoding.UTF8, "application/soap+xml"),
        };
        request.Options.Set(StediAuthenticationHandler.SkipAuthorization, true);
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        }

        using var response = await _apiClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw StediErrorParser.CreateException(response.StatusCode, body, response);
        }

        return body;
    }

    public async Task<HttpResponseMessage> SendSoapRawAsync(Uri url, string soapXml, string? forwardedFor, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(soapXml, Encoding.UTF8, "application/soap+xml"),
        };
        request.Options.Set(StediAuthenticationHandler.SkipAuthorization, true);
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        }

        var response = await _apiClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                response.Dispose();
            }
        }

        return response;
    }

    public async Task PutStreamAsync(Uri url, Stream content, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(content);
        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StreamContent(content),
        };
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        request.Options.Set(StediAuthenticationHandler.SkipAuthorization, true);

        using var response = await _downloadClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw StediErrorParser.CreateException(response.StatusCode, body, response);
        }
    }

    public async Task<StediFileResponse> GetPresignedFileAsync(Uri url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Options.Set(StediAuthenticationHandler.SkipAuthorization, true);
        var response = await _downloadClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        try
        {
            if (!response.IsSuccessStatusCode)
            {
                await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }

            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return new StediFileResponse(
                stream,
                response.Content.Headers.ContentType?.ToString(),
                GetFileName(response),
                response.Headers,
                response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private async Task<T> SendAsync<T>(HttpMethod method, Uri url, object? body, string? contentType, bool skipAuthorization, CancellationToken cancellationToken, StediCallOptions? callOptions)
    {
        using var response = await SendCoreAsync(method, url, body, contentType, skipAuthorization, HttpCompletionOption.ResponseHeadersRead, callOptions, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        if (typeof(T) == typeof(string))
        {
            var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return (T)(object)text;
        }

        var result = await response.Content.ReadFromJsonAsync<T>(StediJsonSerializer.Options, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            throw new StediApiException("The API returned an empty JSON payload.", response.StatusCode, responseBody: null);
        }

        return result;
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpMethod method,
        Uri url,
        object? body,
        string? contentType,
        bool skipAuthorization,
        HttpCompletionOption completionOption,
        StediCallOptions? callOptions,
        CancellationToken cancellationToken,
        string? accept = null)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            using var request = new HttpRequestMessage(method, url);
            if (skipAuthorization)
            {
                request.Options.Set(StediAuthenticationHandler.SkipAuthorization, true);
            }

            ApplyCallOptions(request, callOptions);
            if (!skipAuthorization && !request.Headers.Contains("Authorization"))
            {
                if (string.IsNullOrWhiteSpace(_options.ApiKey))
                {
                    throw new InvalidOperationException("Stedi API key is missing. Set StediHealthcareOptions.ApiKey.");
                }

                request.Headers.TryAddWithoutValidation("Authorization", _options.ApiKey);
            }
            if (!string.IsNullOrWhiteSpace(accept))
            {
                request.Headers.Accept.Clear();
                request.Headers.TryAddWithoutValidation("Accept", accept);
            }

            if (body is not null)
            {
                if (body is string raw && string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase) is false)
                {
                    request.Content = new StringContent(raw, Encoding.UTF8, contentType ?? "text/plain");
                }
                else
                {
                    request.Content = JsonContent.Create(body, options: StediJsonSerializer.Options);
                    if (!string.IsNullOrWhiteSpace(contentType))
                    {
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                    }
                }
            }

            LogBodyIfEnabled("request", body);

            HttpResponseMessage? response = null;
            try
            {
                response = await _apiClient.SendAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
                if (ShouldRetry(method, response.StatusCode, attempt))
                {
                    var delay = RetryAfterParser.Parse(response) ?? TimeSpan.FromMilliseconds(200 * attempt);
                    response.Dispose();
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return response;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && ShouldRetry(method, HttpStatusCode.RequestTimeout, attempt))
            {
                response?.Dispose();
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                response?.Dispose();
                throw;
            }
        }
    }

    private async Task<HttpResponseMessage> FollowDocumentRedirectAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is not HttpStatusCode.MovedPermanently and not HttpStatusCode.Found and not HttpStatusCode.RedirectKeepVerb and not HttpStatusCode.TemporaryRedirect)
        {
            return response;
        }

        var location = response.Headers.Location;
        if (location is null)
        {
            return response;
        }

        if (!location.IsAbsoluteUri && response.RequestMessage?.RequestUri is not null)
        {
            location = new Uri(response.RequestMessage.RequestUri, location);
        }

        response.Dispose();
        return await GetPresignedResponseAsync(location, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> GetPresignedResponseAsync(Uri url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Options.Set(StediAuthenticationHandler.SkipAuthorization, true);
        return await _downloadClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    }

    private bool ShouldRetry(HttpMethod method, HttpStatusCode statusCode, int attempt)
    {
        if (!_options.EnableRetries || attempt > _options.MaxRetries)
        {
            return false;
        }

        if (!RetryableMethods.Contains(method))
        {
            return false;
        }

        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static void ApplyCallOptions(HttpRequestMessage request, StediCallOptions? options)
    {
        if (options is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.IdempotencyKey))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", options.IdempotencyKey);
        }

        if (!string.IsNullOrWhiteSpace(options.ForwardedFor))
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", options.ForwardedFor);
        }

        if (options.AdditionalHeaders is null)
        {
            return;
        }

        foreach (var header in options.AdditionalHeaders)
        {
            if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ThrowApiExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var body = response.Content is null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw StediErrorParser.CreateException(response.StatusCode, body, response);
    }

    private static string? GetFileName(HttpResponseMessage response)
    {
        var disposition = response.Content.Headers.ContentDisposition;
        return disposition?.FileNameStar ?? disposition?.FileName?.Trim('"');
    }

    private void LogBodyIfEnabled(string direction, object? body)
    {
        if (!_options.EnableSensitiveBodyLogging || _logger is null || !_logger.IsEnabled(LogLevel.Trace) || body is null)
        {
            return;
        }

        _logger.LogTrace(
            "Diagnostic {Direction} body logging is enabled and may contain PHI/PII. Payload: {Payload}",
            direction,
            body is string s ? s : StediJsonSerializer.Serialize(body));
    }
}
