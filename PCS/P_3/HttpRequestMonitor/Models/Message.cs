namespace HttpRequestMonitor.Models;

public sealed class Message
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Text { get; init; } = string.Empty;

    public DateTime ReceivedAt { get; init; } = DateTime.Now;
}
