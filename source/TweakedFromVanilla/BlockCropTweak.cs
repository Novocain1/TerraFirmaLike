using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Items;
using TerraFirmaLike.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TerraFirmaLike.TweakedFromVanilla
{
    class BlockCropTweaked : BlockCrop
    {
        double GrownAt;

        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            extra = null;
            if (Atlas.seasonString != "Winter" && world.Calendar.TotalHours > GrownAt)
            {
                extra = GetNextGrowthStageBlock(world, pos);
                return true;
            }
            return false;
        }

        public Block GetNextGrowthStageBlock(IWorldAccessor world, BlockPos pos)
        {
            int nextStage = CurrentStage() + 1;
            Block block = world.GetBlock(CodeWithParts(nextStage.ToString()));
            if (block == null)
            {
                nextStage = 1;
            }
            return world.GetBlock(CodeWithParts(nextStage.ToString()));
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace)
        {
            if (CurrentStage() == CropProps.GrowthStages && Atlas.seasonString == "Autumn")
            {
                GrownAt = GetGrownAt();
                return true;
            }
            if (CurrentStage() != CropProps.GrowthStages && Atlas.seasonString == "Autumn")
            {
                return false;
            }

            GrownAt = GetGrownAt();
            return true;
        }

        public double GetGrownAt()
        {
            return Atlas.cal.TotalHours + ((24 * 16) + Atlas.sapi.World.Rand.Next(1,4));
        }
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            foreach (BlockBehavior behavior in BlockBehaviors)
            {
                EnumHandling handling = EnumHandling.PreventSubsequent;
                behavior.OnBlockBroken(world, pos, byPlayer, ref handling);
            }

            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                ItemStack[] drops = GetDrops(world, pos, byPlayer, dropQuantityMultiplier);

                if (drops != null)
                {
                    foreach (var val in drops)
                    {
                        if (val.Collectible is TFLFood)
                        {
                            int prevsize = val.StackSize;
                            ItemStack stack = new ItemStack(val.Collectible);
                            stack.Attributes.SetInt("durability", 160);
                            
                            val.StackSize = 1;
                            for (int i = 0; i < prevsize; i++)
                            {
                                world.SpawnItemEntity(val.Clone(), new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                            }
                        }
                        else
                        {
                            world.SpawnItemEntity(val, new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                        }
                    }
                }

                world.PlaySoundAt(Sounds?.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer);
            }

            if (EntityClass != null)
            {
                BlockEntity entity = world.BlockAccessor.GetBlockEntity(pos);
                if (entity != null)
                {
                    entity.OnBlockBroken();
                }
            }

            world.BlockAccessor.SetBlock(0, pos);
        }
    }
}
