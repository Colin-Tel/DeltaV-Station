﻿using System.Linq;
using System.Numerics;
using Content.Shared.Administration.Notes;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client.Administration.UI.Notes;

[GenerateTypedNameReferences]
public sealed partial class AdminNotesControl : Control
{
    public event Action<int, string>? OnNoteChanged;
    public event Action<string>? OnNewNoteEntered;
    public event Action<int>? OnNoteDeleted;

    private AdminNotesLinePopup? _popup;

    public AdminNotesControl()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        NewNote.OnTextEntered += NewNoteEntered;
    }

    private Dictionary<int, AdminNotesLine> Inputs { get; } = new();
    private bool CanCreate { get; set; }
    private bool CanDelete { get; set; }
    private bool CanEdit { get; set; }

    private void NewNoteEntered(LineEditEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Text))
        {
            return;
        }

        NewNote.Clear();
        OnNewNoteEntered?.Invoke(args.Text);
    }

    private void NoteSubmitted(AdminNotesLine input)
    {
        var text = input.EditText.Trim();
        if (input.OriginalMessage == text)
        {
            return;
        }

        OnNoteChanged?.Invoke(input.Id, text);
    }

    private bool NoteClicked(AdminNotesLine line)
    {
        ClosePopup();

        _popup = new AdminNotesLinePopup(line.Note, CanDelete, CanEdit);
        _popup.OnEditPressed += noteId =>
        {
            if (!Inputs.TryGetValue(noteId, out var input))
            {
                return;
            }

            input.SetEditable(true);
        };
        _popup.OnDeletePressed += noteId => OnNoteDeleted?.Invoke(noteId);

        var box = UIBox2.FromDimensions(UserInterfaceManager.MousePositionScaled.Position, Vector2.One);
        _popup.Open(box);

        return true;
    }

    private void ClosePopup()
    {
        _popup?.Close();
        _popup = null;
    }

    public void SetNotes(Dictionary<int, SharedAdminNote> notes)
    {
        foreach (var (id, input) in Inputs)
        {
            if (!notes.ContainsKey(id))
            {
                Notes.RemoveChild(input);
                Inputs.Remove(id);
            }
        }

        foreach (var note in notes.Values.OrderBy(note => note.Id))
        {
            if (Inputs.TryGetValue(note.Id, out var input))
            {
                input.UpdateNote(note);
                continue;
            }

            input = new AdminNotesLine(note);
            input.OnSubmitted += NoteSubmitted;
            input.OnClicked += NoteClicked;
            Notes.AddChild(input);
            Inputs[note.Id] = input;
        }
    }

    public void SetPermissions(bool create, bool delete, bool edit)
    {
        CanCreate = create;
        CanDelete = delete;
        CanEdit = edit;
        NewNoteLabel.Visible = create;
        NewNote.Visible = create;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        foreach (var input in Inputs.Values)
        {
            input.OnSubmitted -= NoteSubmitted;
        }

        Inputs.Clear();
        NewNote.OnTextEntered -= NewNoteEntered;

        if (_popup != null)
        {
            UserInterfaceManager.PopupRoot.RemoveChild(_popup);
        }

        OnNoteChanged = null;
        OnNewNoteEntered = null;
        OnNoteDeleted = null;
    }
}
