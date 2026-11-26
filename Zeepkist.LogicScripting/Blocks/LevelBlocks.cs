using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

internal static class LevelBlocks
{
    /// <summary>
    /// Reads the editor's own block dictionary rather than <see cref="StaticConnectorTracker"/>, which can still hold a
    /// block after undo removes it.
    /// </summary>
    public static BlockProperties Find(LEV_LevelEditorCentral central, string uid)
    {
        return uid != null && central.undoRedo.allBlocksDictionary.TryGetValue(uid, out var block) && block != null
            ? block
            : null;
    }

    /// <summary>
    /// Every block in the editor's own block dictionary, leaving out the destroyed ones it can still hold.
    /// </summary>
    public static IEnumerable<BlockProperties> GetAll(LEV_LevelEditorCentral central)
    {
        return central.undoRedo.allBlocksDictionary.Values.Where(block => block != null);
    }
}
