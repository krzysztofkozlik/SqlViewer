using System.Text.Json;
using System.Text.RegularExpressions;

namespace SqlViewer.Api.Parsing;

public static class SqlStatementParser
{
    // Matches the JSON comment injected by the application:
    //   /* {"cs":"...","ctx":"...","parentId":"...","id":"..."} */
    private static readonly Regex JsonCommentPattern = new(
        @"/\*\s*(\{.*?\})\s*\*/",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // Plain SQL — first keyword identifies the operation.
    private static readonly Regex CommandTypePattern = new(
        @"^\s*(SELECT|INSERT|UPDATE|DELETE|MERGE|CREATE|DROP|ALTER|TRUNCATE)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // sp_executesql wraps the real SQL in its first string argument.
    // Capture the first keyword of that inner SQL instead of reporting "EXEC".
    private static readonly Regex SpExecuteSqlPattern = new(
        @"^\s*EXEC(?:UTE)?\s+sp_executesql\s+N?'\s*(SELECT|INSERT|UPDATE|DELETE|MERGE|CREATE|DROP|ALTER|TRUNCATE)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // First table after FROM, UPDATE, INTO, or JOIN.
    // Handles optional schema prefix and an optional block comment between the
    // keyword and the table name (e.g. UPDATE /*comment*/ dbo.TableName).
    private static readonly Regex FirstTablePattern = new(
        @"\b(?:FROM|UPDATE|INTO|JOIN)\s+(?:/\*[\s\S]*?\*/\s*)?(?:\[?\w+\]?\.)?\[?(\w+)\]?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public record ParsedStatement(
        string MethodName,
        string Url,
        string TraceId,
        string SpanId,
        string CommandType,
        string FirstTable
    );

    /// <summary>
    /// Returns null if the statement does not contain the expected JSON comment marker.
    /// </summary>
    public static ParsedStatement? TryParse(string sql)
    {
        var commentMatch = JsonCommentPattern.Match(sql);
        if (!commentMatch.Success)
            return null;

        string methodName, url, traceId, spanId;
        try
        {
            using var doc = JsonDocument.Parse(commentMatch.Groups[1].Value);
            var root = doc.RootElement;
            methodName = GetString(root, "cs");
            url        = GetString(root, "ctx").Replace("''", "'");
            traceId    = GetString(root, "parentId");
            spanId     = GetString(root, "id");
        }
        catch (JsonException)
        {
            return null;
        }

        var commandType = ExtractCommandType(sql);

        var sqlWithoutComment = JsonCommentPattern.Replace(sql, "");
        var firstTable = string.Empty;
        var tableMatch = FirstTablePattern.Match(sqlWithoutComment);
        if (tableMatch.Success)
            firstTable = tableMatch.Groups[1].Value;

        return new ParsedStatement(methodName, url, traceId, spanId, commandType, firstTable);
    }

    private static string GetString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var el) ? el.GetString() ?? "" : "";

    private static string ExtractCommandType(string sql)
    {
        var spMatch = SpExecuteSqlPattern.Match(sql);
        if (spMatch.Success)
            return spMatch.Groups[1].Value.ToUpperInvariant();

        var cmdMatch = CommandTypePattern.Match(sql);
        return cmdMatch.Success ? cmdMatch.Groups[1].Value.ToUpperInvariant() : "UNKNOWN";
    }
}
