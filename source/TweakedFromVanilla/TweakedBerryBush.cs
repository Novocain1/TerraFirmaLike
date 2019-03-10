using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TerraFirmaLike.TweakedFromVanilla
{
    class TweakedBerryBush : BlockBerryBush
    {
        private int[] seasonrange;
        private int[] temprange;
        private int lifespan;
        private bool spawnonpeat = false;

        private Block ripe;
        private Block flowering;
        private Block empty;
        private Block dBlock;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            seasonrange = Attributes["seasonrange"].AsObject<int[]>();
            temprange = Attributes["temperaturerange"].AsObject<int[]>();
            spawnonpeat = Attributes["specialspawnonpeat"].AsBool(false);
            lifespan = Attributes["lifespan"].AsInt();

            ripe = api.World.BlockAccessor.GetBlock(CodeWithPart("ripe", 2));
            flowering = api.World.BlockAccessor.GetBlock(CodeWithPart("flowering", 2));
            empty = api.World.BlockAccessor.GetBlock(CodeWithPart("empty", 2));
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace)
        {
            dBlock = blockAccessor.GetBlock(pos.X, pos.Y - 1, pos.Z);
            if (spawnonpeat && dBlock.LastCodePart() != "peat") {
                return false;
            }
            Block block = blockAccessor.GetBlock(pos);
            if (block.IsReplacableBy(this) && CanPlantStay(blockAccessor, pos))
            {
                if ((LastCodePart() == "ripe" || LastCodePart() == "flowering") && Atlas.currentSeason >= seasonrange[0] && Atlas.currentSeason <= seasonrange[1])
                {
                    blockAccessor.SetBlock(BlockId, pos);
                }
                else
                {
                    blockAccessor.SetBlock(empty.BlockId, pos);
                }

                blockAccessor.SpawnBlockEntity(EntityClass, pos);
                return true;
            }
            return false;
        }

        public bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
        {
            return dBlock.Fertility > 0;
        }
    }
}
