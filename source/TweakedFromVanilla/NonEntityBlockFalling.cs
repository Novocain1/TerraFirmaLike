using System.Collections.Generic;
using System.Linq;
using TerraFirmaLike.Utility;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace TerraFirmaLike.TweakedFromVanilla
{
    public class NonEntityBlockFalling : BlockBehavior
    {
        BlockPos[] largeAreaBelowOffsets;
        BlockPos[] cardinalOffsets;
        BlockPos[] areaBelowOffsets;

        List<BlockPos> possiblePos = new List<BlockPos>();
        public bool isGrass = false;
        public bool isSoil = false;
        public bool isIce = false;
        public Block tBlock;

        public NonEntityBlockFalling(Block block) : base(block)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            largeAreaBelowOffsets = AreaMethods.LargeAreaBelowOffsetList().ToArray();
            cardinalOffsets = AreaMethods.CardinalOffsetList().ToArray();
            areaBelowOffsets = AreaMethods.AreaBelowOffsetList().ToArray();

            var bbA = api.World.BulkBlockAccessor;

            isGrass = (block.FirstCodePart() == "soil" && block.LastCodePart() != "none");
            isSoil = (block.FirstCodePart() == "soil");
            isIce = (block.FirstCodePart() == "glacierice" || block.FirstCodePart() == "lakeice");

            if (isGrass)
            {
                tBlock = bbA.GetBlock(block.CodeWithPart("none", 3));
            }
            else
            {
                tBlock = block;
            }

        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
        {
            if (world.NearestPlayer(pos.X, pos.Y, pos.Z) == null) return;
            float distance = GameMath.Sqrt(world.NearestPlayer(pos.X, pos.Y, pos.Z).Entity.ServerPos.AsBlockPos.HorDistanceSqTo(pos.X, pos.Z));

            if (pos.Y < 1 || distance > 64) return;

            if (world.Side.IsServer())
            {
                world.RegisterCallbackUnique(UpdateFall, pos, 50);
            }
        }

        public override void OnNeighourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            if (world.NearestPlayer(pos.X, pos.Y, pos.Z) == null) return;
            float distance = GameMath.Sqrt(world.NearestPlayer(pos.X, pos.Y, pos.Z).Entity.ServerPos.AsBlockPos.HorDistanceSqTo(pos.X, pos.Z));

            if (world.BlockAccessor.GetBlock(neibpos).Class == "BlockPlant" || world.BlockAccessor.GetBlock(neibpos).IsLiquid() || pos.Y < 1 || distance > 64) return;

            if (world.Side.IsServer())
            {
                world.RegisterCallbackUnique(UpdateFall, pos, 50);
            }
        }

        public void UpdateFall(IWorldAccessor world, BlockPos pos, float dt)
        {
            if (AreaMethods.IsBeamNearby(world, pos, cardinalOffsets, largeAreaBelowOffsets, true)) return;


            var bbA = world.BulkBlockAccessor;

            if ((isSoil || isIce) && StuckCheck(world.BulkBlockAccessor, pos)) return;

            possiblePos.Clear();

            BlockPos dPos = new BlockPos(pos.X, pos.Y - 1, pos.Z);
            Block dBlock = bbA.GetBlock(dPos);
            BlockPos nextPos = pos;

            if (dBlock.IsReplacableBy(block))
            {
                nextPos = dPos;
            }
            else
            {

                foreach (var val in areaBelowOffsets)
                {
                    Block oBlock = bbA.GetBlock(pos.X + val.X, pos.Y + val.Y, pos.Z + val.Z);

                    if (oBlock.IsReplacableBy(block))
                    {
                        possiblePos.Add(new BlockPos(pos.X + val.X, pos.Y + val.Y, pos.Z + val.Z));
                    }

                }
                if (possiblePos.Count == 0)
                {
                    return;
                }
                nextPos = possiblePos[world.Rand.Next(0, possiblePos.Count)];
            }

            bbA.SetBlock(0, pos);
            bbA.SetBlock(tBlock.BlockId, nextPos);
            bbA.TriggerNeighbourBlockUpdate(pos);
            bbA.Commit();
            world.PlaySoundAt(block.Sounds.Break, pos.X, pos.Y, pos.Z);
        }
        public bool StuckCheck(IBulkBlockAccessor bbA, BlockPos pos)
        {
            return (bbA.GetBlock(pos.X + cardinalOffsets[0].X, pos.Y + cardinalOffsets[0].Y, pos.Z + cardinalOffsets[0].Z).CollisionBoxes != null &&
                    bbA.GetBlock(pos.X + cardinalOffsets[1].X, pos.Y + cardinalOffsets[1].Y, pos.Z + cardinalOffsets[2].Z).CollisionBoxes != null) || 
                    (bbA.GetBlock(pos.X + cardinalOffsets[2].X, pos.Y + cardinalOffsets[2].Y, pos.Z + cardinalOffsets[1].Z).CollisionBoxes != null &&
                    bbA.GetBlock(pos.X + cardinalOffsets[3].X, pos.Y + cardinalOffsets[3].Y, pos.Z + cardinalOffsets[3].Z).CollisionBoxes != null);
        }
    }
}
