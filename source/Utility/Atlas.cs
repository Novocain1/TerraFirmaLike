using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace TerraFirmaLike.Utility
{
    class Atlas : Block
    {
        public static int currentYear = 1;
        public static int currentMonth = 1;
        public static int currentSeason = 1;
        public static int dayOfMonth = 1;
        public static int dayOfYear = 1;

        public static string monthString = "";
        public static string seasonString = "";
        public static int seaLevel;
        public static int worldHeight;

        public const int DaysPerYear = 96;
        public const int DaysPerMonth = 8;
        public const int SeasonsPerYear = 12;
        public const int SeasonLength = DaysPerYear / SeasonsPerYear;
        public const int MonthsPerYear = 12;

        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;
        public static ICoreAPI coreapi;
        public static IBulkBlockAccessor bbA;
        public static float[] DailyTemps;

        long currentTime;
        public static GameCalendar cal;
        public override void OnLoaded(ICoreAPI api)
        {
            bbA = api.World.BulkBlockAccessor;
            DailyTemps = Climate.GenDailyTemps();
            cal = (GameCalendar)api.World.Calendar;
            cal.DaysPerYear = DaysPerYear;
            seaLevel = api.World.SeaLevel;
            worldHeight = api.World.BlockAccessor.MapSizeY;
            coreapi = api;

            if (api.Side == EnumAppSide.Server)
            {
                sapi = api as ICoreServerAPI;
                GetTime();
                currentTime = api.World.RegisterGameTickListener(CurrentTime, 1000);
            }

            if (api.Side == EnumAppSide.Client)
            {
                capi = api as ICoreClientAPI;
            }

            api.World.RegisterGameTickListener(GetThirst, 2000);

        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace)
        {
            return false;
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Server)
            {
                api.World.UnregisterGameTickListener(currentTime);
            }
        }

        public void CurrentTime(float dt)
        {
            GetTime();
        }

        public void GetThirst(float dt)
        {
            CheckThirst();
        }

        public static Dictionary<int, string> GetMonth = Climate.GetMonthString();
        public static Dictionary<int, string> GetSeason = Climate.GetSeasonString();

        public static void GetTime()
        {

            int td = (int)cal.TotalDays + 24;
            currentSeason = (td / SeasonLength % SeasonsPerYear) + 1;
            currentYear = (td / DaysPerYear) + 1;
            currentMonth = (td / DaysPerMonth % MonthsPerYear) + 1;
            dayOfMonth = (td % DaysPerMonth) + 1;
            dayOfYear = (DaysPerMonth * (currentMonth - 1)) + dayOfMonth;

            GetMonth.TryGetValue(currentMonth, out monthString);
            GetSeason.TryGetValue(currentSeason, out seasonString);
        }

        public static void CheckThirst()
        {
            if (capi == null || sapi == null) return;

            if (capi.Input.MouseButton.Right)
            {

                string id = capi.World.Player.PlayerUID;
                EntityPlayer splayer = sapi.World.PlayerByUid(id).Entity as EntityPlayer;
                EntityPlayer cplayer = capi.World.PlayerByUid(id).Entity as EntityPlayer;

                if (cplayer.BlockSelection != null)
                {
                    BlockPos pos = cplayer.BlockSelection.Position;
                    Block block = bbA.GetBlock(pos.X, pos.Y + 1, pos.Z);

                    if (block.IsWater())
                    {
                        ITreeAttribute stree = splayer.WatchedAttributes.GetTreeAttribute("hunger");
                        ITreeAttribute ctree = cplayer.WatchedAttributes.GetTreeAttribute("hunger");

                        float? cthirst = ctree.TryGetFloat("currentthirst");
                        float? cmaxthirst = ctree.TryGetFloat("maxthirst");
                        float? sthirst = stree.TryGetFloat("currentthirst");
                        float? smaxthirst = stree.TryGetFloat("maxthirst");

                        if (cthirst < cmaxthirst && sthirst < smaxthirst)
                        {
                            stree.SetFloat("currentthirst", (float)sthirst + 1.0f);
                            ctree.SetFloat("currentthirst", (float)cthirst + 1.0f);
                            splayer.PlayEntitySound("drink");
                        }
                    }
                }
            }
        }
    }
}
