namespace Wajub.Tests;

public class IdPathTests
{
    private static readonly Dictionary<string, object?> Body = new() { ["amount"] = 1 };

    // Every call that puts an id in the path, with the name of the parameter that holds it.
    private static readonly (string Name, string ParamName, Func<WajubClient, string, Task> Call)[] Calls =
    [
        ("Payments.RetrieveAsync", "id", (c, id) => c.Payments.RetrieveAsync(id)),
        ("Payments.CancelAsync", "id", (c, id) => c.Payments.CancelAsync(id)),
        ("Payments.ProcessAsync", "id", (c, id) => c.Payments.ProcessAsync(id, Body)),
        ("Payments.ProcessSplitAsync", "id", (c, id) => c.Payments.ProcessSplitAsync(id, Body)),
        ("Payments.RetrieveRefundAsync", "refundId", (c, id) => c.Payments.RetrieveRefundAsync(id)),
        ("Payments.ListRefundsAsync", "id", (c, id) => c.Payments.ListRefundsAsync(id)),
        ("Refunds.RetrieveAsync", "id", (c, id) => c.Refunds.RetrieveAsync(id)),
        ("Refunds.UpdateAsync", "id", (c, id) => c.Refunds.UpdateAsync(id, Body)),
        ("Refunds.DeleteAsync", "id", (c, id) => c.Refunds.DeleteAsync(id)),
        ("Transfers.RetrieveAsync", "id", (c, id) => c.Transfers.RetrieveAsync(id)),
        ("Beneficiaries.UpdateAsync", "id", (c, id) => c.Beneficiaries.UpdateAsync(id, Body)),
        ("Links.DeleteAsync", "id", (c, id) => c.Links.DeleteAsync(id)),
        ("Customers.RetrieveAsync", "id", (c, id) => c.Customers.RetrieveAsync(id)),
        ("Customers.BlockAsync", "id", (c, id) => c.Customers.BlockAsync(id)),
        ("Customers.UnblockAsync", "id", (c, id) => c.Customers.UnblockAsync(id)),
        ("Customers.ActivateAsync", "id", (c, id) => c.Customers.ActivateAsync(id)),
        ("Customers.DeactivateAsync", "id", (c, id) => c.Customers.DeactivateAsync(id)),
        ("Customers.ListTaxIdsAsync", "customerId", (c, id) => c.Customers.ListTaxIdsAsync(id)),
        ("Customers.CreateTaxIdAsync", "customerId", (c, id) => c.Customers.CreateTaxIdAsync(id, Body)),
        ("Customers.DeleteTaxIdAsync customer", "customerId", (c, id) => c.Customers.DeleteTaxIdAsync(id, "txi_1")),
        ("Customers.DeleteTaxIdAsync tax id", "taxId", (c, id) => c.Customers.DeleteTaxIdAsync("cus_1", id)),
        ("Invoices.UpdateAsync", "id", (c, id) => c.Invoices.UpdateAsync(id, Body)),
        ("Invoices.SendAsync", "id", (c, id) => c.Invoices.SendAsync(id)),
        ("Invoices.MarkPaidAsync", "id", (c, id) => c.Invoices.MarkPaidAsync(id)),
        ("Invoices.CancelAsync", "id", (c, id) => c.Invoices.CancelAsync(id)),
        ("Accounts.DeleteAsync", "id", (c, id) => c.Accounts.DeleteAsync(id)),
        ("Accounts.RegenerateTokenAsync", "id", (c, id) => c.Accounts.RegenerateTokenAsync(id)),
        ("WebhookEndpoints.RetrieveAsync", "id", (c, id) => c.WebhookEndpoints.RetrieveAsync(id)),
        ("WebhookEndpoints.RotateSecretAsync", "id", (c, id) => c.WebhookEndpoints.RotateSecretAsync(id)),
        ("Events.RetrieveAsync", "id", (c, id) => c.Events.RetrieveAsync(id)),
        ("Events.ResendAsync", "id", (c, id) => c.Events.ResendAsync(id)),
        ("Disputes.RetrieveAsync", "id", (c, id) => c.Disputes.RetrieveAsync(id)),
        ("Disputes.SubmitEvidenceAsync", "id", (c, id) => c.Disputes.SubmitEvidenceAsync(id, Body)),
        ("Disputes.AcceptAsync", "id", (c, id) => c.Disputes.AcceptAsync(id)),
        ("Disputes.CloseAsync", "id", (c, id) => c.Disputes.CloseAsync(id)),
        ("Disputes.SendMessageAsync", "id", (c, id) => c.Disputes.SendMessageAsync(id, Body)),
        ("Tax.RetrieveCodeAsync", "code", (c, id) => c.Tax.RetrieveCodeAsync(id)),
        ("Tax.RetrieveRegistrationAsync", "id", (c, id) => c.Tax.RetrieveRegistrationAsync(id)),
        ("Tax.UpdateRegistrationAsync", "id", (c, id) => c.Tax.UpdateRegistrationAsync(id, Body)),
        ("Tax.DeleteRegistrationAsync", "id", (c, id) => c.Tax.DeleteRegistrationAsync(id)),
        ("Shield.RemoveFromBlocklistAsync", "id", (c, id) => c.Shield.RemoveFromBlocklistAsync(id)),
    ];

