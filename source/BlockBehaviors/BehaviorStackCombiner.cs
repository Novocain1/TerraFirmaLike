using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Items;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TerraFirmaLike.BlockBehaviors
{
    class BehaviorStackCombiner : BlockBehavior
    {
        List<ItemStack> contains = new List<ItemStack>();
        BlockPos pos;
        const int mx = 160;

        public BehaviorStackCombiner(Block block) : base(block)
        {
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            pos = blockSel.Position;

            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack == null) {
                CombineStacks(world, byPlayer);
                return true;
            }

            if (slot.Itemstack.Collectible is TFLFood)
            {
                if (contains.Count() == 0 || contains.First().Collectible == slot.Itemstack.Collectible)
                {
                    contains.Add(slot.Itemstack);
                    slot.Itemstack = null;
                    return true;
                }
            }
            else
            {
                CombineStacks(world, byPlayer);
            }

            return true;
        }

        public void CombineStacks(IWorldAccessor world, IPlayer byPlayer)
        {
            if (contains.Count == 0) return;

            List<ItemStack> output = new List<ItemStack>();
            int CombinedDamageValues = 0;

            foreach (var val in contains)
            {
                var f = val.Attributes.GetInt("durability", mx);
                CombinedDamageValues += (f != 0 ? f : mx);
            }

            for (int i = CombinedDamageValues; i > 0; i -= mx)
            {
                var stack = new ItemStack(contains.First().Collectible);
                stack.Attributes.SetInt("durability", Math.Min(i, mx));
                output.Add(stack);
            }

            foreach (var val in output)
            {
                if (!byPlayer.InventoryManager.TryGiveItemstack(val.Clone()))
                {
                    world.SpawnItemEntity(val.Clone(), pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }

            contains.Clear();
            output.Clear();
        }
    }
}
