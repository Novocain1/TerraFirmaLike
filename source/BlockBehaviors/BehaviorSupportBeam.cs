using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TerraFirmaLike.BlockBehaviors
{
    class BehaviorSupportBeam : BlockBehavior
    {
        public BehaviorSupportBeam(Block block) : base(block)
        {
        }

        public override void OnNeighourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            Block[] blocks = AreaMethods.BlockFullCardinal(world, pos);
            Block dBlock = world.BlockAccessor.GetBlock(pos.DownCopy());

            if (dBlock.IsReplacableBy(block) || blocks.All(val => val.IsReplacableBy(block)))
            {
                world.BlockAccessor.BreakBlock(pos, null);
                return;
            }
        }

        public override void OnHeldInteractStart(IItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (blockSel == null) return;

            handHandling = EnumHandHandling.PreventDefault;

            var bA = byEntity.World.BlockAccessor;
            BlockPos pos = blockSel.Position;
            if (slot.Itemstack.StackSize > 2 && bA.GetBlock(pos.X, pos.Y + 3, pos.Z).IsReplacableBy(slot.Itemstack.Block) && blockSel.Face.Code == "up")
            {
                for (int i = 1; i <= 3 && bA.GetBlock(pos.X, pos.Y + i, pos.Z).IsReplacableBy(slot.Itemstack.Block); i++)
                {
                    slot.Itemstack.StackSize -= 1;
                    bA.SetBlock(bA.GetBlock(slot.Itemstack.Collectible.Code).BlockId, new BlockPos(pos.X, pos.Y + i, pos.Z));
                }
                slot.MarkDirty();
                if (byEntity.Api.Side == EnumAppSide.Client)
                {
                    byEntity.World.PlaySoundAt(bA.GetBlock(slot.Itemstack.Collectible.Code).Sounds.Place, pos.X, pos.Y, pos.Z);
                }
            }
            else
            {
                handHandling = EnumHandHandling.NotHandled;
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, ref handHandling, ref handling);
            }
            
        }
    }
}
