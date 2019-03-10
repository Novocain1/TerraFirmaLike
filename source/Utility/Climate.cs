using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TerraFirmaLike.Utility
{
    class Climate
    {
        public static Dictionary<int, string> GetMonthString()
        {
            return new Dictionary<int, string>
            {
                { 1, "January" },
                { 2, "Febuary" },
                { 3, "March" },
                { 4, "April" },
                { 5, "May" },
                { 6, "June" },
                { 7, "July" },
                { 8, "August" },
                { 9, "September" },
                { 10, "October" },
                { 11, "November" },
                { 12, "December" },
            };
        }

        public static Dictionary<int, string> GetSeasonString()
        {
            return new Dictionary<int, string>
            {
                { 1, "Winter" },
                { 2, "Late Winter" },
                { 3, "Early Spring" },
                { 4, "Spring" },
                { 5, "Late Spring" },
                { 6, "Early Summer" },
                { 7, "Summer" },
                { 8, "Late Summer" },
                { 9, "Early Autumn" },
                { 10, "Autumn" },
                { 11, "Late Autumn" },
                { 12, "Early Winter" },
            };
        }

        public static BlockPos AveragePos(BlockPos[] positions)
        {
            int x = 0, y = 0, z = 0, length = positions.Length;
            foreach (BlockPos pos in positions)
            {
                x += pos.X;
                y += pos.Y;
                z += pos.Z;
            }
            return new BlockPos(x / length, y / length, z / length);
        }

        public static float[] GenDailyTemps()
        {
            float temp = -15.0f;
            Dictionary<int, float> temps = new Dictionary<int, float>();
            float[] tempsa = new float[Atlas.DaysPerYear];

            for (int i = 1; i < Atlas.DaysPerYear; i++)
            {
                if (i <= Atlas.DaysPerYear / 2)
                {
                    temp += 1.2f;
                    temps.Add(i, temp);
                    continue;
                }
                else
                {
                    temp -= 1.2f;
                    temps.Add(i, temp);
                    continue;
                }
            }
            foreach (var val in temps)
            {
                tempsa[val.Key] = val.Value;
            }
            return tempsa;
        }

        //player always spawns around 300000, north is less than this, south is more than this
        //we want the player to spawn in the northern hemisphere, in a temperate location

        public const int Spawn = 300000;
        public const int Arctic = Spawn - 10000;
        public const int Equator = Spawn + 10000;
        public const int Antarctic = Spawn + 20000;
        public const double pi = Math.PI;

        public static float GetBlockTemp(BlockPos pos)
        {
            float dtemp = Atlas.DailyTemps[Atlas.dayOfYear];
            float daylight = Atlas.cal.DayLightStrength;
            return GetTemp(pos, dtemp, daylight);
        }

        public static float AverageTemp(BlockPos pos)
        {
            float dtemp = Atlas.DailyTemps.Average();
            float daylight = 0.5f;
            return GetTemp(pos, dtemp, daylight);
        }

        public static float GetTemp(BlockPos pos, float dtemp, float daylight)
        {
            float maslmod = GetMASL(pos) / 2.0f;
            maslmod = GetMASL(pos) < 0 ? maslmod : GetMASL(pos) / 16.0f;
            float combinedtemp = (float)Math.Round(dtemp - maslmod + (daylight * 10.0f) - (Math.Abs(GetLat(pos)) / 500.0f) + Math.Sin(GetLng(pos) / 16), 2);

            return (float)Math.Round(GameMath.Clamp(combinedtemp + 5.0f, -14.24f, 44.61f), 2);
        }

        public static int GetLat(BlockPos pos)
        {
            int lat = -(pos.Z - Equator);
            return lat;
        }

        public static int GetLng(BlockPos pos)
        {
            int lng = pos.X - Spawn;
            return lng;
        }

        public static int GetMASL(BlockPos pos)
        {
            int masl = -(Atlas.seaLevel - pos.Y);
            return masl;
        }
        
        public static double RotateAngle(double input, double rotation)
        {
            return GameMath.Mod(input + rotation + 36000, 360);
        }

        public static float LatPercent(BlockPos pos)
        {
            if (pos.Z < Equator)
            {
                return -GetPercent(pos.Z, Equator, Arctic);
            }
            else
            {
                return GetPercent(pos.Z, Equator, Antarctic);
            }
        }

        public static float GetPercent(float input, float min, float max)
        {
            return (input - min) * 100 / (max - min);
        }
    }
}
