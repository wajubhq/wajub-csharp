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
}
