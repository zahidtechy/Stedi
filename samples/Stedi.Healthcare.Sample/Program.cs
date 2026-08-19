using Stedi.Healthcare;
using Stedi.Healthcare.Models;

namespace Stedi.Healthcare.Sample;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Stedi Healthcare SDK sample");
        Console.WriteLine("This application does not send a live transaction on startup.");
        Console.WriteLine();

        var apiKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("Set STEDI_API_KEY to send a real request. Using a local dry-run instead.");
            ShowDryRun();
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var client = new StediHealthcareClient(new StediHealthcareOptions
        {
            ApiKey = apiKey,
        });

        var request = CreateSyntheticEligibilityRequest();
        try
        {
            //var response = await client.Eligibility.CheckAsync(request, cts.Token).ConfigureAwait(false);
            var response = await client.Payers.ListAsync(new ListPayersRequest()).ConfigureAwait(false);
            
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("The request was canceled.");
        }
        catch (StediApiException ex)
        {
            Console.WriteLine($"Stedi API error HTTP {(int)ex.StatusCode}: {ex.ErrorCode} {ex.ErrorMessage}");
        }
    }

    private static void ShowDryRun()
    {
        var request = CreateSyntheticEligibilityRequest();
        Console.WriteLine("Synthetic eligibility request (not sent):");
        Console.WriteLine($"  Payer: {request.TradingPartnerServiceId}");
        Console.WriteLine($"  Member: {request.Subscriber?.MemberId}");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  set STEDI_API_KEY=YOUR_STEDI_API_KEY");
        Console.WriteLine("  dotnet run --project samples/Stedi.Healthcare.Sample");
    }

    private static EligibilityCheckRequest CreateSyntheticEligibilityRequest()
        => new()
        {
            TradingPartnerServiceId = "60054",
            ControlNumber = "111111111",
            TradingPartnerName = "Synthetic Test Payer",
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
        };
}
