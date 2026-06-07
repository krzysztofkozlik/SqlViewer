using System.Text.RegularExpressions;

namespace SqlViewer.Api.Parsing;

public static class SqlStatementParser
{
    // Matches two comment formats (no spaces around the slash in either case):
    //   Web API: /* MethodName /url/path [traceId/spanId] */
    //   Non Web API:  /* MethodName AssemblyName [processName/operationId] */
    // Group 1 = methodName, group 2 = url/assemblyName,
    // group 3 = traceId/processName, group 4 = spanId/operationId (grouping key).
    // [^\]/\s]+ allows any non-whitespace, non-bracket, non-slash characters
    // so processNames like "dotnet" are accepted alongside hex traceIds.
    private static readonly Regex CommentPattern = new(
        @"/\*\s*(\S+)\s+(\S+)\s+\[([^\]/\s]+)/([^\]\s]+)\]\s*\*/",
        RegexOptions.Compiled);

    // Plain SQL — first keyword identifies the operation.
    private static readonly Regex CommandTypePattern = new(
        @"^\s*(SELECT|INSERT|UPDATE|DELETE|MERGE|CREATE|DROP|ALTER|TRUNCATE)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // sp_executesql wraps the real SQL in its first string argument.
    // Capture the first keyword of that inner SQL instead of reporting "EXEC".
    private static readonly Regex SpExecuteSqlPattern = new(
        @"^\s*EXEC(?:UTE)?\s+sp_executesql\s+N?'\s*(SELECT|INSERT|UPDATE|DELETE|MERGE|CREATE|DROP|ALTER|TRUNCATE)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // First table after FROM, UPDATE, INTO, or JOIN. Handles optional schema prefix.
    private static readonly Regex FirstTablePattern = new(
        @"\b(?:FROM|UPDATE|INTO|JOIN)\s+(?:\[?\w+\]?\.)?\[?(\w+)\]?",
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
    /// Returns null if the statement does not contain the expected comment marker.
    /// </summary>
    public static ParsedStatement? TryParse(string sql)
    {
        var commentMatch = CommentPattern.Match(sql);
        if (!commentMatch.Success)
            return null;

        var methodName = commentMatch.Groups[1].Value;
        var url = commentMatch.Groups[2].Value;
        var traceId = commentMatch.Groups[3].Value;
        var spanId = commentMatch.Groups[4].Value;

        var commandType = ExtractCommandType(sql);

        var firstTable = string.Empty;
        var tableMatch = FirstTablePattern.Match(sql);
        if (tableMatch.Success)
            firstTable = tableMatch.Groups[1].Value;

        return new ParsedStatement(methodName, url, traceId, spanId, commandType, firstTable);
    }

    private static string ExtractCommandType(string sql)
    {
        var spMatch = SpExecuteSqlPattern.Match(sql);
        if (spMatch.Success)
            return spMatch.Groups[1].Value.ToUpperInvariant();

        var cmdMatch = CommandTypePattern.Match(sql);
        return cmdMatch.Success ? cmdMatch.Groups[1].Value.ToUpperInvariant() : "UNKNOWN";
    }
}
