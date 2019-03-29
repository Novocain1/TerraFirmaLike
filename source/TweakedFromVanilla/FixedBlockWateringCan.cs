using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TerraFirmaLike.TweakedFromVanilla
{
    public class FixedBlockWateringCan : BlockWateringCan
    {

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null) return false;
            if (slot.Itemstack.TempAttributes.GetInt("refilled") > 0) return false;

            float prevsecondsused = slot.Itemstack.TempAttributes.GetFloat("secondsUsed");
            slot.Itemstack.TempAttributes.SetFloat("secondsUsed", secondsUsed);

            float remainingwater = GetRemainingWateringSeconds(slot.Itemstack);
            SetRemainingWateringSeconds(slot.Itemstack, remainingwater -= secondsUsed - prevsecondsused);


            if (remainingwater <= 0) return false;

            IWorldAccessor world = byEntity.World;

            BlockPos targetPos = blockSel.Position;

            Block facingBlock = world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(blockSel.Face));
            if (facingBlock.Code.Path == "fire")
            {
                world.BlockAccessor.SetBlock(0, blockSel.Position.AddCopy(blockSel.Face));
            }

            Block block = world.BlockAccessor.GetBlock(blockSel.Position);
            bool notOnSolidblock = false;
            if ((block.CollisionBoxes == null || block.CollisionBoxes.Length == 0) && !block.IsLiquid())
            {
                notOnSolidblock = true;
                targetPos = targetPos.DownCopy();
            }

            FixedBlockEntityFarmland be = world.BlockAccessor.GetBlockEntity(targetPos) as FixedBlockEntityFarmland;
            if (be != null)
            {
                be.WaterFarmland(secondsUsed - prevsecondsused);
            }

            float speed = 4f;

            if (world.Side == EnumAppSide.Client)
            {
                ModelTransform tf = new ModelTransform();
                tf.EnsureDefaultValues();

                tf.Origin.Set(0.5f, 0.2f, 0.5f);
                tf.Translation.Set(-Math.Min(0.25f, speed * secondsUsed / 4), 0, 0);

                tf.Rotation.Z = GameMath.Min(60, secondsUsed * 90 * speed, 80 - remainingwater * 5);
                byEntity.Controls.UsingHeldItemTransformBefore = tf;
            }

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);


            if (secondsUsed > 1 / speed)
            {
                Vec3d pos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
                if (notOnSolidblock) pos.Y = (int)pos.Y + 0.05;
                WaterParticles.minPos = pos.Add(-0.125 / 2, 1 / 16f, -0.125 / 2);
                byEntity.World.SpawnParticles(WaterParticles, byPlayer);
            }

            return true;
        }
    }
}
