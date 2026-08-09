namespace Wajub;

/// <summary>Represents a Wajub payment (transaction).</summary>
public record Payment
{
    public string Id { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public long Amount { get; init; }

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
        JsonUtils.TryGetDouble(transaction.GetValueOrDefault("amount"), out var amount);

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
            Amount = (long)amount,
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

    public long Amount { get; init; }

    public Dictionary<string, object?> Raw { get; init; } = [];

    internal static Refund Parse(Dictionary<string, object?> body)
    {
        var data = JsonUtils.PickResource(body, "refund");

        JsonUtils.TryGetString(data, "id", out var id);
        JsonUtils.TryGetString(data, "status", out var status);
        JsonUtils.TryGetDouble(data.GetValueOrDefault("amount"), out var amount);

        return new Refund
        {
            Id = id ?? string.Empty,
            Status = status ?? string.Empty,
            Amount = (long)amount,
            Raw = data,
        };
    }
}
