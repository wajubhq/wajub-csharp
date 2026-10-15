namespace Wajub;

/// <summary>Represents a Wajub payment (transaction).</summary>
public record Payment
{
    public string Id { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    /// <summary>The amount without its decimals: GHS 12.50 reads 12.</summary>
    [Obsolete("Amount drops the decimals of every currency that has them: GHS 12.50 reads 12. Use AmountDecimal.")]
    public long Amount { get; init; }

    /// <summary>
    /// The amount in the currency's major unit, decimals included, as the API sends it:
    /// 15000 for XAF 15,000, 12.5 for GHS 12.50.
    /// </summary>
    public decimal AmountDecimal { get; init; }

    public string Currency { get; init; } = string.Empty;

    public string AuthorizationUrl { get; init; } = string.Empty;

    public string AuthorizationToken { get; init; } = string.Empty;

    public Dictionary<string, object?> Raw { get; init; } = [];

    internal static Payment Parse(Dictionary<string, object?> body)
    {
        var transaction = JsonUtils.PickResource(body, "transaction");

        JsonUtils.TryGetString(transaction, "id", out var id);
        JsonUtils.TryGetString(transaction, "status", out var status);
        JsonUtils.TryGetString(transaction, "currency", out var currency);
        JsonUtils.TryGetDecimal(transaction.GetValueOrDefault("amount"), out var amount);

        var token = body.GetValueOrDefault("authorization_token")
            ?? transaction.GetValueOrDefault("authorization_token")
            ?? transaction.GetValueOrDefault("id");
        var url = body.GetValueOrDefault("authorization_url")
            ?? transaction.GetValueOrDefault("authorization_url");

        return new Payment
        {
            Id = id ?? string.Empty,
            Status = status ?? string.Empty,
            Currency = currency ?? string.Empty,
#pragma warning disable CS0618 // Kept, deprecated, with the value it always had.
            Amount = JsonUtils.WholeUnits(amount),
#pragma warning restore CS0618
            AmountDecimal = amount,
            AuthorizationToken = JsonUtils.ToStringValue(token),
            AuthorizationUrl = JsonUtils.ToStringValue(url),
            Raw = transaction,
        };
    }
}

/// <summary>Represents a Wajub customer.</summary>
public record Customer
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public Dictionary<string, object?> Raw { get; init; } = [];

    internal static Customer Parse(Dictionary<string, object?> body)
    {
        var data = JsonUtils.PickResource(body, "customer");

        JsonUtils.TryGetString(data, "id", out var id);
        JsonUtils.TryGetString(data, "email", out var email);
        JsonUtils.TryGetString(data, "name", out var name);

        return new Customer
        {
            Id = id ?? string.Empty,
            Email = email ?? string.Empty,
            Name = name ?? string.Empty,
            Raw = data,
        };
    }
}

/// <summary>Represents a Wajub refund.</summary>
public record Refund
{
    public string Id { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    /// <summary>The amount without its decimals: GHS 12.50 reads 12.</summary>
    [Obsolete("Amount drops the decimals of every currency that has them: GHS 12.50 reads 12. Use AmountDecimal.")]
    public long Amount { get; init; }

    /// <summary>
    /// The amount in the currency's major unit, decimals included, as the API sends it:
    /// 12.5 for GHS 12.50.
    /// </summary>
    public decimal AmountDecimal { get; init; }

    public Dictionary<string, object?> Raw { get; init; } = [];

    internal static Refund Parse(Dictionary<string, object?> body)
    {
        var data = JsonUtils.PickResource(body, "refund");

        JsonUtils.TryGetString(data, "id", out var id);
        JsonUtils.TryGetString(data, "status", out var status);
        JsonUtils.TryGetDecimal(data.GetValueOrDefault("amount"), out var amount);

        return new Refund
        {
            Id = id ?? string.Empty,
            Status = status ?? string.Empty,
#pragma warning disable CS0618 // Kept, deprecated, with the value it always had.
            Amount = JsonUtils.WholeUnits(amount),
#pragma warning restore CS0618
            AmountDecimal = amount,
            Raw = data,
        };
    }
}
