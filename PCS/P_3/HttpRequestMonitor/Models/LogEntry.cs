using System.Text;

namespace HttpRequestMonitor.Models;

public sealed class LogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;

    public string Source { get; init; } = string.Empty;

    public string Method { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public int StatusCode { get; init; }

    public double ProcessingTimeMs { get; init; }

    public string Headers { get; init; } = string.Empty;

    public string RequestBody { get; init; } = string.Empty;

    public string ResponseBody { get; init; } = string.Empty;

    public string DisplayText
    {
        get
        {
            var builder = new StringBuilder();
            builder.Append('[').Append(Timestamp.ToString("yyyy-MM-dd HH:mm:ss")).Append("] ");
            builder.Append(Source).Append(' ');
            builder.Append(Method).Append(' ');
            builder.Append(Url).Append(" -> ");
            builder.Append(StatusCode).Append(" (");
            builder.Append(ProcessingTimeMs.ToString("F1")).Append(" ms)");

            if (!string.IsNullOrWhiteSpace(RequestBody))
            {
                builder.AppendLine();
                builder.Append("Body: ").Append(RequestBody);
            }

            return builder.ToString();
        }
    }
}
