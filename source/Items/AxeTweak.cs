using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TerraFirmaLike.Items
{
    class AxeTweak : ItemAxe
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, ref handling);
            handling = EnumHandHandling.PreventDefault;

            if (blockSel == null) return;

            var bbA = byEntity.World.BulkBlockAccessor;
            Block block = bbA.GetBlock(blockSel.Position);
            Block uBlock = bbA.GetBlock(blockSel.Position.UpCopy());

            if (uBlock.BlockId == 0 && block.FirstCodePart() == "log")
            {
                Block tblock = bbA.GetBlock(new AssetLocation("stackcombiner-" + block.LastCodePart(1)));
                bbA.SetBlock(tblock.BlockId, blockSel.Position.UpCopy());
                bbA.Commit();
            }
        }
    }
}
