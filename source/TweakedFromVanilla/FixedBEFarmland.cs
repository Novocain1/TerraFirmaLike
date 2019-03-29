using System;
using TerraFirmaLike.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace TerraFirmaLike.TweakedFromVanilla
{
    public class CodeAndChance
    {
        public AssetLocation Code;
        public float Chance;
    }

    public class FixedBlockEntityFarmland : BlockEntity, IFarmlandBlockEntity
    {
        static Random rand = new Random();
        static CodeAndChance[] weedNames;
        static float totalWeedChance;
        //float temp;

        public static OrderedDictionary<string, float> Fertilities = new OrderedDictionary<string, float>{
            { "verylow", 5 },
            { "low", 25 },
            { "medium", 50 },
            { "high", 75 },
        };


        BlockPos upPos;
        long growListenerId;
        double totalHoursForNextStage;
        double totalHoursFertilityCheck;

        float[] nutrients = new float[3];
        double lastWateredTotalHours = 0;

        float currentlyWateredSeconds;
        long lastWateredMs;
        


        public int originalFertility;                 
        TreeAttribute cropAttrs = new TreeAttribute();

        int delayGrowthBelowSunLight = 19;
        float lossPerLevel = 0.1f;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            upPos = base.pos.UpCopy();

            if (api is ICoreServerAPI)
            {
                //MyTemp(0.0f);
                //RegisterGameTickListener(MyTemp, 10000);

                if (HasUnripeCrop())
                {
                    growListenerId = RegisterGameTickListener(CheckGrow, 2000);
                }

                RegisterGameTickListener(SlowTick, 15000);
                
            }

            Block block = api.World.BlockAccessor.GetBlock(base.pos);
            if (block.Attributes != null)
            {
                delayGrowthBelowSunLight = block.Attributes["delayGrowthBelowSunLight"].AsInt(19);
                lossPerLevel = block.Attributes["lossPerLevel"].AsFloat(0.1f);

                if (weedNames == null)
                {
                    weedNames = block.Attributes["weedBlockCodes"].AsObject<CodeAndChance[]>();
                    for (int i = 0; weedNames != null && i < weedNames.Length; i++)
                    {
                        totalWeedChance += weedNames[i].Chance;
                    }
                }

            }
        }
        
        /*
        public void MyTemp(float dt)
        {
            temp = Climate.BlockTemp(pos);
        }
        */

        internal void CreatedFromSoil(Block block)
        {
            string fertility = block.LastCodePart(2);
            if (block is BlockFarmland)
            {
                fertility = block.LastCodePart(2);
            }
            originalFertility = (int)Fertilities[fertility];

            nutrients[0] = originalFertility;
            nutrients[1] = originalFertility;
            nutrients[2] = originalFertility;

            totalHoursFertilityCheck = api.World.Calendar.TotalHours;
        }


        private void SlowTick(float dt)
        {
            bool waterNearby = api.World.BlockAccessor.GetBlock(pos.X, pos.Y - 1, pos.Z).IsWater();

            for (int dx = -3; dx <= 3 && !waterNearby; dx++)
            {
                for (int dz = -3; dz <= 3; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    if (api.World.BlockAccessor.GetBlock(base.pos.X + dx, base.pos.Y, base.pos.Z + dz).IsWater())
                    {
                        waterNearby = true;
                        break;
                    }
                }
            }

            if (waterNearby)
            {
                lastWateredTotalHours = api.World.Calendar.TotalHours;
            }

            if (IsWatered)
            {
                Block block = api.World.BlockAccessor.GetBlock(base.pos);
                if (block.BlockMaterial == EnumBlockMaterial.Air)
                {
                    api.World.BlockAccessor.RemoveBlockEntity(base.pos);
                    return;
                }

                if (HasUnripeCrop() && growListenerId == 0)
                {
                    growListenerId = RegisterGameTickListener(CheckGrow, 2000);
                }
            }

            UpdateFarmlandBlock();


            Block upblock = api.World.BlockAccessor.GetBlock(upPos);
            if (upblock == null) return;

            double hoursPassed = api.World.Calendar.TotalHours - totalHoursFertilityCheck;

            double hoursRequired = (3 + rand.NextDouble());
            int fertilityGained = 0;
            bool growTallGrass = false;
            while (hoursPassed > hoursRequired)
            {
                fertilityGained++;
                hoursPassed -= hoursRequired;
                totalHoursFertilityCheck += hoursRequired;

                hoursRequired = (2 + rand.NextDouble());
                growTallGrass |= rand.NextDouble() < 0.006;
            }

            if (upblock.BlockMaterial == EnumBlockMaterial.Air && growTallGrass)
            {
                double rnd = rand.NextDouble() * totalWeedChance;
                for (int i = 0; i < weedNames.Length; i++)
                {
                    rnd -= weedNames[i].Chance;
                    if (rnd <= 0)
                    {
                        Block weedsBlock = api.World.GetBlock(weedNames[i].Code);
                        if (weedsBlock != null)
                        {
                            api.World.BlockAccessor.SetBlock(weedsBlock.BlockId, upPos);
                        }
                        break;
                    }
                }
            }


            if (fertilityGained > 0 && (nutrients[0] < originalFertility || nutrients[1] < originalFertility || nutrients[2] < originalFertility))
            {
                EnumSoilNutrient? currentlyConsumedNutrient = null;
                if (upblock.CropProps != null)
                {
                    currentlyConsumedNutrient = upblock.CropProps.RequiredNutrient;
                    fertilityGained /= 3;
                    if (HasRipeCrop()) fertilityGained = 0;
                }

                if (currentlyConsumedNutrient != EnumSoilNutrient.N) nutrients[0] = Math.Min(originalFertility, nutrients[0] + fertilityGained);
                if (currentlyConsumedNutrient != EnumSoilNutrient.P) nutrients[1] = Math.Min(originalFertility, nutrients[1] + fertilityGained);
                if (currentlyConsumedNutrient != EnumSoilNutrient.K) nutrients[2] = Math.Min(originalFertility, nutrients[2] + fertilityGained);

                api.World.BlockAccessor.MarkBlockEntityDirty(base.pos);
            }
        }

        private void CheckGrow(float dt)
        {
            if (Atlas.seasonString.Contains("Winter")) { api.World.BlockAccessor.BreakBlock(pos.UpCopy(), null); return; }
            double hoursNextStage = GetHoursForNextStage();

            if (api.World.ElapsedMilliseconds - lastWateredMs > 10000)
            {
                currentlyWateredSeconds = Math.Max(0, currentlyWateredSeconds - dt);
            }


            if (!IsWatered)
            {
                totalHoursForNextStage = api.World.Calendar.TotalHours + hoursNextStage;
                return;
            }

            int sunlight = api.World.BlockAccessor.GetLightLevel(base.pos.UpCopy(), EnumLightLevelType.MaxLight);
            double lightGrowthSpeedFactor = GameMath.Clamp(1 - (delayGrowthBelowSunLight - sunlight) * lossPerLevel, 0, 1);

            if (lightGrowthSpeedFactor <= 0)
            {
                return;
            }

            double lightHoursPenalty = hoursNextStage / lightGrowthSpeedFactor - hoursNextStage;


            while (api.World.Calendar.TotalHours > totalHoursForNextStage + lightHoursPenalty)
            {
                TryGrowCrop(totalHoursForNextStage);
                totalHoursForNextStage += hoursNextStage;
            }

            if (HasRipeCrop())
            {
                api.Event.UnregisterGameTickListener(growListenerId);
                growListenerId = 0;
            }
        }

        public double GetHoursForNextStage()
        {
            Block block = GetCrop();
            if (block == null) return 99999999;

            float stageHours = 24 * block.CropProps.TotalGrowthDays / block.CropProps.GrowthStages;

            stageHours *= LowNutrientPenalty(block.CropProps.RequiredNutrient);

            stageHours *= (float)(0.9 + 0.2 * rand.NextDouble());


            return stageHours;
        }

        public float LowNutrientPenalty(EnumSoilNutrient nutrient)
        {
            if (nutrients[(int)nutrient] > 75) return 1 / 1.1f;
            if (nutrients[(int)nutrient] > 50) return 1;
            if (nutrients[(int)nutrient] > 35) return 1 / 0.9f;
            if (nutrients[(int)nutrient] > 20) return 1 / 0.6f;
            if (nutrients[(int)nutrient] > 5) return 1 / 0.3f;
            return 1 / 0.1f;
        }

        public float DeathChance(int nutrientIndex)
        {
            if (nutrients[nutrientIndex] <= 5) return 0.5f;
            return 0f;
        }

        internal bool TryPlant(Block block)
        {
            if (CanPlant() && block.CropProps != null)
            {
                api.World.BlockAccessor.SetBlock(block.BlockId, upPos);
                totalHoursForNextStage = api.World.Calendar.TotalHours + GetHoursForNextStage();

                if (HasUnripeCrop() && api is ICoreServerAPI && growListenerId == 0)
                {
                    growListenerId = RegisterGameTickListener(CheckGrow, 2000);
                }

                foreach (CropBehavior behavior in block.CropProps.Behaviors)
                {
                    behavior.OnPlanted(api);
                }

                return true;
            }

            return false;
        }

        internal bool CanPlant()
        {
            Block block = api.World.BlockAccessor.GetBlock(upPos);
            return block == null || block.BlockMaterial == EnumBlockMaterial.Air || Atlas.currentSeason != 4;
        }

        internal bool HasUnripeCrop()
        {
            Block block = GetCrop();
            return block != null && CropStage(block) < block.CropProps.GrowthStages;
        }

        internal bool HasRipeCrop()
        {
            Block block = GetCrop();
            return block != null && CropStage(block) >= block.CropProps.GrowthStages;
        }

        internal bool TryGrowCrop(double currentTotalHours)
        {
            Block block = GetCrop();
            if (block == null) return false;

            int currentGrowthStage = CropStage(block);
            if (currentGrowthStage < block.CropProps.GrowthStages)
            {
                int newGrowthStage = currentGrowthStage + 1;

                Block nextBlock = api.World.GetBlock(block.CodeWithParts("" + newGrowthStage));
                if (nextBlock == null) return false;

                if (block.CropProps.Behaviors != null)
                {
                    EnumHandling handled = EnumHandling.PassThrough;
                    bool result = false;
                    foreach (CropBehavior behavior in block.CropProps.Behaviors)
                    {
                        result = behavior.TryGrowCrop(api, this, currentTotalHours, newGrowthStage, ref handled);
                        if (handled == EnumHandling.PreventSubsequent) return result;
                    }
                    if (handled == EnumHandling.PreventDefault) return result;
                }

                api.World.BlockAccessor.SetBlock(nextBlock.BlockId, upPos);
                ConsumeNutrients(block);
                return true;
            }
            return false;
        }

        private void ConsumeNutrients(Block cropBlock)
        {
            float nutrientLoss = cropBlock.CropProps.NutrientConsumption / cropBlock.CropProps.GrowthStages;
            nutrients[(int)cropBlock.CropProps.RequiredNutrient] = Math.Max(0, nutrients[(int)cropBlock.CropProps.RequiredNutrient] - nutrientLoss);
            UpdateFarmlandBlock();
        }


        void UpdateFarmlandBlock()
        {
            int nowLevel = FertilityLevel((nutrients[0] + nutrients[1] + nutrients[2]) / 3);
            Block farmlandBlock = api.World.BlockAccessor.GetBlock(pos);
            Block nextFarmlandBlock = api.World.GetBlock(farmlandBlock.CodeWithParts(IsWatered ? "moist" : "dry", farmlandBlock.LastCodePart(1), Fertilities.GetKeyAtIndex(nowLevel)));
            
            if (nextFarmlandBlock == null)
            {
                api.World.BlockAccessor.RemoveBlockEntity(pos);
                return;
            }

            if (farmlandBlock.BlockId != nextFarmlandBlock.BlockId)
            {
                api.World.BlockAccessor.ExchangeBlock(nextFarmlandBlock.BlockId, pos);
                api.World.BlockAccessor.MarkBlockEntityDirty(pos);
                api.World.BlockAccessor.MarkBlockDirty(pos);
            }
        }


        int FertilityLevel(float fertiltyValue)
        {
            int i = 0;
            foreach (var val in Fertilities)
            {
                if (val.Value >= fertiltyValue) return i;
                i++;
            }
            return 3;
        }

        internal Block GetCrop()
        {
            Block block = api.World.BlockAccessor.GetBlock(upPos);
            if (block == null || block.CropProps == null) return null;
            return block;
        }


        internal int CropStage(Block block)
        {
            int stage = 0;
            int.TryParse(block.LastCodePart(), out stage);
            return stage;
        }


        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);
            nutrients[0] = tree.GetFloat("n");
            nutrients[1] = tree.GetFloat("p");
            nutrients[2] = tree.GetFloat("k");
            lastWateredTotalHours = tree.GetDouble("lastWateredTotalHours");
            originalFertility = tree.GetInt("originalFertility");

            if (tree.HasAttribute("totalHoursForNextStage"))
            {
                totalHoursForNextStage = tree.GetDouble("totalHoursForNextStage");
                totalHoursFertilityCheck = tree.GetDouble("totalHoursFertilityCheck");
            }
            else
            {
                totalHoursForNextStage = tree.GetDouble("totalDaysForNextStage") * 24;
                totalHoursFertilityCheck = tree.GetDouble("totalDaysFertilityCheck") * 24;
            }

            TreeAttribute cropAttrs = tree["cropAttrs"] as TreeAttribute;
            if (cropAttrs == null) cropAttrs = new TreeAttribute();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("n", nutrients[0]);
            tree.SetFloat("p", nutrients[1]);
            tree.SetFloat("k", nutrients[2]);
            tree.SetDouble("lastWateredTotalHours", lastWateredTotalHours);
            tree.SetInt("originalFertility", originalFertility);
            tree.SetDouble("totalHoursForNextStage", totalHoursForNextStage);
            tree.SetDouble("totalHoursFertilityCheck", totalHoursFertilityCheck);
            tree["cropAttrs"] = cropAttrs;
        }


        public override string GetBlockInfo(IPlayer forPlayer)
        {
            return
                "Nutrient Levels: N " + (int)nutrients[0] + "%, P " + (int)nutrients[1] + "%, K " + (int)nutrients[2] + "%\n" +
                "Growth Speeds: N " + Math.Round(100 * 1 / LowNutrientPenalty(EnumSoilNutrient.N), 0) + "%, P " + Math.Round(100 * 1 / LowNutrientPenalty(EnumSoilNutrient.P), 0) + "%, K " + Math.Round(100 * 1 / LowNutrientPenalty(EnumSoilNutrient.K), 0)
                + "\n" + "Temperature: " + Climate.GetBlockTemp(pos)
            ;
        }

        public void WaterFarmland(float dt, bool waterNeightbours = true)
        {
            currentlyWateredSeconds += dt;
            lastWateredMs = api.World.ElapsedMilliseconds;

            if (currentlyWateredSeconds > 1f)
            {
                if (IsWatered && waterNeightbours)
                {
                    foreach (BlockFacing neib in BlockFacing.HORIZONTALS)
                    {
                        BlockPos npos = pos.AddCopy(neib);
                        BlockEntityFarmland bef = api.World.BlockAccessor.GetBlockEntity(npos) as BlockEntityFarmland;
                        if (bef != null) bef.WaterFarmland(1.01f, false);
                    }
                }

                lastWateredTotalHours = api.World.Calendar.TotalHours;
                UpdateFarmlandBlock();
                currentlyWateredSeconds--;
            }

        }

        public double TotalHoursForNextStage
        {
            get
            {
                return totalHoursForNextStage;
            }
        }

        public double TotalHoursFertilityCheck
        {
            get
            {
                return totalHoursFertilityCheck;
            }
        }

        public float[] Nutrients
        {
            get
            {
                return nutrients;
            }
        }

        public bool IsWatered
        {
            get
            {
                return lastWateredTotalHours > 0 && (api.World.Calendar.TotalHours - lastWateredTotalHours) < 24.5;
            }
        }

        public int OriginalFertility
        {
            get
            {
                return originalFertility;
            }
        }

        public BlockPos Pos
        {
            get
            {
                return base.pos;
            }
        }

        public BlockPos UpPos
        {
            get
            {
                return upPos;
            }
        }

        public ITreeAttribute CropAttributes
        {
            get
            {
                return cropAttrs;
            }
        }
    }
}
