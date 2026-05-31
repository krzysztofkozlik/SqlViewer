namespace SqlViewer.Api.Models;

public record SqlCommandEvent(
    string TraceId,
    string Url,
    string MethodName,
    string CommandType,
    string FirstTable,
    long DurationUs,
    long RowCount,
    string SqlText,
    DateTimeOffset CapturedAt
);
