using System.Net;
using System.Net.Http.Headers;

namespace Wajub.Tests;

public class ClientTests
{
    [Fact]
    public void Create_RequiresApiKey()
    {
        Environment.SetEnvironmentVariable("WAJUB_API_KEY", null);

        var ex = Assert.Throws<ArgumentException>(() => WajubClient.Create(new WajubConfig()));
        Assert.Contains("api key is required", ex.Message);
    }

    [Fact]
    public void Create_ReadsApiKeyFromEnvironment()
    {
        Environment.SetEnvironmentVariable("WAJUB_API_KEY", TestFixtures.TestApiKey);
        try
        {
            var client = WajubClient.Create(new WajubConfig());
            Assert.NotNull(client);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WAJUB_API_KEY", null);
        }
    }

    [Fact]
    public void Create_AcceptsPublicAndRestrictedKeys()
    {
        Assert.NotNull(WajubClient.Create(new WajubConfig { ApiKey = "pk_test.public" }));
        Assert.NotNull(WajubClient.Create(new WajubConfig { ApiKey = "rk_test.restricted" }));
    }

    [Fact]
    public async Task HttpAuthorizationHeader()
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (200, new Dictionary<string, object?> { ["ok"] = true }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        await client.Global.PingAsync();

        Assert.Equal(TestFixtures.TestApiKey, recorded.Authorization);
    }

    [Fact]
    public async Task HttpPost_IncludesIdempotencyKey()
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (200, new Dictionary<string, object?> { ["transaction"] = new Dictionary<string, object?> { ["id"] = "pay_1" } }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        await client.Payments.CreateAsync(new Dictionary<string, object?> { ["amount"] = 1000 });

        Assert.StartsWith("wajub-", recorded.IdempotencyKey);
    }

    [Fact]
    public async Task HttpPost_HonorsCustomOptions()
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (200, new Dictionary<string, object?> { ["transaction"] = new Dictionary<string, object?> { ["id"] = "pay_1" } }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        await client.Payments.CreateAsync(
            new Dictionary<string, object?> { ["amount"] = 1000 },
            new RequestOptions { IdempotencyKey = "custom-key-123", Sync = "acct_sync_ref" });

        Assert.Equal("custom-key-123", recorded.IdempotencyKey);
        Assert.Equal("acct_sync_ref", recorded.Sync);
    }

    [Fact]
    public async Task HttpGet_DoesNotSendIdempotencyKey()
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (200, new Dictionary<string, object?> { ["transactions"] = Array.Empty<object>() }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        await client.Payments.ListAsync();

        Assert.Equal(string.Empty, recorded.IdempotencyKey);
    }

