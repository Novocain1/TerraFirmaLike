﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace TerraFirmaLike.TweakedFromVanilla
{
    class ItemHoeFixed : Item
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling)
        {
            if (blockSel == null) return;
            BlockPos pos = blockSel.Position;
            Block block = byEntity.World.BlockAccessor.GetBlock(pos);

            byEntity.Attributes.SetInt("didtill", 0);

            if (block.Code.Path.StartsWith("soil"))
            {
                handHandling = EnumHandHandling.PreventDefault;
            }
        }


        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null) return false;
            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
            if (!(block is BlockSoil)) return false;

            IPlayer byPlayer = (byEntity as EntityPlayer).Player;

            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform tf = new ModelTransform();
                tf.EnsureDefaultValues();

                float rotateToTill = GameMath.Clamp(secondsUsed * 18, 0, 2f);
                float scrape = GameMath.SmoothStep(1 / 0.4f * GameMath.Clamp(secondsUsed - 0.35f, 0, 1));
                float scrapeShake = secondsUsed > 0.35f && secondsUsed < 0.75f ? (float)(GameMath.Sin(secondsUsed * 50) / 60f) : 0;

                float rotateWithReset = Math.Max(0, rotateToTill - GameMath.Clamp(24 * (secondsUsed - 0.75f), 0, 2));
                float scrapeWithReset = Math.Max(0, scrape - Math.Max(0, 20 * (secondsUsed - 0.75f)));

                tf.Origin.Set(0f, 0, 0.5f);
                tf.Rotation.Set(0, rotateWithReset * 45, 0);
                tf.Translation.Set(scrapeShake, 0, scrapeWithReset / 2);

                byEntity.Controls.UsingHeldItemTransformBefore = tf;
            }

            if (secondsUsed > 0.35f && secondsUsed < 0.87f)
            {
                Vec3d dir = new Vec3d().AheadCopy(1, 0, byEntity.LocalPos.Yaw - GameMath.PI);
                Vec3d pos = blockSel.Position.ToVec3d().Add(0.5 + dir.X, 1.03, 0.5 + dir.Z);

                pos.X -= dir.X * secondsUsed * 1 / 0.75f * 1.2f;
                pos.Z -= dir.Z * secondsUsed * 1 / 0.75f * 1.2f;

                byEntity.World.SpawnCubeParticles(blockSel.Position, pos, 0.25f, 3, 0.5f, byPlayer);
            }

            if (secondsUsed > 0.6f && byEntity.Attributes.GetInt("didtill") == 0 && byEntity.World.Side == EnumAppSide.Server)
            {
                byEntity.Attributes.SetInt("didtill", 1);
                DoTill(secondsUsed, slot, byEntity, blockSel, entitySel);
            }

            return secondsUsed < 1;
        }


        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return false;
        }

        public void DoTill(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null) return;
            BlockPos pos = blockSel.Position;
            Block block = byEntity.World.BlockAccessor.GetBlock(pos);

            if (!block.Code.Path.StartsWith("soil")) return;


            string fertility = block.LastCodePart(2);
            Block farmland = byEntity.World.GetBlock(new AssetLocation("farmland-dry-" + block.FirstCodePart(2) + "-" + fertility));

            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            if (farmland == null || byPlayer == null) return;




            if (block.Sounds != null) byEntity.World.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z, null);

            byEntity.World.BlockAccessor.SetBlock(farmland.BlockId, pos);
            byEntity.World.BlockAccessor.MarkBlockDirty(pos);
            slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot);

            if (byEntity.World is IServerWorldAccessor)
            {
                BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(pos);
                if (be is FixedBlockEntityFarmland)
                {
                    ((FixedBlockEntityFarmland)be).CreatedFromSoil(block);
                }
            }
        }
    }
}
