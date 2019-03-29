using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.BlockBehaviors;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace TerraFirmaLike.Items
{
    class TFLFood : Item
    {
        string[] multinutrition;
        int saturation;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            multinutrition = Attributes["multinutrition"].AsObject<string[]>();
            saturation = Attributes["saturation"].AsObject<int>();
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            float sat = GetSaturation(byEntity);
            float mSat = GetMaxSaturation(byEntity);
            if (sat >= mSat) return;

            if (multinutrition != null)
            {
                byEntity.World.RegisterCallback((dt) =>
                {
                    if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
                    {
                        IPlayer player = null;
                        if (byEntity is EntityPlayer) player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

                        byEntity.PlayEntitySound("eat", player);
                    }
                }, 500);

                byEntity.AnimManager?.StartAnimation("eat");

                handling = EnumHandHandling.PreventDefault;
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (multinutrition == null) return false;

            Vec3d pos = byEntity.Pos.AheadCopy(0.4f).XYZ;
            pos.Y += byEntity.EyeHeight - 0.4f;

            if (secondsUsed > 0.5f && (int)(30 * secondsUsed) % 7 == 1)
            {
                byEntity.World.SpawnCubeParticles(pos, slot.Itemstack, 0.3f, 4, 0.5f, (byEntity as EntityPlayer)?.Player);
            }


            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform tf = new ModelTransform();

                tf.EnsureDefaultValues();

                tf.Origin.Set(0f, 0, 0f);

                if (secondsUsed > 0.5f)
                {
                    tf.Translation.Y = Math.Min(0.02f, GameMath.Sin(20 * secondsUsed) / 10);
                }

                tf.Translation.X -= Math.Min(1f, secondsUsed * 4 * 1.57f);
                tf.Translation.Y += Math.Min(0.05f, secondsUsed * 2);

                tf.Rotation.X += Math.Min(30f, secondsUsed * 350);
                tf.Rotation.Y += Math.Min(80f, secondsUsed * 350);

                byEntity.Controls.UsingHeldItemTransformAfter = tf;

                return secondsUsed <= 1f;
            }

            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.World is IServerWorldAccessor && multinutrition != null && secondsUsed >= 0.95f)
            {
                RecieveMultiSat(byEntity);
                DamageItemByAmount(byEntity.World, byEntity, slot, 5);
                
                slot.MarkDirty();
            }
        }

        public virtual void DamageItemByAmount(IWorldAccessor world, EntityAgent byEntity, ItemSlot slot, int count)
        {
            int cD = slot.Itemstack.Attributes.GetInt("durability", Durability);
            slot.Itemstack.Attributes.SetInt("durability", cD - count);
            if (cD <= 0)
            {
                slot.Itemstack = null;
                world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
            }
            slot.MarkDirty();
        }

        public virtual void RecieveMultiSat(EntityAgent byEntity)
        {
            foreach (var val in multinutrition)
            {
                switch (val.ToLower())
                {
                    case "fruit":
                        byEntity.ReceiveSaturation(saturation, EnumFoodCategory.Fruit);
                        break;
                    case "vegetable":
                        byEntity.ReceiveSaturation(saturation, EnumFoodCategory.Vegetable);
                        break;
                    case "protein":
                        byEntity.ReceiveSaturation(saturation, EnumFoodCategory.Protein);
                        break;
                    case "grain":
                        byEntity.ReceiveSaturation(saturation, EnumFoodCategory.Grain);
                        break;
                    case "dairy":
                        byEntity.ReceiveSaturation(saturation, EnumFoodCategory.Dairy);
                        break;
                }
            }
        }

        public static float GetSaturation(EntityAgent byEntity)
        {
            ITreeAttribute tree = byEntity.WatchedAttributes.GetTreeAttribute("hunger");
            if (tree == null) byEntity.WatchedAttributes["hunger"] = tree = new TreeAttribute();

            return tree.GetFloat("currentsaturation");
        }

        public static float GetMaxSaturation(EntityAgent byEntity)
        {
            ITreeAttribute tree = byEntity.WatchedAttributes.GetTreeAttribute("hunger");
            if (tree == null) byEntity.WatchedAttributes["hunger"] = tree = new TreeAttribute();

            return tree.GetFloat("maxsaturation");
        }
    }
}
