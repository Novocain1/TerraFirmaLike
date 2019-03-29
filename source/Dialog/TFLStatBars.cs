using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TerraFirmaLike.Dialog
{
    class TFLStatBars : HudElement
    {
        float lastThirst;
        float lastMaxThirst;
        bool fix = false;
        GuiDialog statbardialogue = null;

        GuiElementStatbar thirstbar;

        public TFLStatBars(ICoreClientAPI capi) : base(capi)
        {
            Register(capi);
        }

        public void Register(ICoreClientAPI capi)
        {
            capi.Event.RegisterGameTickListener(OnGameTick, 20);
        }

        public override void OnOwnPlayerDataReceived()
        {
            ComposeBar();
            UpdateThirst();
        }

        public override double InputOrder { get { return 2; } }
        public override string ToggleKeyCombinationCode { get { return null; } }

        public void FixSaturationGUI(GuiDialog dialog, float? maxsat, float interval)
        {
            dialog.Composers["statbar"].GetStatbar("saturationstatbar").SetLineInterval((float)maxsat / interval);
            dialog.Composers["statbar"].ReCompose();
            
        }

        public GuiDialog GetSaturationGUI(ICoreClientAPI api)
        {
            foreach (GuiDialog dialog in api.Gui.OpenedGuis)
            {
                if (dialog is HudElement && dialog.Composers["statbar"] != null)
                {
                    return dialog;
                }
            }
            
            return null;
        }

        public GuiDialog GetCompassGUI(ICoreClientAPI api)
        {
            foreach (GuiDialog dialog in api.OpenedGuis)
            {
                if (dialog is HudElement && dialog.ToggleKeyCombinationCode == "coordinateshud")
                {
                    return dialog;
                }
            }
            return null;
        }

        void UpdateThirst()
        {
            if (capi.World.Player == null) return;

            ITreeAttribute hungerTree = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
            if (hungerTree == null) return;

            float? thirst = hungerTree.TryGetFloat("currentthirst");
            float? maxthirst = hungerTree.TryGetFloat("maxthirst");
            float? maxsat = hungerTree.TryGetFloat("maxsaturation");
            float interval = 16.0f;

            if (!fix)
            {
                if (GetSaturationGUI(capi) != null)
                {
                    statbardialogue = GetSaturationGUI(capi);
                    fix = true;
                }
            }

            if (fix)
            {
                FixSaturationGUI(statbardialogue, maxsat, interval);
            }

            if ((thirst == null || maxthirst == null) || (lastThirst == thirst && lastMaxThirst == maxthirst) || (thirstbar == null)) return;
            thirstbar.SetLineInterval((float)maxthirst / interval);
            thirstbar.SetValues((float)thirst, 0, (float)maxthirst);

            lastThirst = (float)thirst;
            lastMaxThirst = (float)maxthirst;
        }

        public void ComposeBar()
        {
            double elemToDlgPad = GuiStyle.ElementToDialogPadding;
            float width = 850;

            ElementBounds bounds = new ElementBounds()
            {
                Alignment = EnumDialogArea.CenterBottom,
                BothSizing = ElementSizing.Fixed,
                fixedWidth = width,
                fixedHeight = 100
            }.WithFixedAlignmentOffset(0, -1);

            ElementBounds thirstBarBounds = Statbar(EnumDialogArea.RightTop, width * 0.41).WithFixedAlignmentOffset(-2, 7);
            ElementBounds horBarBounds = ElementStdBounds.SlotGrid(EnumDialogArea.CenterFixed, 0, 38, 10, 1);
            Composers["thirstbar"] =
                capi.Gui
                .CreateCompo("inventory-thirstbar", bounds.FlatCopy().FixedGrow(0, 20))
                .BeginChildElements(bounds)
                .AddInvStatbar(thirstBarBounds, GuiStyle.DialogBlueBgColor, "thirststatbar")
                .EndChildElements()
                .Compose();
            thirstbar = Composers["thirstbar"].GetStatbar("thirststatbar");

            TryOpen();
        }

        private void OnGameTick(float dt)
        {
            UpdateThirst();
        }

        public override bool TryClose()
        {
            return false;
        }

        public override bool ShouldReceiveKeyboardEvents()
        {
            return false;
        }

        public override void OnRenderGUI(float deltaTime)
        {
            if (capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
            {
                base.OnRenderGUI(deltaTime);
            }
        }

        public override void Focus() { }
        public override void UnFocus() { }

        public static ElementBounds Statbar(EnumDialogArea alignment, double width)
        {
            return new ElementBounds()
            {
                Alignment = alignment,
                fixedWidth = width,
                fixedHeight = GuiElementStatbar.DefaultHeight,
                BothSizing = ElementSizing.Fixed
            };
        }
    }
}
