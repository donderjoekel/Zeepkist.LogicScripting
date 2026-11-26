using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Storage;

internal sealed class LibrariesFile
{
    public List<LibraryFileEntry> Libraries { get; set; } = [];
}
