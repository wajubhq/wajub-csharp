using System.Text.Json;

namespace Wajub;

internal static class ErrorFactory
{
    public static WajubError FromResponse(int status, Dictionary<string, object?> body, int retryAfter)
    {
        var message = $"Request failed ({status})";
        if (JsonUtils.TryGetString(body, "message", out var msg) && msg is not null)
        {
            message = msg;
        }

        var code = $"http_{status}";
        if (JsonUtils.TryGetString(body, "error_code", out var errorCode) && errorCode is not null)
        {
            code = errorCode;
        }
        else if (JsonUtils.TryGetString(body, "code", out var bodyCode) && bodyCode is not null)
        {
            code = bodyCode;
        }

        Dictionary<string, string>? fieldErrors = null;
        Dictionary<string, IReadOnlyList<string>>? allFieldErrors = null;
        if (body.TryGetValue("errors", out var rawErrors) && rawErrors is not null)
        {
            var errorsDict = JsonUtils.AsDictionary(rawErrors);
            if (errorsDict is not null)
            {
                fieldErrors = new Dictionary<string, string>();
                allFieldErrors = new Dictionary<string, IReadOnlyList<string>>();
                foreach (var (field, value) in errorsDict)
                {
                    if (value is JsonElement { ValueKind: JsonValueKind.Array } arr && arr.GetArrayLength() > 0)
                    {
                        fieldErrors[field] = arr[0].ToString();
                    }
                    else if (value is object[] objArr && objArr.Length > 0)
                    {
                        fieldErrors[field] = objArr[0]?.ToString() ?? string.Empty;
                    }
                    else if (value is string[] strArr && strArr.Length > 0)
                    {
                        fieldErrors[field] = strArr[0];
                    }
                    else
                    {
                        fieldErrors[field] = value?.ToString() ?? string.Empty;
                    }

                    allFieldErrors[field] = Messages(value);
                }
            }
        }

        return status switch
        {
            401 => new AuthenticationError(message, code, status, fieldErrors, body) { FieldErrors = allFieldErrors },
            403 => new PermissionError(message, code, status, fieldErrors, body) { FieldErrors = allFieldErrors },
            404 => new NotFoundError(message, code, status, fieldErrors, body) { FieldErrors = allFieldErrors },
            429 => new RateLimitError(message, code, status, fieldErrors, body, retryAfter) { FieldErrors = allFieldErrors },
            400 or 422 => new InvalidRequestError(message, code, status, fieldErrors, body) { FieldErrors = allFieldErrors },
            _ => new WajubError(message, code, status) { Errors = fieldErrors, FieldErrors = allFieldErrors, Raw = body },
        };
    }

    /// <summary>Every message the API gave for one field: a list as is, a single message as a list of one, null as none.</summary>
    private static IReadOnlyList<string> Messages(object? value)
    {
        return value switch
        {
            JsonElement { ValueKind: JsonValueKind.Array } arr => arr.EnumerateArray().Select(item => item.ToString()).ToList(),
            JsonElement { ValueKind: JsonValueKind.Null } => [],
            string s => [s],
            System.Collections.IEnumerable items => items.Cast<object?>().Select(item => item?.ToString() ?? string.Empty).ToList(),
            null => [],
            _ => [value.ToString() ?? string.Empty],
        };
    }
}
