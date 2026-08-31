using L = VibeCatchEditor.Localization.Strings;
namespace VibeCatchEditor.Core;

public sealed class EditorHistory
{
    private sealed record Change(string Label, MapDocument Before, MapDocument After);

    private readonly Stack<Change> undo = new();
    private readonly Stack<Change> redo = new();
    private MapDocument baseline;
    private MapDocument? transactionStart;
    private string transactionLabel = "";

    public EditorHistory(MapDocument document)
    {
        Document = document.DeepClone();
        baseline = Document.DeepClone();
    }

    public MapDocument Document { get; private set; }
    public bool IsDirty => !Document.ContentEquals(baseline);
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public string UndoLabel => undo.TryPeek(out var change) ? change.Label : "";

    public void MarkSaved()
    {
        if (transactionStart is not null) throw new InvalidOperationException(L.Get("core.history.saveDuringEdit"));
        baseline = Document.DeepClone();
    }

    public void Begin(string label)
    {
        if (transactionStart is not null) throw new InvalidOperationException(L.Get("core.history.activeEdit"));
        transactionStart = Document.DeepClone();
        transactionLabel = label;
    }

    public void Commit()
    {
        if (transactionStart is null) return;
        if (!Document.ContentEquals(transactionStart))
        {
            undo.Push(new Change(transactionLabel, transactionStart, Document.DeepClone()));
            redo.Clear();
        }
        transactionStart = null;
        transactionLabel = "";
    }

    public void Cancel()
    {
        if (transactionStart is null) return;
        Document = transactionStart;
        transactionStart = null;
        transactionLabel = "";
    }

    public void Undo()
    {
        if (transactionStart is not null) { Cancel(); return; }
        if (!undo.TryPop(out var change)) return;
        Document = change.Before.DeepClone();
        redo.Push(change);
    }

    public void Redo()
    {
        if (transactionStart is not null) { Cancel(); return; }
        if (!redo.TryPop(out var change)) return;
        Document = change.After.DeepClone();
        undo.Push(change);
    }

    public void Reset(MapDocument document)
    {
        Document = document.DeepClone();
        baseline = Document.DeepClone();
        undo.Clear();
        redo.Clear();
        transactionStart = null;
        transactionLabel = "";
    }
}
