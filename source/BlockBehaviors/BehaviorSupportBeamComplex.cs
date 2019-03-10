using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TerraFirmaLike.BlockBehaviors
{
    class BehaviorSupportBeamComplex : BlockBehavior
    {
        public BehaviorSupportBeamComplex(Block block) : base(block)
        {
        }

        public override void OnNeighourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            UpdateBlock(world, pos, neibpos);
        }

        public void UpdateBlock(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            var bA = world.BlockAccessor;
            Block block = bA.GetBlock(pos);
            Block dBlock = bA.GetBlock(pos.DownCopy());
            Block nBlock = bA.GetBlock(neibpos);
            Block[] cBlocks = AreaMethods.BlockCardinal(world, pos);
            Block[] blocks = AreaMethods.BlockFullCardinal(world, pos);
            int count = AreaMethods.CardinalCount(world, pos);
            AreaMethods.CountString().TryGetValue(count, out string sCount);
            AreaMethods.CardinalDict(pos).TryGetValue(neibpos, out string direction);

            //if (block.LastCodePart(1) == "vertical" || block.WildCardMatch(new AssetLocation("*corner*")))
            //{
            if (block.LastCodePart(1) != "horizontal" && (dBlock.IsReplacableBy(block) || blocks.All(val => val.IsReplacableBy(block))))
            {
                bA.BreakBlock(pos, null);
                return;
            }
            else if (block.LastCodePart(1) == "horizontal" && cBlocks.All(val => val.IsReplacableBy(block)))
            {
                bA.BreakBlock(pos, null);
                return;
            }
            //}

            if (direction != null && !dBlock.IsReplacableBy(block) && cBlocks.Any(val => block.WildCardMatch(new AssetLocation(block.CodeWithoutParts(2) + "*"))))
            {

                if (count != 0)
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(block.CodeWithoutParts(2) + "-corner" + sCount + "-" + direction)).BlockId, pos);
                }
                else
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(block.CodeWithoutParts(2) + "-vertical-" + direction)).BlockId, pos);
                }
            }
            else if (direction != null)
            {
                bA.SetBlock(bA.GetBlock(new AssetLocation(block.CodeWithoutParts(2) + "-horizontal-" + direction)).BlockId, pos);
            }
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
        {
        }

        public override void OnHeldInteractStart(IItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (blockSel == null) return;

            handHandling = EnumHandHandling.PreventDefault;

            var bA = byEntity.World.BlockAccessor;
            BlockPos pos = blockSel.Position;
            if (slot.Itemstack.StackSize > 2 && bA.GetBlock(pos.AddCopy(0, 3, 0)).IsReplacableBy(slot.Itemstack.Block) && blockSel.Face.Code == "up")
            {
                for (int i = 0; i < 3 && bA.GetBlock(pos.UpCopy().AddCopy(0, i, 0)).IsReplacableBy(slot.Itemstack.Block); i++)
                {
                    slot.Itemstack.StackSize -= 1;
                    bA.SetBlock(bA.GetBlock(slot.Itemstack.Collectible.Code).BlockId, pos.UpCopy().AddCopy(0, i, 0));
                }
                slot.MarkDirty();
            }
            else if (blockSel.Face.IsVertical)
            {
                handHandling = EnumHandHandling.NotHandled;
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, ref handHandling, ref handling);
            }
            else if (slot.Itemstack.StackSize > 2 && bA.GetBlock(pos.AddCopy(4, 0, 0)).WildCardMatch(new AssetLocation(slot.Itemstack.Block.CodeWithoutParts(2) + "*")) && blockSel.Face.IsHorizontal)
            {
                string direction = blockSel.Face.Code;
                for (int i = 0; i < 3; i++)
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(slot.Itemstack.Collectible.CodeWithoutParts(2) + "-horizontal-" + direction)).BlockId, pos.AddCopy(1+i,0,0));
                }
            }
            byEntity.World.PlaySoundAt(bA.GetBlock(slot.Itemstack.Collectible.Code).Sounds.Place, pos.X, pos.Y, pos.Z);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }
    }
}
