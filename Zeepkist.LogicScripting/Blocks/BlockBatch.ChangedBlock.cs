namespace TNRD.Zeepkist.LogicScripting.Blocks;

internal static partial class BlockBatch
{
    private sealed class ChangedBlock
    {
        public ChangedBlock(BlockProperties block, string before)
        {
            Block = block;
            Before = before;
        }

        public BlockProperties Block { get; }
        public string Before { get; }

        /// <summary>
        /// Notes the block as it is now, so call it before changing the block.
        /// </summary>
        public static ChangedBlock Capture(BlockProperties block)
        {
            return new ChangedBlock(block, block.ConvertBlockToJSON_v15_string(true));
        }
    }
}
