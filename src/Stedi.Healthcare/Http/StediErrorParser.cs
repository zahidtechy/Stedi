using System.Net;
using System.Text.Json;
using Stedi.Healthcare.Serialization;

namespace Stedi.Healthcare.Http;

internal static class StediErrorParser
{
    public static StediApiException CreateException(HttpStatusCode statusCode, string? body, HttpResponseMessage response)
    {
        string? code = null;
        string? message = null;
        var errors = new List<StediApiError>();

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                code = ReadString(root, "error") ?? ReadString(root, "code");
                message = ReadString(root, "message") ?? ReadString(root, "error");

                if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in errorsElement.EnumerateArray())
                    {
                        errors.Add(new StediApiError
                        {
                            Code = ReadString(item, "error") ?? ReadString(item, "code"),
                            Message = ReadString(item, "message") ?? ReadString(item, "error"),
                            Raw = item.GetRawText(),
                        });
                    }
                }
            }
            catch (JsonException)
            {
                // SOAP and PDF error bodies are not JSON. Preserve the raw text.
            }
        }

        message ??= $"Stedi API request failed with HTTP {(int)statusCode} ({statusCode}).";

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            return new StediRateLimitException(message, statusCode, code, body, errors, RetryAfterParser.Parse(response));
        }

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return new StediAuthenticationException(message, statusCode, code, body, errors);
        }

        if (statusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
        {
            return new StediValidationException(message, statusCode, code, body, errors);
        }

        return new StediApiException(message, statusCode, code, body, errors);
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }
}
