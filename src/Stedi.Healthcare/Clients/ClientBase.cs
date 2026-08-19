using Stedi.Healthcare.Http;

namespace Stedi.Healthcare.Clients;

internal abstract class ClientBase
{
    protected ClientBase(StediHttpPipeline pipeline, StediHealthcareOptions options)
    {
        Pipeline = pipeline;
        Options = options;
    }

    protected StediHttpPipeline Pipeline { get; }

    protected StediHealthcareOptions Options { get; }

    protected static void Ensure(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", paramName);
        }
    }
}
