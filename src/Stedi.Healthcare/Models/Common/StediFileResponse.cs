using System.Net.Http.Headers;

namespace Stedi.Healthcare;

/// <summary>
/// A file or document response. Dispose the instance to release the underlying HTTP connection.
/// The content stream remains readable until disposal.
/// </summary>
public sealed class StediFileResponse : IDisposable, IAsyncDisposable
{
    private readonly IDisposable _lifetime;
    private bool _disposed;

    /// <summary>Initializes a file response.</summary>
    public StediFileResponse(Stream content, string? contentType, string? fileName, HttpResponseHeaders? headers, IDisposable lifetime)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentType = contentType;
        FileName = fileName;
        Headers = headers;
        _lifetime = lifetime;
    }

    /// <summary>Response body stream. Do not dispose this stream separately; dispose the <see cref="StediFileResponse"/>.</summary>
    public Stream Content { get; }

    /// <summary>Response <c>Content-Type</c> when present.</summary>
    public string? ContentType { get; }

    /// <summary>File name from <c>Content-Disposition</c> when present.</summary>
    public string? FileName { get; }

    /// <summary>HTTP response headers when the payload came from an HTTP response.</summary>
    public HttpResponseHeaders? Headers { get; }

    /// <summary>Reads the entire payload into memory. Prefer streaming <see cref="Content"/> for large files.</summary>
    public async Task<byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await Content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <summary>Reads the payload as text.</summary>
    public async Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(Content, leaveOpen: true);
#if NET7_0_OR_GREATER
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#else
        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadToEndAsync().ConfigureAwait(false);
#endif
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Content.Dispose();
        _lifetime.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
