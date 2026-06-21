namespace SqlViewer.Api.Models;

public record SqlCommandEvent(
    string TraceId,
    string SpanId,
    string Url,
    string RequestType,
    string MethodName,
    string CommandType,
    string FirstTable,
    long DurationUs,
    long RowCount,
    string SqlText,
    DateTimeOffset CapturedAt
);
