using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Wajub;

internal static partial class JsonUtils
{
    internal const string ApiUrl = "https://api.wajub.com";

    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [GeneratedRegex(@"^(?i)bearer\s+", RegexOptions.Compiled)]
    private static partial Regex BearerPrefix();

    public static string NormalizeApiKey(string raw)
    {
        var key = raw.Trim();
        if (BearerPrefix().IsMatch(key))
        {
            key = BearerPrefix().Replace(key, string.Empty).Trim();
        }

        return key;
    }

    public static string DefaultApiUrl() => ApiUrl;

    public static string CreateIdempotencyKey(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            prefix = "wajub";
        }

        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return $"{prefix}-{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }

    public static Dictionary<string, object?> DecodeJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object?>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, SerializerOptions)
            ?? new Dictionary<string, object?>();
    }

    public static string SerializeBody(Dictionary<string, object?> body)
    {
        return JsonSerializer.Serialize(body, SerializerOptions);
    }

    public static Dictionary<string, object?> PickResource(Dictionary<string, object?> body, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (body.TryGetValue(key, out var value) && value is not null)
            {
                var dict = AsDictionary(value);
                if (dict is not null)
                {
                    return dict;
                }
            }
        }

        return body;
    }

    public static (List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta) PickList(
        Dictionary<string, object?> body,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (body.TryGetValue(key, out var value) && value is not null)
            {
                return (ToMapList(value), ToMeta(body.GetValueOrDefault("meta")));
            }
        }

        if (body.TryGetValue("items", out var items) && items is not null)
        {
            return (ToMapList(items), ToMeta(body.GetValueOrDefault("meta")));
        }

        return (ToMapList(body.GetValueOrDefault("data")), ToMeta(body.GetValueOrDefault("meta")));
    }

    public static List<Dictionary<string, object?>> ToMapList(object? value)
    {
        if (value is JsonElement { ValueKind: JsonValueKind.Array } element)
        {
            var list = new List<Dictionary<string, object?>>();
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    list.Add(JsonElementToDictionary(item));
                }
            }

            return list;
        }

        if (value is IEnumerable<object?> enumerable)
        {
            var list = new List<Dictionary<string, object?>>();
            foreach (var item in enumerable)
            {
                var dict = AsDictionary(item);
                if (dict is not null)
                {
                    list.Add(dict);
                }
            }

            return list;
        }

        return [];
    }

    public static Dictionary<string, object?>? ToMeta(object? value) => AsDictionary(value);

    public static Dictionary<string, object?>? AsDictionary(object? value)
    {
        return value switch
        {
            Dictionary<string, object?> dict => dict,
            JsonElement { ValueKind: JsonValueKind.Object } element => JsonElementToDictionary(element),
            _ => null,
        };
    }

    public static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = property.Value;
        }

        return dict;
    }

    public static bool TryGetString(Dictionary<string, object?> body, string key, out string? value)
    {
        value = null;
        if (!body.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        value = raw switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } el => el.GetString(),
            _ => raw.ToString(),
        };

        return value is not null;
    }

    public static bool TryGetDouble(object? value, out double result)
    {
        result = 0;
        switch (value)
        {
            case double d:
                result = d;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case JsonElement { ValueKind: JsonValueKind.Number } el when el.TryGetDouble(out var dbl):
                result = dbl;
                return true;
            default:
                return false;
        }
    }

    public static bool TryGetBool(object? value, out bool result)
    {
        result = false;
        switch (value)
        {
            case bool b:
                result = b;
                return true;
            case JsonElement { ValueKind: JsonValueKind.True }:
                result = true;
                return true;
            case JsonElement { ValueKind: JsonValueKind.False }:
                result = false;
                return true;
            default:
                return false;
        }
    }

    public static string ToStringValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } el => el.GetString() ?? string.Empty,
            _ => value.ToString() ?? string.Empty,
        };
    }
}