    [Fact]
    public async Task HttpErrorResponse()
    {
        var (httpClient, _) = TestFixtures.CreateTestClient([
            _ => (422, new Dictionary<string, object?>
            {
                ["message"] = "Unprocessable",
                ["code"] = "validation_error",
            }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        var ex = await Assert.ThrowsAsync<InvalidRequestError>(() =>
            client.Payments.CreateAsync(new Dictionary<string, object?> { ["amount"] = -1 }));

        Assert.Equal(422, ex.HttpStatus);
    }

    [Fact]
    public async Task PaymentsCreate_UnwrapsUrls()
    {
        var (httpClient, _) = TestFixtures.CreateTestClient([
            _ => (200, new Dictionary<string, object?>
            {
                ["transaction"] = new Dictionary<string, object?> { ["id"] = "pay_123", ["status"] = "pending" },
                ["authorization_token"] = "tok_abc",
                ["authorization_url"] = "https://checkout.test/pay/pay_123",
            }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        var payment = await client.Payments.CreateAsync(new Dictionary<string, object?>
        {
            ["amount"] = 5000,
            ["currency"] = "XAF",
        });

        Assert.Equal("pay_123", payment.Id);
        Assert.Equal("tok_abc", payment.AuthorizationToken);
    }

    [Fact]
    public async Task PaymentsInitialize_Alias()
    {
        var (httpClient, _) = TestFixtures.CreateTestClient([
            _ => (200, new Dictionary<string, object?>
            {
                ["transaction"] = new Dictionary<string, object?> { ["id"] = "pay_456" },
                ["authorization_url"] = "https://checkout.test/pay/pay_456",
            }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        var payment = await client.Payments.InitializeAsync(new Dictionary<string, object?>
        {
            ["amount"] = 1000,
            ["currency"] = "XAF",
        });

        Assert.Equal("pay_456", payment.Id);
    }

    [Fact]
    public async Task PaymentsList_Pagination()
    {
        var (httpClient, _) = TestFixtures.CreateTestClient([
            _ => (200, new Dictionary<string, object?>
            {
                ["transactions"] = new[] { new Dictionary<string, object?> { ["id"] = "pay_1" } },
                ["meta"] = new Dictionary<string, object?> { ["current_page"] = 1, ["last_page"] = 2 },
            }),
            _ => (200, new Dictionary<string, object?>
            {
                ["transactions"] = new[] { new Dictionary<string, object?> { ["id"] = "pay_2" } },
                ["meta"] = new Dictionary<string, object?> { ["current_page"] = 2, ["last_page"] = 2 },
            }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        var page1 = await client.Payments.ListAsync(new Dictionary<string, object?> { ["per_page"] = 1 });

        Assert.Equal("pay_1", JsonUtils.ToStringValue(page1.Data[0]["id"]));
        Assert.True(page1.HasMore);

        var page2 = await page1.GetNextPageAsync();
        Assert.Equal("pay_2", JsonUtils.ToStringValue(page2.Data[0]["id"]));
        Assert.False(page2.HasMore);
    }

    [Fact]
    public async Task RefundsCrud()
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (200, new Dictionary<string, object?> { ["refund"] = new Dictionary<string, object?> { ["id"] = "ref_new" } }),
            _ => (200, new Dictionary<string, object?> { ["refund"] = new Dictionary<string, object?> { ["id"] = "ref_new", ["status"] = "pending" } }),
            _ => (200, new Dictionary<string, object?>()),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);

        var created = await client.Refunds.CreateAsync(new Dictionary<string, object?>
        {
            ["payment_id"] = "pay_1",
            ["amount"] = 500,
        });
        var refund = Refund.Parse(new Dictionary<string, object?> { ["refund"] = created });
        Assert.Equal("ref_new", refund.Id);

        var retrieved = await client.Refunds.RetrieveAsync("ref_new");
        var parsed = Refund.Parse(new Dictionary<string, object?> { ["refund"] = retrieved });
        Assert.Equal("pending", parsed.Status);

        await client.Refunds.DeleteAsync("ref_new");
        Assert.Equal(HttpMethod.Delete.Method, recorded.Method);
    }

    [Fact]
    public async Task RetriesOn503ThenSucceeds()
    {
        var (httpClient, _) = TestFixtures.CreateTestClient([
            _ => (503, new Dictionary<string, object?> { ["message"] = "Unavailable" }),
            _ => (200, new Dictionary<string, object?> { ["ok"] = true }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        var result = await client.Global.PingAsync();

        Assert.True(JsonUtils.TryGetBool(result.GetValueOrDefault("ok"), out var ok) && ok);
    }

    [Fact]
    public async Task RetryReusesSameIdempotencyKeyAcrossAttempts()
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (503, new Dictionary<string, object?> { ["message"] = "Unavailable" }),
            _ => (503, new Dictionary<string, object?> { ["message"] = "Unavailable" }),
            _ => (200, new Dictionary<string, object?> { ["transaction"] = new Dictionary<string, object?> { ["id"] = "pay_1" } }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        await client.Payments.CreateAsync(new Dictionary<string, object?> { ["amount"] = 1000 });

        Assert.Equal(3, recorded.IdempotencyKeyAttempts.Count);
        Assert.All(recorded.IdempotencyKeyAttempts, key => Assert.False(string.IsNullOrEmpty(key)));
        Assert.Equal(1, recorded.IdempotencyKeyAttempts.Distinct().Count());
    }

    [Fact]
    public void PickListAndNormalizeApiKey()
    {
        var (data, meta) = JsonUtils.PickList(new Dictionary<string, object?>
        {
            ["refunds"] = new[] { new Dictionary<string, object?> { ["id"] = "ref_1" } },
            ["meta"] = new Dictionary<string, object?> { ["total"] = 1 },
        }, "refunds");

        Assert.Single(data);
        Assert.Equal("ref_1", JsonUtils.ToStringValue(data[0]["id"]));
        Assert.NotNull(meta);
        Assert.Equal("sk_test.abc", JsonUtils.NormalizeApiKey("Bearer sk_test.abc"));
    }

    [Fact]
    public void PickList_FallsBackToItemsKey()
    {
        var (data, meta) = JsonUtils.PickList(new Dictionary<string, object?>
        {
            ["items"] = new[] { new Dictionary<string, object?> { ["id"] = "pay_1" } },
            ["meta"] = new Dictionary<string, object?> { ["total"] = 1 },
        }, "transactions");

        Assert.Single(data);
        Assert.Equal("pay_1", JsonUtils.ToStringValue(data[0]["id"]));
        Assert.NotNull(meta);
    }

    [Fact]
    public void PickResource()
    {
        var body = new Dictionary<string, object?>
        {
            ["transaction"] = new Dictionary<string, object?> { ["id"] = "pay_123" },
        };

        var resource = JsonUtils.PickResource(body, "transaction");
        Assert.Equal("pay_123", JsonUtils.ToStringValue(resource["id"]));
    }

    [Fact]
    public void ErrorFromResponse()
    {
        var error = ErrorFactory.FromResponse(422, new Dictionary<string, object?>
        {
            ["message"] = "Validation failed",
            ["code"] = "validation_error",
            ["errors"] = new Dictionary<string, object?> { ["amount"] = new[] { "Must be positive" } },
        }, 0);

        var invalid = Assert.IsType<InvalidRequestError>(error);
        Assert.Equal("Validation failed", invalid.Message);
        Assert.Equal("validation_error", invalid.Code);
        Assert.Equal(422, invalid.HttpStatus);
        Assert.Equal("Must be positive", invalid.Errors?["amount"]);
    }
}
