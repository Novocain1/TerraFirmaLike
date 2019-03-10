using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TerraFirmaLike.BlockEntities
{
    class BlockEntityMudbrick : BlockEntity
    {
        double hoursTillDry;
        Block tBlock;
        long listenerID;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Block block = api.World.BlockAccessor.GetBlock(pos);
            tBlock = api.World.BlockAccessor.GetBlock(block.CodeWithPart("dry", 2));

            if (api is ICoreServerAPI && !block.WildCardMatch(new AssetLocation("*dry*")))
            {
                listenerID = RegisterGameTickListener(DryingTick, 10000);
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            hoursTillDry = ResetHours();
        }



        private void DryingTick(float dt)
        {
            var bA = api.World.BlockAccessor;
            BlockPos[] cardinal = AreaMethods.Cardinal(pos);
            Block block = bA.GetBlock(pos);

            if (!cardinal.All(val => bA.GetBlock(val).IsReplacableBy(block)))
            {
                hoursTillDry = ResetHours();
            }
            else if (api.World.Calendar.TotalHours > hoursTillDry && api.World.Calendar.DayLightStrength > 0.8)
            {
                bA.SetBlock(tBlock.BlockId, pos);
                api.World.UnregisterGameTickListener(listenerID);
            }
        }

        public double ResetHours()
        {
            return api.World.Calendar.TotalHours + 24 + api.World.Rand.NextDouble();
        }
    }
}
