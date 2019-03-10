using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TerraFirmaLike.Blocks
{
    class BlockSand : Block
    {
        BlockPos[] cardinal;
        BlockPos[] circle;
        const int radius = 4;
        IBulkBlockAccessor bbA;
        public override void OnLoaded(ICoreAPI api)
        {
            cardinal = AreaMethods.CardinalOffsetList().ToArray();
            circle = AreaMethods.CircularOffsetList(radius).ToArray();
            bbA = api.World.BulkBlockAccessor;

            base.OnLoaded(api);
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace)
        {
            Block block = blockAccessor.GetBlock(pos);
            BlockPos dPos = new BlockPos(pos.X, pos.Y - 1, pos.Z);
            Block dBlock = blockAccessor.GetBlock(dPos);
            if (this == dBlock || blockAccessor.GetClimateAt(pos).Rainfall > 0.5) return false;

            if (dBlock.LastCodePart(1) == LastCodePart())
            {
                foreach (var v in cardinal)
                {
                    BlockPos rPos = new BlockPos(dPos.X + v.X, dPos.Y, dPos.Z + v.Z);
                    Block rBlock = blockAccessor.GetBlock(rPos);

                    if (rBlock.IsWater())
                    {
                        MakeBeach(dPos, blockAccessor);
                        return true;
                    }
                }
            }

            return false;
        }

        public void MakeBeach(BlockPos pos, IBlockAccessor bA)
        {
            foreach (var val in circle)
            {
                BlockPos tPos = new BlockPos(pos.X + val.X, pos.Y + val.Y, pos.Z + val.Z);
                Block block = bA.GetBlock(tPos);
                if (Replacable(block)) continue;

                BlockPos uPos = tPos.UpCopy();
                Block uBlock = bA.GetBlock(uPos);

                if (uBlock is BlockPlant) bA.SetBlock(0, uPos);

                for (int i = 0; i < 4; i++)
                {
                    BlockPos ipos = uPos.AddCopy(0, i, 0);
                    Block iblock = bA.GetBlock(ipos);

                    if (iblock is BlockPlant) { bA.SetBlock(0, ipos); break; };
                    if (Replacable(iblock)) break;
                    if (iblock.CollisionBoxes != null) bA.SetBlock(BlockId, ipos);
                }
                

                bA.SetBlock(BlockId, tPos);
                bA.SetBlock(BlockId, tPos.Add(new BlockPos(0,-1,0)));
            }
        }

        public bool Replacable(Block block)
        {
            return (block.IsLiquid() || block.Id == 0 || block == this || block is BlockBamboo || block.FirstCodePart() == "log");
        }
    }
}
