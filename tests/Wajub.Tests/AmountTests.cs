using System.Globalization;
using System.Text.Json;

namespace Wajub.Tests;

public class AmountTests
{
    [Theory]
    [InlineData("15000", "XAF", "15000", 15000L)]
    [InlineData("12.5", "GHS", "12.5", 12L)]
    [InlineData("\"12.50\"", "GHS", "12.50", 12L)]
    [InlineData("12.1", "GHS", "12.1", 12L)]
    [InlineData("0.01", "USD", "0.01", 0L)]
    [InlineData("1.234", "KWD", "1.234", 1L)]
    [InlineData("9876543210.99", "NGN", "9876543210.99", 9876543210L)]
    public async Task PaymentAmountKeepsItsDecimals(string sent, string currency, string expected, long whole)
    {
        var (httpClient, _) = TestFixtures.CreateTestClient([
            _ => (200, Json($$$"""{"transaction":{"id":"pay_1","amount":{{{sent}}},"currency":"{{{currency}}}"}}""")),
        ]);

        var payment = await TestFixtures.CreateWajubClient(httpClient).Payments.RetrieveAsync("pay_1");

        Assert.Equal(expected, payment.AmountDecimal.ToString(CultureInfo.InvariantCulture));
#pragma warning disable CS0618 // The deprecated Amount keeps the value it always had.
        Assert.Equal(whole, payment.Amount);
#pragma warning restore CS0618
    }

    [Fact]
    public void RefundAmountKeepsItsDecimals()
    {
        var fromJson = Refund.Parse(JsonUtils.DecodeJson("""{"refund":{"id":"ref_1","amount":7.25}}"""));
        Assert.Equal(7.25m, fromJson.AmountDecimal);
#pragma warning disable CS0618
        Assert.Equal(7L, fromJson.Amount);
#pragma warning restore CS0618

        var fromString = Refund.Parse(new Dictionary<string, object?>
        {
            ["refund"] = new Dictionary<string, object?> { ["id"] = "ref_2", ["amount"] = "0.50" },
        });
        Assert.Equal(0.5m, fromString.AmountDecimal);

        var fromDouble = Refund.Parse(new Dictionary<string, object?>
        {
            ["refund"] = new Dictionary<string, object?> { ["id"] = "ref_3", ["amount"] = 12.1 },
        });
        Assert.Equal(12.1m, fromDouble.AmountDecimal);

        var unreadable = Refund.Parse(new Dictionary<string, object?>
        {
            ["refund"] = new Dictionary<string, object?> { ["id"] = "ref_4", ["amount"] = "not a number" },
        });
        Assert.Equal(0m, unreadable.AmountDecimal);
    }

    [Fact]
    public async Task AmountIsSentWithItsDecimalsWhateverTheCulture()
    {
        // A culture that writes 12,5, as fr-FR does.
        var comma = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        comma.NumberFormat.NumberDecimalSeparator = ",";
        comma.NumberFormat.NumberGroupSeparator = " ";

        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = comma;
        try
        {
            var (httpClient, recorded) = TestFixtures.CreateTestClient([
                _ => (200, Json("""{"transaction":{"id":"pay_1","amount":12.1,"currency":"GHS"}}""")),
                _ => (200, Json("""{"refund":{"id":"ref_1"}}""")),
                _ => (200, Json("""{"transactions":[],"meta":{"current_page":1,"last_page":1}}""")),
            ]);
            var client = TestFixtures.CreateWajubClient(httpClient);

            var payment = await client.Payments.CreateAsync(new Dictionary<string, object?>
            {
                ["amount"] = 12.10m,
                ["currency"] = "GHS",
                ["email"] = "buyer@example.com",
            });
            // The amount read back goes out again unchanged.
            await client.Refunds.CreateAsync(new Dictionary<string, object?>
            {
                ["payment_id"] = payment.Id,
                ["amount"] = payment.AmountDecimal,
            });
            await client.Payments.ListAsync(new Dictionary<string, object?>
            {
                ["amount_min"] = 12.5m,
                ["amount_max"] = 1234.567,
            });

            Assert.Contains("\"amount\":12.10,", recorded.Bodies[0]);
            Assert.Contains("\"amount\":12.1}", recorded.Bodies[1]);
            Assert.Contains("amount_min=12.5", recorded.Query);
            Assert.Contains("amount_max=1234.567", recorded.Query);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
