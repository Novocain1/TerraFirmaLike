using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Items;
using TerraFirmaLike.Utility;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace TerraFirmaLike.BlockBehaviors
{
    class BehaviorCuttingBoard : BlockBehavior
    {
        IPlayer player;
        List<string> ing = new List<string>();
        string grain = "";
        int dmg = 0;
        bool ready = false;
        List<ItemStack> contains = new List<ItemStack>();
        BlockPos pos;

        public BehaviorCuttingBoard(Block block) : base(block)
        {
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            EmptyContents(world, null);
            base.OnBlockRemoved(world, pos, ref handling);
        }
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
            BlockPos dPos = pos.DownCopy();
            Block dBlock = world.BlockAccessor.GetBlock(dPos);
            if (dBlock.IsReplacableBy(block))
            {
                world.BlockAccessor.BreakBlock(pos, player);
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack == null) {
                EmptyContents(world, byPlayer);
                return true;
            }

            CollectibleObject obj = slot.Itemstack.Collectible;
            pos = blockSel.Position;
            player = byPlayer as IPlayer;

            if (obj is TFLFood)
            {
                int cD = slot.Itemstack.Attributes.GetInt("durability");
                cD = cD == 0 ? cD = 160 : cD;

                string[] nutrients = obj.Attributes["multinutrition"].AsObject<string[]>();
                if (obj.FirstCodePart() == "bread" && !ready)
                {
                    grain = obj.LastCodePart();
                    dmg += cD;
                    contains.Add(slot.Itemstack);
                    slot.Itemstack = null;
                    ready = true;
                }
                else if (obj.FirstCodePart() != "sandwich" && ready)
                {
                    foreach (var val in nutrients)
                    {
                        if (val == "grain") return false;
                        dmg += cD;
                        ing.Add(val);
                    }
                    contains.Add(slot.Itemstack);
                    slot.Itemstack = null;
                    if (ing.Count() > 3)
                    {
                        MakeSandwich(world, byPlayer);
                    }
                }
            }
            else
            {
                EmptyContents(world, byPlayer);
                return true;
            }
            slot.MarkDirty();
            return true;
        }

        public void EmptyContents(IWorldAccessor world, IPlayer byPlayer)
        {
            foreach (var val in contains)
            {
                if (byPlayer == null || !byPlayer.InventoryManager.TryGiveItemstack(val.Clone()))
                {
                    world.SpawnItemEntity(val.Clone(), pos.ToVec3d());
                }
            }
            ClearState();
        }

        public void ClearState()
        {
            contains.Clear();
            grain = "";
            ing.Clear();
            ready = false;
            dmg = 0;
        }

        public void MakeSandwich(IWorldAccessor world, IPlayer byPlayer)
        {
            string sandwich = "sandwich-";
            foreach (var val in ing)
            {
                sandwich += val + "-";
            }
            AssetLocation asset = new AssetLocation(sandwich + grain);
            ItemStack sandw = new ItemStack(world.GetItem(asset)){ StackSize = 1 };
            sandw.Attributes.SetInt("durability", dmg / (ing.Count() + 1));

            for (int i = 0; i < ing.Count() + 1; i++)
            {
                if (!byPlayer.InventoryManager.TryGiveItemstack(sandw.Clone()))
                {
                    world.SpawnItemEntity(sandw.Clone(), pos.ToVec3d());
                }
                    
            }

            ClearState();
        }
    }
}
