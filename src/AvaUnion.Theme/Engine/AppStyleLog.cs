using System.Collections.Generic;
using Avalonia.Logging;

namespace AvaUnion.Engine;

internal sealed class AppStyleLog
{
    private readonly HashSet<string> _logged = new(System.StringComparer.Ordinal);
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public void Unavailable(object source, string id, string reason) =>
        Write(source, id, $"AvaUnion: style {id} unavailable: {reason}");

    public void HasErrors(object source, string id) =>
        Write(source, id, $"AvaUnion: style {id} has CSS errors");

    private void Write(object source, string id, string line)
    {
        if (!_logged.Add(id))
        {
            return;
        }

        _lines.Add(line);
        Logger.TryGet(LogEventLevel.Warning, "AvaUnion")?.Log(source, "{Line}", line);
    }
}
