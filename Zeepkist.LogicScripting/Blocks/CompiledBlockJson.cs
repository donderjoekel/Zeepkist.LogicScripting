using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

internal static class CompiledBlockJson
{
    private const string KeyConnectionBase = "id";

    /// <param name="result">A successful Compile.</param>
    /// <param name="origin">World position of the group's center cell.</param>
    /// <param name="script">The Script whose Block Group the blocks join; the first block carries it.</param>
    public static List<BlockPropertyJSON> Create(CompileResult result, Vector3 origin, StoredScript script)
    {
        List<BlockPropertyJSON> jsons = [];
        foreach (var block in result.Blocks)
        {
            var position = origin + new Vector3(block.Cell.X, 0, block.Cell.Z) * GridCell.Size;
            var json = BlockJson.CreateNew(block.Style.BlockId, position, block.Yaw);
            LevelScripts.Tag(json.d, script.Id, json.u);
            foreach (var setting in block.Settings)
            {
                BlockJson.Write(json.d, setting);
            }

            jsons.Add(json);
        }

        if (jsons.Count > 0)
            LevelScripts.Write(jsons[0].d, script);

        foreach (var wire in result.Wires)
        {
            AddWire(jsons[wire.From.Block], wire.FromConnector, jsons[wire.To.Block], wire.ToConnector);
        }

        return jsons;
    }

    /// <param name="result">A successful Compile.</param>
    /// <param name="jsons">The blocks <see cref="Create"/> made from it.</param>
    public static List<BlockWire> CreatePlacedWires(CompileResult result, IReadOnlyList<BlockPropertyJSON> jsons)
    {
        List<BlockWire> wires = [];
        foreach (var wire in result.PlacedWires)
        {
            wires.Add(new BlockWire(GetUid(wire.From, jsons), wire.FromConnector, GetUid(wire.To, jsons), wire.ToConnector));
        }

        return wires;
    }

    private static string GetUid(WireEnd end, IReadOnlyList<BlockPropertyJSON> jsons)
    {
        return end.IsPlaced ? end.Uid : jsons[end.Block].u;
    }

    /// <summary>
    /// Both ends hold the Wire, as with LEV_ConnectorTool's Connect on both ends; the output end is the drawing leader.
    /// </summary>
    private static void AddWire(BlockPropertyJSON from, int fromConnector, BlockPropertyJSON to, int toConnector)
    {
        AddConnection(from, fromConnector, new ConnectionStruct(to.u, toConnector, true));
        AddConnection(to, toConnector, new ConnectionStruct(from.u, fromConnector, false));
    }

    private static void AddConnection(BlockPropertyJSON json, int connector, ConnectionStruct connection)
    {
        var countKey = KeyConnectionBase + connector;
        var count = json.d.GetIntDictionaryValue(countKey);
        json.d.SetStringDictionaryValue(countKey + "-" + count, connection.Serialize());
        json.d.SetIntDictionaryValue(countKey, count + 1);
    }
}
