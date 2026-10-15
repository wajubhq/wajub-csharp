using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

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

        Assert.StartsWith("wajub-", Assert.Single(Assert.Single(recorded.IdempotencyKeyValues)));
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

        Assert.Equal(["custom-key-123"], Assert.Single(recorded.IdempotencyKeyValues));
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

        Assert.Empty(Assert.Single(recorded.IdempotencyKeyValues));
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
            _ => (200, new Dictionary<string, object?>
            {
                ["refunds"] = new[] { new Dictionary<string, object?> { ["id"] = "ref_new" } },
                ["meta"] = new Dictionary<string, object?> { ["has_more"] = false },
            }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);

        var created = await client.Refunds.CreateAsync(new Dictionary<string, object?>
        {
            ["payment"] = "pay_1",
            ["amount"] = 500,
        });
        using (var sent = JsonDocument.Parse(recorded.Bodies.Single()))
        {
            Assert.Equal("pay_1", sent.RootElement.GetProperty("payment").GetString());
        }

        var refund = Refund.Parse(new Dictionary<string, object?> { ["refund"] = created });
        Assert.Equal("ref_new", refund.Id);

        var retrieved = await client.Refunds.RetrieveAsync("ref_new");
        var parsed = Refund.Parse(new Dictionary<string, object?> { ["refund"] = retrieved });
        Assert.Equal("pending", parsed.Status);

        var listed = await client.Refunds.ListAsync();
        Assert.Equal(HttpMethod.Get.Method, recorded.Method);
        Assert.Equal("/refunds", recorded.Path);
        Assert.Equal("ref_new", JsonUtils.ToStringValue(Assert.Single(listed.Data)["id"]));
    }

    [Fact]
    public async Task LinksUpdateAndDelete()
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (200, new Dictionary<string, object?> { ["link"] = new Dictionary<string, object?> { ["id"] = "lnk_1", ["amount"] = 2500 } }),
            _ => (200, new Dictionary<string, object?>()),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);

        var updated = await client.Links.UpdateAsync("lnk_1", new Dictionary<string, object?> { ["amount"] = 2500 });
        Assert.Equal(HttpMethod.Put.Method, recorded.Method);
        Assert.Equal("/links/lnk_1", recorded.Path);
        using (var sent = JsonDocument.Parse(Assert.Single(recorded.Bodies)))
        {
            Assert.Equal(2500, sent.RootElement.GetProperty("amount").GetInt32());
        }

        Assert.Single(recorded.IdempotencyKeyValues[0]);
        Assert.Equal("lnk_1", JsonUtils.ToStringValue(updated["id"]));

        await client.Links.DeleteAsync("lnk_1");
        Assert.Equal(HttpMethod.Delete.Method, recorded.Method);
        Assert.Equal("/links/lnk_1", recorded.Path);
        // No body: the update's is still the only one.
        Assert.Single(recorded.Bodies);
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
        Assert.Single(recorded.IdempotencyKeyAttempts.Distinct());
        Assert.All(recorded.IdempotencyKeyValues, keys => Assert.Single(keys));
    }

    [Fact]
    public async Task RetriesA409ForAnIdempotencyKeyStillBeingProcessed()
    {
        // A call sent again after a timeout can reach the API while the first
        // one is still running: the answer is on its way, so ask again.
        const string message = "A request with this Idempotency-Key is still being processed.";
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (409, new Dictionary<string, object?>
            {
                ["message"] = message,
                ["errors"] = new Dictionary<string, object?> { ["idempotency_key"] = new[] { message } },
            }),
            _ => (201, new Dictionary<string, object?> { ["transaction"] = new Dictionary<string, object?> { ["id"] = "trx_1" } }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        await client.Payments.CreateAsync(
            new Dictionary<string, object?> { ["amount"] = 1000 },
            new RequestOptions { IdempotencyKey = "ORDER-42" });

        Assert.Equal(["ORDER-42", "ORDER-42"], recorded.IdempotencyKeyAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), RetryPolicy.IdempotencyConflictDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(4), RetryPolicy.IdempotencyConflictDelay(2));
    }

    [Fact]
    public async Task RaisesAnyOther409AtOnce()
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([
            _ => (409, new Dictionary<string, object?>
            {
                ["error_code"] = "payment_in_progress",
                ["message"] = "A charge is already in progress.",
            }),
            _ => (200, new Dictionary<string, object?> { ["ok"] = true }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);

        await Assert.ThrowsAnyAsync<WajubError>(() =>
            client.Payments.ProcessAsync("trx_1", new Dictionary<string, object?> { ["channel"] = "cm.mtn" }));
        Assert.Single(recorded.IdempotencyKeyAttempts);
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

    [Fact]
    public async Task InvalidRequestError_KeepsEveryMessageOfAField()
    {
        var (httpClient, _) = TestFixtures.CreateTestClient([
            _ => (422, new Dictionary<string, object?>
            {
                ["message"] = "The given data was invalid.",
                ["errors"] = new Dictionary<string, object?>
                {
                    ["amount"] = new[] { "The amount must be a number.", "The amount must be at least 1." },
                    ["currency"] = new[] { "The currency field is required." },
                    ["email"] = "The email is not valid.",
                },
            }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        var ex = await Assert.ThrowsAsync<InvalidRequestError>(() =>
            client.Payments.CreateAsync(new Dictionary<string, object?> { ["amount"] = "x" }));

        Assert.NotNull(ex.FieldErrors);
        Assert.Equal(["The amount must be a number.", "The amount must be at least 1."], ex.FieldErrors["amount"]);
        Assert.Equal(["The currency field is required."], ex.FieldErrors["currency"]);
        Assert.Equal(["The email is not valid."], ex.FieldErrors["email"]);

        // Errors keeps the first message of each field, as before.
        Assert.Equal("The amount must be a number.", ex.Errors?["amount"]);
        Assert.Equal("The currency field is required.", ex.Errors?["currency"]);
        Assert.Equal("The email is not valid.", ex.Errors?["email"]);
    }

    [Fact]
    public async Task FieldErrors_NullMessagesAreAnEmptyList()
    {
        var (httpClient, _) = TestFixtures.CreateTestClient([
            _ => (422, new Dictionary<string, object?>
            {
                ["message"] = "The given data was invalid.",
                ["errors"] = new Dictionary<string, object?> { ["email"] = null },
            }),
        ]);

        var client = TestFixtures.CreateWajubClient(httpClient);
        var ex = await Assert.ThrowsAsync<InvalidRequestError>(() =>
            client.Payments.CreateAsync(new Dictionary<string, object?> { ["amount"] = 1 }));

        Assert.NotNull(ex.FieldErrors);
        Assert.Empty(ex.FieldErrors["email"]);
        Assert.Equal(string.Empty, ex.Errors?["email"]);
    }

    [Fact]
    public void ErrorFromResponse_FieldErrorsOnEveryErrorType()
    {
        var body = new Dictionary<string, object?>
        {
            ["errors"] = new Dictionary<string, object?> { ["key"] = new[] { "First.", "Second." } },
        };

        foreach (var status in new[] { 400, 401, 403, 404, 422, 429, 500 })
        {
            var error = ErrorFactory.FromResponse(status, body, 0);
            Assert.Equal(["First.", "Second."], error.FieldErrors?["key"]);
            Assert.Equal("First.", error.Errors?["key"]);
        }

        Assert.Null(ErrorFactory.FromResponse(422, new Dictionary<string, object?> { ["message"] = "No fields" }, 0).FieldErrors);
    }
}
