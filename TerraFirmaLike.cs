using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.BlockBehaviors;
using TerraFirmaLike.BlockEntities;
using TerraFirmaLike.Blocks;
using TerraFirmaLike.Items;
using TerraFirmaLike.TweakedFromVanilla;
using TerraFirmaLike.Dialog;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory;
using Vintagestory.ServerMods.NoObf;
using VintagestoryAPI;
using Vintagestory.Common;
using Vintagestory.API.Server;
using TerraFirmaLike.Utility;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TerraFirmaLike
{
    public class TerraFirmaLike : ModSystem
    {
        ICoreAPI api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        TFLCompass compass;
        long cid;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            base.Start(api);

            RegisterEntityBehaviors();
            RegisterBlocks();
            RegisterBlockEntities();
            RegisterBlockBehaviors();
            RegisterItems();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;
            
            base.StartServerSide(api);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            
            this.capi = api;

            cid = capi.Event.RegisterGameTickListener(TryRegister, 500);

            TFLStatBars thirstbar = new TFLStatBars(api);
            thirstbar.OnOwnPlayerDataReceived();
        }

        public void TryRegister(float dt)
        {
            if (capi.World.Player.Entity.State == EnumEntityState.Active)
            {
                capi.Event.UnregisterGameTickListener(cid);
                compass = new TFLCompass(capi);
                compass.OnOwnPlayerDataReceived();
                capi.Input.RegisterHotKey("coordinateshud", "coordinateshud", GlKeys.Semicolon, HotkeyType.GUIOrOtherControls);
                capi.Input.RegisterHotKey("tflcompass", "TFL Compass", GlKeys.V, HotkeyType.GUIOrOtherControls);
                capi.Input.SetHotKeyHandler("tflcompass", OnHotkeyCompassHud);
            }
        }

        public bool OnHotkeyCompassHud(KeyCombination comb)
        {
            if (compass.IsOpened())
            {
                compass.TryClose();
            }
            else
            {
                compass.TryOpen();
            }
            return true;
        }

        public void RegisterEntityBehaviors()
        {
            api.RegisterEntityBehaviorClass("hungertweak", typeof(HungerTweak));
        }

        public void RegisterBlocks()
        {
            api.RegisterBlockClass("FixedStairs", typeof(FixedStairs));
            api.RegisterBlockClass("FixedBlockWateringCan", typeof(FixedBlockWateringCan));
            api.RegisterBlockClass("BlockPlatycodon", typeof(BlockPlatycodon));
            api.RegisterBlockClass("BlockBerryBush", typeof(TweakedBerryBush));
            api.RegisterBlockClass("BlockCrop", typeof(BlockCropTweaked));
            api.RegisterBlockClass("BlockSand", typeof(BlockSand));
        }

        public void RegisterBlockEntities()
        {
            api.RegisterBlockEntityClass("BlockEntityMudbrick", typeof(BlockEntityMudbrick));
            api.RegisterBlockEntityClass("FixedBlockEntityFarmland", typeof(FixedBlockEntityFarmland));
            api.RegisterBlockEntityClass("Dripping", typeof(BlockEntityDrip));
            api.RegisterBlockEntityClass("BEBerryBush", typeof(TweakedBEBerryBush));
        }

        public void RegisterBlockBehaviors()
        {
            api.RegisterBlockBehaviorClass("BehaviorCuttingBoard", typeof(BehaviorCuttingBoard));
            api.RegisterBlockBehaviorClass("BehaviorStackCombiner", typeof(BehaviorStackCombiner));
            api.RegisterBlockBehaviorClass("Harvestable", typeof(HarvestableTweak));
            api.RegisterBlockBehaviorClass("UnstableFalling", typeof(NonEntityBlockFalling));
            api.RegisterBlockBehaviorClass("BehaviorStoneCollapse", typeof(BehaviorStoneCollapse));
            api.RegisterBlockBehaviorClass("BehaviorSupportBeam", typeof(BehaviorSupportBeam));
        }

        public void RegisterItems()
        {
            api.RegisterItemClass("ItemHoeFixed", typeof(ItemHoeFixed));
            api.RegisterItemClass("ItemPlantableSeed", typeof(ItemPlantableSeedFix));
            api.RegisterItemClass("ItemKnife", typeof(KnifeTweak));
            api.RegisterItemClass("ItemAxe", typeof(AxeTweak));
            api.RegisterItemClass("TFLfood", typeof(TFLFood));
        }

    }
}
