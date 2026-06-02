using System.Text.RegularExpressions;
using SqlViewer.Api.Models;

namespace SqlViewer.Api.Parsing;

public static class SqlStatementParser
{
    // Matches: /* MethodName /url/path [traceId/spanId] */
    // Group 3 = traceId, group 4 = spanId. Grouping is by spanId (one span = one HTTP request handler).
    private static readonly Regex CommentPattern = new(
        @"/\*\s*(\S+)\s+(\S+)\s+\[([a-fA-F0-9\-]+)/([a-fA-F0-9\-]+)\]\s*\*/",
        RegexOptions.Compiled);

    private static readonly Regex CommandTypePattern = new(
        @"^\s*(SELECT|INSERT|UPDATE|DELETE|EXEC|EXECUTE|MERGE|CREATE|DROP|ALTER|TRUNCATE)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Captures the first table name after FROM, UPDATE, INTO, or JOIN.
    // Handles optional schema prefix: dbo.TableName or [dbo].[TableName].
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

        var commandType = "UNKNOWN";
        var cmdMatch = CommandTypePattern.Match(sql);
        if (cmdMatch.Success)
            commandType = cmdMatch.Groups[1].Value.ToUpperInvariant();

        var firstTable = string.Empty;
        var tableMatch = FirstTablePattern.Match(sql);
        if (tableMatch.Success)
            firstTable = tableMatch.Groups[1].Value;

        return new ParsedStatement(methodName, url, traceId, spanId, commandType, firstTable);
    }
}
