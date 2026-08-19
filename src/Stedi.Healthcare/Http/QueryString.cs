using System.Globalization;
using System.Net;
using System.Text;

namespace Stedi.Healthcare.Http;

internal static class QueryStringBuilder
{
    public static string Build(params (string Name, object? Value)[] values)
    {
        var sb = new StringBuilder();
        foreach (var (name, value) in values)
        {
            Append(sb, name, value);
        }

        return sb.Length == 0 ? string.Empty : sb.ToString();
    }

    public static void Append(StringBuilder sb, string name, object? value)
    {
        if (value is null)
        {
            return;
        }

        if (value is string s)
        {
            if (s.Length == 0)
            {
                return;
            }

            AppendPair(sb, name, s);
            return;
        }

        if (value is bool b)
        {
            AppendPair(sb, name, b ? "true" : "false");
            return;
        }

        if (value is DateTimeOffset dto)
        {
            AppendPair(sb, name, dto.ToUniversalTime().ToString("o"));
            return;
        }

        if (value is DateTime dt)
        {
            AppendPair(sb, name, dt.ToUniversalTime().ToString("o"));
            return;
        }

        if (value is Enum e)
        {
            AppendPair(sb, name, e.ToString());
            return;
        }

        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                Append(sb, name, item);
            }

            return;
        }

        AppendPair(sb, name, Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    private static void AppendPair(StringBuilder sb, string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        sb.Append(sb.Length == 0 ? '?' : '&');
        sb.Append(Uri.EscapeDataString(name));
        sb.Append('=');
        sb.Append(Uri.EscapeDataString(value));
    }
}

internal static class StediUri
{
    public static Uri Combine(string baseUrl, string relativePath, string query = "")
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Base URL is required.", nameof(baseUrl));
        }

        var trimmedBase = baseUrl.TrimEnd('/');
        var trimmedPath = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        return new Uri(trimmedBase + trimmedPath + query, UriKind.Absolute);
    }

    public static string Escape(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Path parameter is required.", nameof(value));
        }

        return Uri.EscapeDataString(value);
    }
}

internal static class RetryAfterParser
{
    public static TimeSpan? Parse(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter is null)
        {
            return null;
        }

        if (response.Headers.RetryAfter.Delta is TimeSpan delta)
        {
            return delta;
        }

        if (response.Headers.RetryAfter.Date is DateTimeOffset date)
        {
            var remaining = date - DateTimeOffset.UtcNow;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        return null;
    }
}
