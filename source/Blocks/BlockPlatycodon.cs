using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TerraFirmaLike.Blocks
{
    class BlockPlatycodon : BlockPlant
    {

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace)
        {
            Block block = blockAccessor.GetBlock(pos);
            Block belowBlock = blockAccessor.GetBlock(pos.DownCopy());

            if (block.IsReplacableBy(this) && belowBlock.WildCardMatch(new AssetLocation("*clay*")) && !belowBlock.WildCardMatch(new AssetLocation("*stone*")))
            {
                blockAccessor.SetBlock(BlockId, pos);
                return true;
            }
            return false;
        }

        public override void OnNeighourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            Block block = world.BlockAccessor.GetBlock(pos);
            Block belowBlock = world.BlockAccessor.GetBlock(pos.DownCopy());

            if (belowBlock.IsReplacableBy(block))
            {
                world.BlockAccessor.BreakBlock(pos, null);
                world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
            }
        }
    }
}
