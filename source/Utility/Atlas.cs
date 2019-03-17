using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.TweakedFromVanilla;
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
    class Atlas : ModSystem
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

        public static Dictionary<int, string> GetMonth = Climate.GetMonthString();
        public static Dictionary<int, string> GetSeason = Climate.GetSeasonString();

        public long currentTime;
        public static GameCalendar cal;

        public override void Start(ICoreAPI api)
        {
            coreapi = api;
            if (api.Side.IsClient())
            {
                capi = (ICoreClientAPI)api;
            }
            if (api.Side.IsServer())
            {
                sapi = (ICoreServerAPI)api;
                sapi.Event.SaveGameLoaded += InitServer;
                sapi.Event.PlayerJoin += InitClient;
            }
        }

        public void InitClient(IPlayer player)
        {
            coreapi.World.RegisterGameTickListener(CheckThirst, 1000);
        }

        public void InitServer()
        {
            bbA = sapi.World.BulkBlockAccessor;
            DailyTemps = Climate.GenDailyTemps();
            cal = (GameCalendar)sapi.World.Calendar;
            cal.DaysPerYear = DaysPerYear;
            seaLevel = sapi.World.SeaLevel;
            worldHeight = sapi.World.BlockAccessor.MapSizeY;

            currentTime = sapi.World.RegisterGameTickListener(GetTime, 1000);
        }

        public static void GetTime(float dt)
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

        public void CheckThirst(float dt)
        {
            if (sapi == null || capi == null) return;

            if (capi.Input.MouseButton.Right)
            {
                CT();
            }
        }

        public void CT()
        {
            string id = capi.World.Player.PlayerUID;
            IClientPlayer cplayer = capi.World.PlayerByUid(id) as IClientPlayer;
            IServerPlayer splayer = sapi.World.PlayerByUid(id) as IServerPlayer;
            ITreeAttribute hungertree = splayer.Entity.WatchedAttributes.GetTreeAttribute("hunger");

            if (cplayer.Entity.BlockSelection != null)
            {
                BlockPos pos = cplayer.Entity.BlockSelection.Position;
                Block block = bbA.GetBlock(pos.X, pos.Y + 1, pos.Z);

                if (block.IsWater())
                {
                    float? thirst = hungertree.TryGetFloat("currentthirst");
                    float? maxthirst = hungertree.TryGetFloat("maxthirst");

                    if (thirst < maxthirst)
                    {
                        hungertree.SetFloat("currentthirst", (float)thirst + 2.0f);
                        splayer.Entity.WatchedAttributes.MarkPathDirty("hunger");
                        cplayer.Entity.PlayEntitySound("drink");
                    }
                }
            }
        }


    }
}
