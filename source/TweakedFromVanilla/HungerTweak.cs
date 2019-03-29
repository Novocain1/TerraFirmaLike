using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TerraFirmaLike.TweakedFromVanilla
{
    public class HungerTweak : EntityBehavior
    {
        ITreeAttribute hungerTree;
        EntityAgent entityAgent;

        long listenerId;
        long lastMoveMs;
        double nextSatUpdate;
        double nextThirstUpdate;
        double lastSatUpdate;
        double lastThirstUpdate;

        public float SpentCalories
        {
            get { return hungerTree.GetFloat("spentcalories"); }
            set { hungerTree.SetFloat("spentcalories", value); entity.WatchedAttributes.MarkPathDirty("hunger"); }
        }

        public float Dehydration
        {
            get { return hungerTree.GetFloat("dehydration"); }
            set { hungerTree.SetFloat("dehydration", value); entity.WatchedAttributes.MarkPathDirty("hunger"); }
        }

        public float Thirst
        {
            get { return hungerTree.GetFloat("currentthirst"); }
            set { hungerTree.SetFloat("currentthirst", value); entity.WatchedAttributes.MarkPathDirty("currentthirst"); }
        }

        public float MaxThirst
        {
            get { return hungerTree.GetFloat("maxthirst"); }
            set { hungerTree.SetFloat("maxthirst", value); entity.WatchedAttributes.MarkPathDirty("hunger"); }
        }

        public float Saturation
        {
            get { return hungerTree.GetFloat("currentsaturation"); }
            set { hungerTree.SetFloat("currentsaturation", value); entity.WatchedAttributes.MarkPathDirty("hunger"); }
        }

        public float MaxSaturation
        {
            get { return hungerTree.GetFloat("maxsaturation"); }
            set { hungerTree.SetFloat("maxsaturation", value); entity.WatchedAttributes.MarkPathDirty("hunger"); }
        }

        public float FruitLevel
        {
            get { return hungerTree.GetFloat("fruitLevel"); }
            set { hungerTree.SetFloat("fruitLevel", value); entity.WatchedAttributes.MarkPathDirty("hunger"); }
        }

        public float VegetableLevel
        {
            get { return hungerTree.GetFloat("vegetableLevel"); }
            set { hungerTree.SetFloat("vegetableLevel", value); entity.WatchedAttributes.MarkPathDirty("hunger"); }
        }

        public float ProteinLevel
        {
            get { return hungerTree.GetFloat("proteinLevel"); }
            set { hungerTree.SetFloat("proteinLevel", value); entity.WatchedAttributes.MarkPathDirty("hunger"); }
        }

        public float GrainLevel
        {
            get { return hungerTree.GetFloat("grainLevel"); }
            set { hungerTree.SetFloat("grainLevel", value); entity.WatchedAttributes.MarkPathDirty("hunger"); }
        }

        public float DairyLevel
        {
            get { return hungerTree.GetFloat("dairyLevel"); }
            set { hungerTree.SetFloat("dairyLevel", value); entity.WatchedAttributes.MarkPathDirty("hunger"); }
        }



        public HungerTweak(Entity entity) : base(entity)
        {
            entityAgent = entity as EntityAgent;
        }

        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            hungerTree = entity.WatchedAttributes.GetTreeAttribute("hunger");

            if (hungerTree == null)
            {
                entity.WatchedAttributes.SetAttribute("hunger", hungerTree = new TreeAttribute());

                Saturation = typeAttributes["currentsaturation"].AsFloat(1200);
                MaxSaturation = typeAttributes["maxsaturation"].AsFloat(1200);
                Thirst = typeAttributes["currentthirst"].AsFloat(1200);
                MaxThirst = typeAttributes["maxthirst"].AsFloat(1200);

                FruitLevel = 0;
                VegetableLevel = 0;
                GrainLevel = 0;
                ProteinLevel = 0;
                DairyLevel = 0;
                SpentCalories = 0;

            }

            lastThirstUpdate = entity.World.Calendar.TotalHours;
            lastSatUpdate = entity.World.Calendar.TotalHours;
            UpdateSaturation();
            UpdateThirst();
            listenerId = entity.World.RegisterGameTickListener(SlowTick, 6000);

            UpdateNutrientHealthBoost();
        }

        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            base.OnEntityDespawn(despawn);

            entity.World.UnregisterGameTickListener(listenerId);
        }

        public override void OnEntityReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10)
        {
            float maxsat = MaxSaturation;
            //bool full = Saturation >= maxsat;

            Saturation = Math.Min(maxsat, Saturation + saturation);

            switch (foodCat)
            {
                case EnumFoodCategory.Fruit:
                    FruitLevel = Math.Min(maxsat, FruitLevel + saturation / 2.5f);
                    break;

                case EnumFoodCategory.Vegetable:
                    VegetableLevel = Math.Min(maxsat, VegetableLevel + saturation / 2.5f);
                    break;

                case EnumFoodCategory.Protein:
                    ProteinLevel = Math.Min(maxsat, ProteinLevel + saturation / 2.5f);
                    break;

                case EnumFoodCategory.Grain:
                    GrainLevel = Math.Min(maxsat, GrainLevel + saturation / 2.5f);
                    break;

                case EnumFoodCategory.Dairy:
                    DairyLevel = Math.Min(maxsat, DairyLevel + saturation / 2.5f);
                    break;
            }

            UpdateNutrientHealthBoost();
        }

        public override void OnGameTick(float dT)
        {
            if (entity is EntityPlayer)
            {
                EntityPlayer plr = (EntityPlayer)entity;
                EnumGameMode mode = entity.World.PlayerByUid(plr.PlayerUID).WorldData.CurrentGameMode;
                if (mode == EnumGameMode.Creative || mode == EnumGameMode.Spectator) return;

                if (plr.Controls.TriesToMove || plr.Controls.Jump)
                {
                    lastMoveMs = entity.World.ElapsedMilliseconds;
                }
            }

            bool isStandingStill = (entity.World.ElapsedMilliseconds - lastMoveMs) > 3000;
            SpentCalories += isStandingStill ? 0 : 0.1f / 64f;
            Dehydration += isStandingStill ? 0 : 0.2f / 64f;

            if (entity.World.Calendar.TotalHours > nextSatUpdate)
            {
                UpdateSaturation();
            }

            if (entity.World.Calendar.TotalHours > nextThirstUpdate)
            {
                UpdateThirst();
            }
        }

        public void UpdateSaturation()
        {
            nextSatUpdate = ResetFoodCalendar();
            double time = entity.World.Calendar.TotalHours;

            UpdateNutritionLevels(SpentCalories);
            float prevSaturation = Saturation;

            float satLoss = SpentCalories;
            if (prevSaturation > 0)
            {
                Saturation = Math.Max(0, (prevSaturation - satLoss) - (float)((time - lastSatUpdate) / 48));
                SpentCalories = 0.0f;
            }

            UpdateNutrientHealthBoost();
        }

        public void UpdateThirst()
        {
            nextThirstUpdate = ResetThirstCalendar();
            double time = entity.World.Calendar.TotalHours;

            float prevThirst = Thirst;
            float dehydrationLoss = Dehydration;
            if (prevThirst > 0)
            {
                Thirst = Math.Max(0, (prevThirst - dehydrationLoss) - (float)((time - lastThirstUpdate) / 24));
                Dehydration = 0;
            }
        }

        public double ResetFoodCalendar()
        {
            return entity.World.Calendar.TotalHours + 4;
        }

        public double ResetThirstCalendar()
        {
            return entity.World.Calendar.TotalHours + 1;
        }

        public void UpdateNutritionLevels(float spentCalories)
        {
            float dC = spentCalories;
            int nCount = NutritionCount();

            if (nCount != 0)
            {
                dC = spentCalories / nCount;
            }

            FruitLevel = FruitLevel != 0 ? dC : 0;
            VegetableLevel = VegetableLevel != 0 ? dC : 0;
            ProteinLevel = ProteinLevel != 0 ? dC : 0;
            GrainLevel = GrainLevel != 0 ? dC : 0;
            DairyLevel = DairyLevel != 0 ? dC : 0;
        }

        public int NutritionCount()
        {
            int i = 0;
            if (FruitLevel > 0)
            {
                i++;
            }
            if (VegetableLevel > 0)
            {
                i++;
            }
            if (ProteinLevel > 0)
            {
                i++;
            }
            if (GrainLevel > 0)
            {
                i++;
            }
            if (DairyLevel > 0)
            {
                i++;
            }
            return i;
        }

        public void UpdateNutrientHealthBoost()
        {
            float fruitRel = FruitLevel / MaxSaturation;
            float grainRel = GrainLevel / MaxSaturation;
            float vegetableRel = VegetableLevel / MaxSaturation;
            float proteinRel = ProteinLevel / MaxSaturation;

            EntityBehaviorHealth bh = entity.GetBehavior<EntityBehaviorHealth>();
            float healthGain = 2.5f * (fruitRel + grainRel + vegetableRel + proteinRel);

            bh.MaxHealthModifiers["nutrientHealthMod"] = healthGain;
            bh.MarkDirty();
        }



        private void SlowTick(float dt)
        {
            EntityBehaviorHealth entityhealth = entity.GetBehavior<EntityBehaviorHealth>();

            if (entity is EntityPlayer)
            {
                EntityPlayer plr = (EntityPlayer)entity;
                if (entity.World.PlayerByUid(plr.PlayerUID).WorldData.CurrentGameMode == EnumGameMode.Creative) return;
            }

            if (Saturation <= 0 || Thirst <= 0)
            {
                entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = EnumDamageType.Hunger }, 0.25f);
            }
            else if (Saturation >= MaxSaturation && entityhealth.Health <= entityhealth.MaxHealth)
            {
                entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = EnumDamageType.Heal }, 0.25f);
            }
        }

        public override string PropertyName()
        {
            return "hunger";
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, float damage)
        {
            if (damageSource.Type == EnumDamageType.Heal && damageSource.Source == EnumDamageSource.Revive)
            {
                FruitLevel = FruitLevel <= 5 ? 0 : FruitLevel / 2;
                VegetableLevel = VegetableLevel <= 5 ? 0 : VegetableLevel / 2;
                ProteinLevel = ProteinLevel <= 5 ? 0 : ProteinLevel / 2;
                GrainLevel = GrainLevel <= 5 ? 0 : GrainLevel / 2;
                DairyLevel = DairyLevel <= 5 ? 0 : DairyLevel / 2;
                Saturation = Saturation < MaxSaturation / 2 ? Saturation : MaxSaturation / 2;
                Thirst = Thirst < MaxThirst / 2 ? Thirst : MaxThirst / 2;

                SpentCalories += 5;
                Dehydration += 5;
                UpdateSaturation();
                UpdateThirst();
            }
        }
    }

}
