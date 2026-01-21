using Serilog.Events;

namespace AISPubSub.Infrastructure.Logging;

public class LogEntry
{
    public string? Message { get; set; }
    public LogEventLevel Level { get; set; }
    public override string ToString() => Message!;
}

