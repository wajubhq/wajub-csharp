namespace Wajub;

/// <summary>Base error for API failures.</summary>
public class WajubError : Exception
{
    public string Code { get; init; } = string.Empty;

    public int HttpStatus { get; init; }

    public Dictionary<string, string>? Errors { get; init; }

    public Dictionary<string, object?>? Raw { get; init; }

    public WajubError(string message, string code = "", int httpStatus = 0)
        : base(message)
    {
        Code = code;
        HttpStatus = httpStatus;
    }
}

/// <summary>Returned for HTTP 401 responses.</summary>
public class AuthenticationError : WajubError
{
    public AuthenticationError(string message, string code, int httpStatus, Dictionary<string, string>? errors, Dictionary<string, object?>? raw)
        : base(message, code, httpStatus)
    {
        Errors = errors;
        Raw = raw;
    }
}

/// <summary>Returned for HTTP 403 responses.</summary>
public class PermissionError : WajubError
{
    public PermissionError(string message, string code, int httpStatus, Dictionary<string, string>? errors, Dictionary<string, object?>? raw)
        : base(message, code, httpStatus)
    {
        Errors = errors;
        Raw = raw;
    }
}

/// <summary>Returned for HTTP 404 responses.</summary>
public class NotFoundError : WajubError
{
    public NotFoundError(string message, string code, int httpStatus, Dictionary<string, string>? errors, Dictionary<string, object?>? raw)
        : base(message, code, httpStatus)
    {
        Errors = errors;
        Raw = raw;
    }
}

/// <summary>Returned for HTTP 400/422 responses.</summary>
public class InvalidRequestError : WajubError
{
    public InvalidRequestError(string message, string code, int httpStatus, Dictionary<string, string>? errors, Dictionary<string, object?>? raw)
        : base(message, code, httpStatus)
    {
        Errors = errors;
        Raw = raw;
    }
}

/// <summary>Returned for HTTP 429 responses.</summary>
public class RateLimitError : WajubError
{
    public int RetryAfter { get; init; }

    public RateLimitError(string message, string code, int httpStatus, Dictionary<string, string>? errors, Dictionary<string, object?>? raw, int retryAfter)
        : base(message, code, httpStatus)
    {
        Errors = errors;
        Raw = raw;
        RetryAfter = retryAfter;
    }
}

/// <summary>Returned when the HTTP transport fails.</summary>
public class ApiConnectionError : WajubError
{
    public ApiConnectionError(string message)
        : base(message, "network_error")
    {
    }
}

/// <summary>Returned when webhook verification fails.</summary>
public class WebhookSignatureVerificationError : Exception
{
    public WebhookSignatureVerificationError(string message)
        : base(message)
    {
    }
}
