using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using VSRidged;

namespace TerraFirmaLike.Blocks
{
    class BlockSand : Block
    {
        BlockPos[] cardinal;
        BlockPos[] circle;
        RidgedNoise genNoise;
        const int radius = 4;
        IBulkBlockAccessor bbA;
        public override void OnLoaded(ICoreAPI api)
        {
            double[] frequencies = new double[3];
            double[] amplitudes = new double[3];
            for (int i = 0; i < 3; i++)
            {
                frequencies[i] = Math.Pow(3, i) * 1 / 4;
                amplitudes[i] = Math.Pow(7, i);
            }

            genNoise = new RidgedNoise(amplitudes, frequencies, api.World.Seed);

            cardinal = AreaMethods.CardinalOffsetList().ToArray();
            circle = AreaMethods.CircularOffsetList(radius).ToArray();
            bbA = api.World.BulkBlockAccessor;

            base.OnLoaded(api);
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace)
        {
            Block block = blockAccessor.GetBlock(pos);
            BlockPos dPos = new BlockPos(pos.X, pos.Y - 1, pos.Z);
            

            ushort id = blockAccessor.GetMapChunkAtBlockPos(pos).TopRockIdMap.FirstOrDefault();
            Block idBlock = blockAccessor.GetBlock(id);
            Block dBlock = blockAccessor.GetBlock(dPos);

            if (this == dBlock || blockAccessor.GetClimateAt(pos).Rainfall > 0.65) return false;
            

            if (idBlock.LastCodePart() == LastCodePart())
            {
                foreach (var v in cardinal)
                {
                    BlockPos rPos = new BlockPos(dPos.X + v.X, dPos.Y, dPos.Z + v.Z);
                    BlockPos uPos = new BlockPos(pos.X + v.X, pos.Y + v.Y + 1, pos.Z + v.Z);
                    Block uBlock = blockAccessor.GetBlock(uPos);
                    Block rBlock = blockAccessor.GetBlock(rPos);

                    if (rBlock.IsWater() && !NotReplacable(uBlock))
                    {
                        MakeBeach(dPos, blockAccessor, idBlock);
                        return true;
                    }
                }
            }

            return false;
        }

        public void MakeBeach(BlockPos pos, IBlockAccessor bA, Block idBlock)
        {
            foreach (var val in circle)
            {
                BlockPos tPos = new BlockPos(pos.X + val.X, pos.Y + val.Y, pos.Z + val.Z);
                Block block = bA.GetBlock(tPos);
                double noise = genNoise.Noise(tPos.X, tPos.Y, tPos.Z);
                if (NotReplacable(block)) continue;
                ushort id = BlockId;
                if (noise > 0.5)
                {
                    if (noise > 0.9)
                    {
                        continue;
                    }
                    else
                    {
                        id = bA.GetBlock(new AssetLocation("gravel-" + idBlock.LastCodePart())).BlockId;
                    }
                }

                BlockPos uPos = tPos.UpCopy();
                Block uBlock = bA.GetBlock(uPos);

                if (NotReplacable(uBlock)) bA.SetBlock(0, uPos);

                for (int i = 0; i < 4; i++)
                {
                    BlockPos ipos = uPos.AddCopy(0, i, 0);
                    Block iblock = bA.GetBlock(ipos);

                    if (iblock is BlockPlant) { bA.SetBlock(0, ipos); break; };
                    if (NotReplacable(iblock)) break;

                    if (iblock.CollisionBoxes != null) bA.SetBlock(id, ipos);
                }
                

                bA.SetBlock(id, tPos);
                bA.SetBlock(id, tPos.Add(new BlockPos(0,-1,0)));
            }
        }

        public bool NotReplacable(Block block)
        {
            return (block.IsLiquid() || block == this || block is BlockBamboo || block is BlockReeds || block is BlockWaterLily || block.FirstCodePart() == "log" || block.FirstCodePart() == "rawclay");
        }
    }
}
