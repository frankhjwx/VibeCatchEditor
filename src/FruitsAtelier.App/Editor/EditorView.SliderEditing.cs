using L = FruitsAtelier.Localization.Strings;
using FruitsAtelier.Core;

namespace FruitsAtelier.App.Editor;

public sealed partial class EditorView
{
    private void EditImportedSlider()
    {
        if (SelectedImportedSlider is not { } slider) return;
        string notice = "";
        if (!Edit(L.Get("editor.command.editImportedSlider"), () =>
        {
            var result = ImportedSliderEditing.ConvertToTrack(Document, slider.Id);
            notice = string.Join(L.Get("editor.diagnostics.separator"), result.Diagnostics);
        })) return;
        Select(slider.Id, slider.Id);
        tool = Tool.Slider;
        StatusMessage = notice.Length == 0 ? L.Get("editor.status.importedSliderEditable") : notice;
    }
}
