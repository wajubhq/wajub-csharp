using System.Text;

namespace Wajub.Tests;

public class WebhooksTests
{
    [Fact]
    public void ConstructEvent_RejectsInvalidSignatureFormat()
    {
        var webhooks = new Webhooks { Secret = "whsec_test" };

        Assert.Throws<WebhookSignatureVerificationError>(() =>
            webhooks.ConstructEvent("{}"u8.ToArray(), "bad", "1700000000"));
    }

    [Fact]
    public void ConstructEvent_RejectsOldTimestamp()
    {
        var secret = "whsec_test";
        var payload = """{"event":"payment.succeeded"}""";
        var oldTs = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var sig = TestFixtures.SignWebhook(secret, payload, oldTs);

        var webhooks = new Webhooks { Secret = secret };

        Assert.Throws<WebhookSignatureVerificationError>(() =>
            webhooks.ConstructEvent(Encoding.UTF8.GetBytes(payload), sig, oldTs.ToString(), TimeSpan.FromSeconds(300)));
    }

    [Fact]
    public void ConstructEvent_VerifiesSignature()
    {
        var secret = "whsec_test_secret";
        var payload = """{"event":"payment.succeeded","data":{}}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = "v1=" + Crypto.HmacSha256(secret, $"{timestamp}.{payload}");

        var webhooks = new Webhooks { Secret = secret };
        var eventPayload = webhooks.ConstructEvent(Encoding.UTF8.GetBytes(payload), signature, timestamp);

        Assert.Equal("payment.succeeded", JsonUtils.ToStringValue(eventPayload["event"]));
    }

    [Fact]
    public void ConstructEvent_RejectsWrongSignature()
    {
        var webhooks = new Webhooks { Secret = "whsec_test" };
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        Assert.Throws<WebhookSignatureVerificationError>(() =>
            webhooks.ConstructEvent("{}"u8.ToArray(), "v1=deadbeef", timestamp));
    }

    [Fact]
    public void ConstructEvent_ReturnsPlainValues()
    {
        // The documented `evt["event"] as string == "payment.succeeded"` has to match.
        var secret = "whsec_test_secret";
        var payload = """{"id":"evt_1","event":"payment.succeeded","livemode":false,"pending_webhooks":2,"data":{"id":"trx_1","amount":1500.5,"items":[{"name":"Tee"}]}}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = "v1=" + Crypto.HmacSha256(secret, $"{timestamp}.{payload}");

        var evt = new Webhooks { Secret = secret }.ConstructEvent(Encoding.UTF8.GetBytes(payload), signature, timestamp);

        Assert.Equal("payment.succeeded", evt["event"] as string);
        Assert.Equal(false, evt["livemode"]);
        Assert.Equal(2L, evt["pending_webhooks"]);
        var data = Assert.IsType<Dictionary<string, object?>>(evt["data"]);
        Assert.Equal("trx_1", data["id"] as string);
        Assert.Equal(1500.5, data["amount"]);
        var items = Assert.IsType<List<object?>>(data["items"]);
        Assert.Equal("Tee", Assert.IsType<Dictionary<string, object?>>(items[0])["name"]);
    }

    [Theory]
    [InlineData(null, "1700000000")]
    [InlineData("v1=deadbeef", null)]
    [InlineData("", "1700000000")]
    public void ConstructEvent_AnswersAMissingHeaderWithASignatureError(string? signature, string? timestamp)
    {
        var webhooks = new Webhooks { Secret = "whsec_test" };

        Assert.Throws<WebhookSignatureVerificationError>(() =>
            webhooks.ConstructEvent("{}"u8.ToArray(), signature, timestamp));
    }
}
