using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace TerraFirmaLike.BlockBehaviors
{
    class BehaviorStoneCollapse : BlockBehavior
    {
        BlockPos[] largeAreaBelowOffsets;
        BlockPos[] cardinalOffsets;
        BlockPos[] areaAroundOffsets;
        string tCode;
        Block tBlock;
        bool cascadeActive = false;

        public BehaviorStoneCollapse(Block block) : base(block)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            var bBA = api.World.BulkBlockAccessor;
            largeAreaBelowOffsets = AreaMethods.LargeAreaBelowOffsetList().ToArray();
            cardinalOffsets = AreaMethods.CardinalOffsetList().ToArray();
            areaAroundOffsets = AreaMethods.AreaAroundOffsetList().ToArray();

            tCode = block.LastCodePart();
            tBlock = bBA.GetBlock(new AssetLocation("cobblestone-" + tCode));
        }

        public override void OnNeighourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            if (world.NearestPlayer(pos.X, pos.Y, pos.Z) == null) return;
            float distance = GameMath.Sqrt(world.NearestPlayer(pos.X, pos.Y, pos.Z).Entity.ServerPos.AsBlockPos.HorDistanceSqTo(pos.X, pos.Z));
            Block nBlock = world.BlockAccessor.GetBlock(neibpos);
            if (pos.Y < 1 || distance > 32 || tBlock == null) return;

            if (world.Side.IsServer() && nBlock.Id == 0)
            {
                world.RegisterCallbackUnique(CollapseCallback, pos, 100);
            }
        }

        public void CollapseCallback(IWorldAccessor world, BlockPos pos, float dt)
        {
            var bBA = world.BulkBlockAccessor;

            if (!bBA.GetBlock(pos.X, pos.Y - 1, pos.Z).IsReplacableBy(block) || bBA.GetBlock(pos.X, pos.Y + 1, pos.Z).Id == 0 || AreaMethods.IsBeamNearby(world, pos, areaAroundOffsets, largeAreaBelowOffsets, false)) return;
            double rng = world.Rand.NextDouble();

            if (cascadeActive)
            {
                bBA.TriggerNeighbourBlockUpdate(pos);
                if (rng < 0.2)
                {
                    cascadeActive = false;
                    return;
                }
            }

            if (rng < 0.1 || cascadeActive)
            {
                foreach (var v in areaAroundOffsets)
                {
                    Block oBlock = bBA.GetBlock(pos.X + v.X, pos.Y + v.Y, pos.Z + v.Z);
                    if (oBlock.IsReplacableBy(block))
                    {
                        bBA.SetBlock(tBlock.BlockId, pos);
                        bBA.Commit();
                        cascadeActive = true;
                        break;
                    }
                }

            }

        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            base.OnBlockBroken(world, pos, byPlayer, ref handling);
            world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
        }
    }
}
