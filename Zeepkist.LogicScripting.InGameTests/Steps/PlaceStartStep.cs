using System.Collections;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Places a start block on the ground below the camera, facing the way it looks, so the soapbox faces the blocks a
/// Compile places in front of the camera. It's one undo step, like placing it by hand.
/// </summary>
internal sealed class PlaceStartStep : ScenarioStep
{
    private const string UndoLabel = "In-game test: place start";
    private const float QuarterTurn = 90;

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        var blocks = central.manager.loader.globalBlockList.blocks;
        var startId = blocks.FindIndex(block => block != null && block.isStart);
        if (startId < 0)
        {
            context.Fail("the game has no start block");
            yield break;
        }

        var camera = central.cam.cameraTransform;
        var position = new Vector3(Mathf.Round(camera.position.x / GridCell.Size) * GridCell.Size, 0, Mathf.Round(camera.position.z / GridCell.Size) * GridCell.Size);
        var yaw = Mathf.Round(camera.eulerAngles.y / QuarterTurn) * QuarterTurn;
        var json = BlockJson.CreateNew(startId, position, yaw);

        BlockBatch.Apply(central, [json], [], [], UndoLabel);
        context.Detail = $"block {startId} at {position}, facing {yaw}°";
        yield return null;
    }
}
