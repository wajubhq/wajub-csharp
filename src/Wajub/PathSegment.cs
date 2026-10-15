using System.Runtime.CompilerServices;

namespace Wajub;

internal static class PathSegment
{
    /// <summary>
    /// An id as one segment of a URL path. A "/" would reach another endpoint, even
    /// escaped, since the API decodes %2F before it routes, and "." or ".." would be
    /// folded into the path around them: those are refused. Anything else, such as
    /// "?" or "#", is escaped so it stays in the id.
    /// </summary>
    public static string Id(string id, [CallerArgumentExpression(nameof(id))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(id, paramName);
        if (id.Contains('/') || id is "." or "..")
        {
            throw new ArgumentException($"\"{id}\" is not a Wajub id: an id never contains \"/\" and is never \".\" or \"..\".", paramName);
        }

        return Uri.EscapeDataString(id);
    }
}
