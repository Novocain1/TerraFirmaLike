using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Items;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TerraFirmaLike.TweakedFromVanilla
{
    class HarvestableTweak : BlockBehaviorHarvestable
    {
        public HarvestableTweak(Block block) : base(block)
        {
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;

            if (block.Code.Path.Contains("ripe") && block.Drops != null && block.Drops.Length >= 1)
            {
                BlockDropItemStack drop = block.Drops.Length == 1 ? block.Drops[0] : block.Drops[1];

                ItemStack stack = drop.GetNextItemStack();
                if (stack.Collectible is TFLFood)
                {
                    int prevsize = stack.StackSize;
                    ItemStack thisstack = new ItemStack(stack.Collectible);
                    thisstack.Attributes.SetInt("durability", 160);
                    thisstack.StackSize = 1;

                    for (int i = 0; i < prevsize; i++)
                    {
                        if (!byPlayer.InventoryManager.TryGiveItemstack(thisstack.Clone()))
                        {
                            world.SpawnItemEntity(thisstack.Clone(), blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                        }
                    }
                }
                else if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    world.SpawnItemEntity(drop.GetNextItemStack(), blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                world.BlockAccessor.SetBlock(world.GetBlock(block.Code.CopyWithPath(block.Code.Path.Replace("ripe", "empty"))).BlockId, blockSel.Position);

                world.PlaySoundAt(new AssetLocation("sounds/block/plant"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);

                return true;
            }

            return false;
        }
    }
}
