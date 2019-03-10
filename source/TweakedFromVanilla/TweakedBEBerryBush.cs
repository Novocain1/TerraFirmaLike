using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace TerraFirmaLike.TweakedFromVanilla
{
    class TweakedBEBerryBush : BlockEntity
    {
        public Block thisblock;
        public long growId;
        public double growTime;

        public int[] seasonrange;
        public int[] temprange;
        public int lifespan;
        public bool spawnonpeat = false;
        public Block ripe;
        public Block flowering;
        public Block empty;
        public string from;
        public string to;

        public TweakedBEBerryBush() : base()
        {
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            thisblock = api.World.BlockAccessor.GetBlock(pos);

            GetData();

            Atlas.GetSeason.TryGetValue(seasonrange[0], out from);
            Atlas.GetSeason.TryGetValue(seasonrange[1], out to);

            if (api is ICoreServerAPI)
            {
                UpdateGrowTime();
                growId = RegisterGameTickListener(CheckGrow, 2000);
            }
            
        }

        public void CheckGrow(float dt)
        {
            float localtemp = Climate.GetBlockTemp(pos);
            int thisseason = Atlas.currentSeason;

            if (thisblock != ripe && InSeason(localtemp, thisseason) && api.World.Calendar.TotalHours > growTime)
            {
                Grow();
            }
        }

        public bool InSeason(float localtemp, int thisseason)
        {
            return (localtemp >= temprange[0] && localtemp <= temprange[1]) && (thisseason >= seasonrange[0] && thisseason <= seasonrange[1]);
        }

        public void Grow()
        {
            UpdateGrowTime();
            Block nextBlock;
            nextBlock = thisblock == flowering ? ripe : flowering;
            api.World.BlockAccessor.SetBlock(nextBlock.BlockId, pos);
        }

        public void UpdateGrowTime()
        {
            growTime = api.World.Calendar.TotalHours + (2400 * 4);
        }

        public override string GetBlockInfo(IPlayer forPlayer)
        {
            return base.GetBlockInfo(forPlayer) + "Season Range: \n\u2022 From: " + from + "\n\u2022 To: " + to;
        }

        public void GetData()
        {
            seasonrange = thisblock.Attributes["seasonrange"].AsObject<int[]>();
            temprange = thisblock.Attributes["temperaturerange"].AsObject<int[]>();
            spawnonpeat = thisblock.Attributes["specialspawnonpeat"].AsBool(false);
            lifespan = thisblock.Attributes["lifespan"].AsInt();

            ripe = api.World.BlockAccessor.GetBlock(thisblock.CodeWithPart("ripe", 2));
            flowering = api.World.BlockAccessor.GetBlock(thisblock.CodeWithPart("flowering", 2));
            empty = api.World.BlockAccessor.GetBlock(thisblock.CodeWithPart("empty", 2));
        }
    }
}
