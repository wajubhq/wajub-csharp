using System.Text.Json;

namespace Wajub.Tests;

public class RefundTests
{
    [Fact]
    public async Task RefundAsyncRefundsThePaymentAndReturnsARefund()
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (201, Json("""
                {"code":201,"refund":{"id":"rfd_1","transaction":"trx_1","amount":12.5,"currency":"GHS","status":"pending","reason":"requested_by_customer"}}
                """)),
        ]);
        var client = TestFixtures.CreateWajubClient(httpClient);

        var parameters = new Dictionary<string, object?>
        {
            ["amount"] = 12.5m,
            ["reason"] = "requested_by_customer",
        };
        Refund refund = await client.Payments.RefundAsync("trx_1", parameters);

        Assert.Equal(HttpMethod.Post.Method, recorded.Method);
        Assert.Equal("/refunds", recorded.Path);
        Assert.StartsWith("wajub-", recorded.IdempotencyKey);
        using (var sent = JsonDocument.Parse(recorded.Bodies.Single()))
        {
            Assert.Equal("trx_1", sent.RootElement.GetProperty("payment").GetString());
            Assert.Equal("requested_by_customer", sent.RootElement.GetProperty("reason").GetString());
            Assert.Equal(12.5m, sent.RootElement.GetProperty("amount").GetDecimal());
        }

        // The caller's parameters are left as they were.
        Assert.False(parameters.ContainsKey("payment"));

        Assert.Equal("rfd_1", refund.Id);
        Assert.Equal("pending", refund.Status);
        Assert.Equal(12.5m, refund.AmountDecimal);
        Assert.Equal("GHS", refund.Currency);
    }

    [Fact]
    public async Task RefundAsyncSendsYourIdempotencyKey()
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (201, Json("""{"refund":{"id":"rfd_1","amount":5000,"status":"pending"}}""")),
        ]);
        var client = TestFixtures.CreateWajubClient(httpClient);

        await client.Payments.RefundAsync(
            "trx_1",
            new Dictionary<string, object?> { ["reason"] = "duplicate" },
            new RequestOptions { IdempotencyKey = "refund-order-1042" });

        Assert.Equal("refund-order-1042", recorded.IdempotencyKey);
    }

    [Fact]
    public async Task RetrieveRefundAsyncReturnsARefundWithItsDecimals()
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (200, Json("""{"code":200,"refund":{"id":"rfd_1","amount":7.25,"currency":"GHS","status":"succeeded"}}""")),
        ]);
        var client = TestFixtures.CreateWajubClient(httpClient);

        Refund refund = await client.Payments.RetrieveRefundAsync("rfd_1");

        Assert.Equal(HttpMethod.Get.Method, recorded.Method);
        Assert.Equal("/refunds/rfd_1", recorded.Path);
        Assert.Equal(string.Empty, recorded.IdempotencyKey);
        Assert.Equal("rfd_1", refund.Id);
        Assert.Equal("succeeded", refund.Status);
        Assert.Equal(7.25m, refund.AmountDecimal);
#pragma warning disable CS0618 // The deprecated Amount keeps the value it always had.
        Assert.Equal(7L, refund.Amount);
#pragma warning restore CS0618
    }

    [Fact]
    public async Task RefundCurrencyIsTheCurrencyOfTheRefund()
    {
        var (httpClient, _) = TestFixtures.CreateTestClient([
            _ => (200, Json("""{"refund":{"id":"rfd_1","amount":15000,"currency":"XAF","status":"succeeded"}}""")),
            _ => (200, Json("""{"refund":{"id":"rfd_2","amount":5,"status":"pending"}}""")),
        ]);
        var client = TestFixtures.CreateWajubClient(httpClient);

        Assert.Equal("XAF", (await client.Payments.RetrieveRefundAsync("rfd_1")).Currency);
        Assert.Equal(string.Empty, (await client.Payments.RetrieveRefundAsync("rfd_2")).Currency);
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
