# Stedi Healthcare .NET SDK

A production-ready, strongly typed .NET 6 SDK for the [Stedi Healthcare API](https://www.stedi.com/docs/healthcare/api-reference). Models and endpoints are derived from Stedi's official OpenAPI specifications.

The library is designed for ASP.NET Core, worker services, console apps, and other .NET 6 applications. It supports JSON, raw X12, PDF/file downloads, pre-signed uploads, pagination, structured errors, cancellation, and `IHttpClientFactory`.

## Installation

GitHub Packages (published from this repository):

```bash
dotnet add package Stedi.Healthcare --source https://nuget.pkg.github.com/zahidtechy/index.json
```

GitHub Packages requires a GitHub username and a personal access token with `read:packages` when restoring.

Once published to nuget.org:

```bash
dotnet add package Stedi.Healthcare
```

## Configuration

```json
{
  "Stedi": {
    "ApiKey": "YOUR_STEDI_API_KEY"
  }
}
```

Never commit a real API key. Test keys can be used with Stedi's documented mock eligibility requests. Production keys send live healthcare transactions.

## Dependency injection

```csharp
builder.Services.AddStediHealthcare(options =>
{
    options.ApiKey = builder.Configuration["Stedi:ApiKey"]
        ?? throw new InvalidOperationException("Stedi API key is missing.");
});
```

Inject `IStediHealthcareClient` or an individual API interface such as `IEligibilityClient`.

```csharp
public sealed class EligibilityService
{
    private readonly IStediHealthcareClient _stedi;

    public EligibilityService(IStediHealthcareClient stedi)
    {
        _stedi = stedi;
    }
}
```

## Direct client creation

```csharp
using var client = new StediHealthcareClient(new StediHealthcareOptions
{
    ApiKey = "YOUR_STEDI_API_KEY"
});
```

Authentication is applied automatically through the `Authorization` header. Callers never add the header themselves.

## Authentication

Stedi expects the API key in the `Authorization` header. The SDK also accepts Stedi's legacy `Key ` prefix if you include it in the configured value, but typical usage is the raw key.

SOAP eligibility is different: CAQH CORE requests authenticate with WS-Security inside the SOAP envelope (`Username` = Stedi account ID, `Password` = API key). Set `StediHealthcareOptions.AccountId` if you want the SDK to build that envelope.

## Eligibility example

```csharp
var response = await client.Eligibility.CheckAsync(
    new EligibilityCheckRequest
    {
        TradingPartnerServiceId = "60054",
        Provider = new Provider
        {
            Npi = "1999999984",
            OrganizationName = "Sample Clinic",
        },
        Subscriber = new RequestSubscriber
        {
            MemberId = "SYNTHETICMEMBER",
            FirstName = "Alex",
            LastName = "Sample",
            DateOfBirth = "19800101",
        },
        Encounter = new Encounter
        {
            ServiceTypeCodes = new[] { "30" },
        },
    },
    cancellationToken);
```

## Professional claim example

```csharp
var claim = await client.Claims.SubmitProfessionalAsync(
    request,
    new StediCallOptions { IdempotencyKey = Guid.NewGuid().ToString() },
    cancellationToken);
```

## Raw X12 example

```csharp
var eligibility = await client.Eligibility.CheckRawX12Async(x12, cancellationToken);
var claim = await client.Claims.SubmitProfessionalX12Async(x12, cancellationToken);
```

Raw X12 endpoints still use JSON on the wire: `{ "x12": "ISA*..." }`. The SDK accepts a string and wraps it for you.

## Payer example

```csharp
var payer = await client.Payers.GetAsync("STEDI-PAYER-ID", cancellationToken);
var page = await client.Payers.ListAsync(new ListPayersRequest { PageSize = 100 }, cancellationToken);
```

## Pagination example

List methods return the official page payload, including `nextPageToken`. Convenience enumerators follow pages for you:

```csharp
await foreach (var transaction in client.Transactions.GetAllAsync(cancellationToken))
{
    Console.WriteLine(transaction.TransactionId);
}
```

## File and PDF download example

```csharp
await using var pdf = await client.Remittances.Get835PdfAsync(transactionId, cancellationToken);
await using var output = File.Create("era.pdf");
await pdf.Content.CopyToAsync(output, cancellationToken);
```

Enrollment and claim-attachment uploads use Stedi pre-signed URLs, not `multipart/form-data`. Pass a `Stream`; the SDK `PUT`s it to the pre-signed URL without forwarding your API key.

## Error handling

```csharp
try
{
    await client.Eligibility.CheckAsync(request, cancellationToken);
}
catch (StediRateLimitException ex)
{
    Console.WriteLine($"Throttled. Retry after {ex.RetryAfter}");
}
catch (StediAuthenticationException ex)
{
    Console.WriteLine($"Auth failed: {ex.ErrorMessage}");
}
catch (StediValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.ErrorMessage}");
}
catch (StediApiException ex)
{
    Console.WriteLine($"HTTP {(int)ex.StatusCode}: {ex.ErrorCode} {ex.ErrorMessage}");
}
```

Automatic retries are **off** by default. Enable `StediHealthcareOptions.EnableRetries` only for idempotent GET traffic; claim submission is not retried automatically.

## Cancellation

Every async public method accepts `CancellationToken`.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await client.Payers.ListAsync(cancellationToken: cts.Token);
```

## ASP.NET Core integration

```csharp
builder.Services.AddStediHealthcare(options =>
{
    options.ApiKey = builder.Configuration["Stedi:ApiKey"]
        ?? throw new InvalidOperationException("Stedi API key is missing.");
    options.Timeout = TimeSpan.FromSeconds(100);
});
```

The registration uses `IHttpClientFactory` with a dedicated download client so pre-signed S3 URLs never receive the `Authorization` header.

## Security / PHI considerations

- Do not log API keys, `Authorization` headers, or healthcare payloads.
- Body logging is disabled unless you set `EnableSensitiveBodyLogging = true`. That option is for compliant debug environments only.
- Tests and samples use synthetic data only.
- Pre-signed uploads/downloads intentionally omit the Stedi API key.

## Supported APIs

| Area | Client |
| --- | --- |
| Payers | `client.Payers` |
| Providers | `client.Providers` |
| Enrollments, documents, tasks | `client.Enrollments` |
| Eligibility JSON, raw X12, SOAP, batch, PDF | `client.Eligibility` |
| Insurance discovery | `client.InsuranceDiscovery` |
| Coordination of benefits | `client.CoordinationOfBenefits` |
| Professional, dental, institutional claims + CMS-1500 PDFs | `client.Claims` |
| Claim attachments | `client.Attachments` |
| 277CA acknowledgments | `client.ClaimAcknowledgments` |
| 835 ERA JSON and PDF | `client.Remittances` |
| Real-time claim status | `client.ClaimStatus` |
| Transactions and file execution input/output | `client.Transactions` |
| Events | `client.Events` |

## Building locally

The library targets **net6.0**.

```bash
dotnet restore
dotnet build -c Release
dotnet pack src/Stedi.Healthcare/Stedi.Healthcare.csproj -c Release -o ./artifacts
```

Package version is `1.0.0` via `Directory.Build.props` (`VersionPrefix`). Change that value before publishing a new version.

## Publishing to NuGet

Pushing a version tag (or running **nuget-publish** manually) publishes to GitHub Packages and nuget.org. nuget.org uses Trusted Publishing for account `zahid94` from the `production` GitHub Environment — no long-lived API key is stored in the repo.

```bash
git tag v1.0.0
git push origin v1.0.0
```

- GitHub Packages: https://github.com/zahidtechy/Stedi/pkgs/nuget/Stedi.Healthcare
- nuget.org: https://www.nuget.org/packages/Stedi.Healthcare

## Updating OpenAPI models

Official specs are pinned under `openapi/`. Refresh them with:

```powershell
./scripts/update-openapi.ps1
```

Then review generated models in `src/Stedi.Healthcare/Models/Generated`.
