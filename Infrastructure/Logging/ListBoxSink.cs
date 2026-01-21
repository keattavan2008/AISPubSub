using System.IO;
using System.Windows.Forms;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace AISPubSub.Infrastructure.Logging;

public class ListBoxSink : ILogEventSink
{
    private readonly ListBox _listBox;
    private readonly MessageTemplateTextFormatter _formatter;

    public ListBoxSink(ListBox listBox)
    {
        _listBox = listBox;
        _formatter = new MessageTemplateTextFormatter("{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}");
    }

    public void Emit(LogEvent logEvent)
    {
        var sw = new StringWriter();
        _formatter.Format(logEvent, sw);

        var entry = new LogEntry
        {
            Message = sw.ToString().Trim(),
            Level = logEvent.Level
        };

        if (_listBox.InvokeRequired)
            _listBox.BeginInvoke(() => AddToGrid(entry));
        else
            AddToGrid(entry);
    }

    private void AddToGrid(LogEntry entry)
    {
        _listBox.Items.Add(entry); // We add the object, not just a string
        _listBox.TopIndex = _listBox.Items.Count - 1;
    }
}