    [Theory]
    [InlineData("pay_1/refunds")]
    [InlineData("../balance")]
    [InlineData("/")]
    [InlineData("..")]
    [InlineData(".")]
    public async Task AnIdThatIsNotAWajubIdIsRefusedBeforeAnythingIsSent(string id)
    {
        foreach (var (name, paramName, call) in Calls)
        {
            var (httpClient, recorded) = TestFixtures.CreateTestClient([]);
            var client = TestFixtures.CreateWajubClient(httpClient);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => call(client, id));
            Assert.True(paramName == ex.ParamName, $"{name}: ParamName is {ex.ParamName}, not {paramName}");
            Assert.True(recorded.Method == string.Empty, $"{name} sent a request");
        }
    }

    [Fact]
    public async Task AnEmptyOrMissingIdIsRefusedBeforeAnythingIsSent()
    {
        foreach (var (name, paramName, call) in Calls)
        {
            var (httpClient, recorded) = TestFixtures.CreateTestClient([]);
            var client = TestFixtures.CreateWajubClient(httpClient);

            var empty = await Assert.ThrowsAsync<ArgumentException>(() => call(client, string.Empty));
            var missing = await Assert.ThrowsAsync<ArgumentNullException>(() => call(client, null!));
            Assert.True(paramName == empty.ParamName && paramName == missing.ParamName, $"{name}: wrong ParamName");
            Assert.True(recorded.Method == string.Empty, $"{name} sent a request");
        }
    }

    [Theory]
    [InlineData("cus_1?force=1", "/customers/cus_1%3Fforce%3D1/tax_ids/txi_1")]
    [InlineData("cus_1#", "/customers/cus_1%23/tax_ids/txi_1")]
    [InlineData("cus_1%2Fx", "/customers/cus_1%252Fx/tax_ids/txi_1")]
    [InlineData("cus.1..2", "/customers/cus.1..2/tax_ids/txi_1")]
    public async Task AnyOtherCharacterStaysInTheId(string customerId, string path)
    {
        var (httpClient, recorded) = TestFixtures.CreateTestClient([_ => (200, new Dictionary<string, object?>())]);
        var client = TestFixtures.CreateWajubClient(httpClient);

        await client.Customers.DeleteTaxIdAsync(customerId, "txi_1");

        Assert.Equal(HttpMethod.Delete.Method, recorded.Method);
        Assert.Equal(path, recorded.Path);
        Assert.Equal(string.Empty, recorded.Query);
    }
}
