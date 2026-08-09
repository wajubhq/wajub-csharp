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
        if (body.TryGetValue("errors", out var rawErrors) && rawErrors is not null)
        {
            var errorsDict = JsonUtils.AsDictionary(rawErrors);
            if (errorsDict is not null)
            {
                fieldErrors = new Dictionary<string, string>();
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
                }
            }
        }

        return status switch
        {
            401 => new AuthenticationError(message, code, status, fieldErrors, body),
            403 => new PermissionError(message, code, status, fieldErrors, body),
            404 => new NotFoundError(message, code, status, fieldErrors, body),
            429 => new RateLimitError(message, code, status, fieldErrors, body, retryAfter),
            400 or 422 => new InvalidRequestError(message, code, status, fieldErrors, body),
            _ => new WajubError(message, code, status) { Errors = fieldErrors, Raw = body },
        };
    }
}
