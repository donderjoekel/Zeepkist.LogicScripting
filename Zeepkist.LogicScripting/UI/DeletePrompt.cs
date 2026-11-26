using Imui.Controls;
using Imui.Core;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed class DeletePrompt<T>
    where T : class
{
    /// <summary>
    /// Null when the prompt is closed.
    /// </summary>
    public T Deleted { get; private set; }

    public void Open(T deleted)
    {
        Deleted = deleted;
    }

    public void Close()
    {
        Deleted = null;
    }

    /// <summary>
    /// Call while <see cref="Deleted"/> is set. Cancel closes the prompt.
    /// <paramref name="pressed"/> is a button pressed for the player, which the prompt handles as clicked.
    /// </summary>
    public DeleteChoice Draw(ImGui gui, string question, bool askAboutBlocks, PromptButton? pressed)
    {
        var choice = DeleteChoice.None;
        gui.Text(question, wrap: true);
        if (askAboutBlocks)
        {
            if (gui.Button("Delete blocks too") || pressed == PromptButton.Delete)
                choice = DeleteChoice.Delete;

            if (gui.Button("Keep blocks") || pressed == PromptButton.KeepBlocks)
                choice = DeleteChoice.KeepBlocks;
        }
        else if (gui.Button("Delete") || pressed == PromptButton.Delete)
        {
            choice = DeleteChoice.Delete;
        }

        // Stacked rather than side by side: the Scripts side is too narrow for the three in a row
        if (gui.Button("Cancel") || pressed == PromptButton.Cancel)
            Close();

        return choice;
    }
}
