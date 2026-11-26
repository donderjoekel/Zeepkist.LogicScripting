using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

public sealed class LibraryReferenceEntry
{
    private const string SettingMark = " (setting)";
    private const string LibraryErrorsNote = "its Library has errors, so it can't be called";

    private readonly IReadOnlyList<string> _names;

    /// <summary>
    /// <paramref name="settingInputs"/> are the inputs the Function's body uses as a Setting.
    /// </summary>
    public LibraryReferenceEntry(
        string library,
        string functionName,
        IReadOnlyList<string> inputs,
        IReadOnlyCollection<string> settingInputs,
        IReadOnlyList<string> outputs,
        bool libraryHasErrors)
    {
        Function = $"{library}.{functionName}";
        Inputs = ReferenceText.JoinNames(inputs.Select(i => settingInputs.Contains(i) ? i + SettingMark : i));
        Outputs = ReferenceText.JoinNames(outputs);
        _names = inputs.Concat(outputs).ToList();
        LibraryHasErrors = libraryHasErrors;
        Layout = new ReferenceEntryLayout(
            Function,
            libraryHasErrors ? LibraryErrorsNote : string.Empty,
            [new ReferenceRow(ReferenceEntryLayout.InputsLabel, Inputs), new ReferenceRow(ReferenceEntryLayout.OutputsLabel, Outputs)],
            []);
    }

    /// <summary>
    /// The qualified name a Script calls, <c>timing.edge</c>.
    /// </summary>
    public string Function { get; }

    /// <summary>
    /// Each input used as a Setting is marked, as a call passes it a constant instead of a Wire.
    /// </summary>
    public string Inputs { get; }

    public string Outputs { get; }

    /// <summary>
    /// A Script can't call the Function until its Library's errors are fixed, as any of them stops every call into it.
    /// </summary>
    public bool LibraryHasErrors { get; }

    public ReferenceEntryLayout Layout { get; }

    /// <summary>
    /// Whether the qualified name, an input's or an output's name contains <paramref name="filter"/>, ignoring case;
    /// a blank filter matches every entry.
    /// </summary>
    public bool Matches(string filter)
    {
        var trimmed = filter.Trim();
        return trimmed.Length == 0 || ReferenceText.Contains(Function, trimmed) || _names.Any(n => ReferenceText.Contains(n, trimmed));
    }
}
