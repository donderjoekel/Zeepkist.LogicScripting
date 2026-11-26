using TNRD.Zeepkist.LogicScripting.Compiler;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

internal static class BlockJson
{
    /// <summary>
    /// A new block with a new UID, at its own scale and without Settings, turned <paramref name="yaw"/> degrees.
    /// </summary>
    public static BlockPropertyJSON CreateNew(int blockId, Vector3 position, float yaw)
    {
        return new BlockPropertyJSON
        {
            i = blockId,
            u = PlayerManager.Instance.GenerateUniqueIDforBlocks(blockId.ToString()),
            p = new CV3(position),
            r = new CV3(new Vector3(0, yaw, 0)),
            s = new CV3(Vector3.one),
            d = new PropertyDictionariesJSON(),
        };
    }

    public static void Write(PropertyDictionariesJSON properties, Setting setting)
    {
        if (setting.Kind == SettingKind.Int)
            properties.SetIntDictionaryValue(setting.Key, setting.IntValue);
        else
            properties.SetStringDictionaryValue(setting.Key, setting.TextValue);
    }
}
