using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TerraFirmaLike.Dialog
{
    class TFLCompass : HudElement
    {
        readonly string[] Direction = new string[8]
        {
            "South","South East","East","North East","North","North West", "West", "South West",
        };

        public override string ToggleKeyCombinationCode
        {
            get { return "tflcompass"; }
        }

        public TFLCompass(ICoreClientAPI capi) : base(capi)
        {

            capi.Event.RegisterGameTickListener(Every100ms, 100);
        }

        public override void OnOwnPlayerDataReceived()
        {
            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.RightTop, 0, 0, 160, 165);
            ElementBounds overlayBounds = textBounds.ForkBoundingParent(5, 5, 5, 5);

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightTop)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);

            SingleComposer = capi.Gui
                .CreateCompo("coordinateshud", dialogBounds)
                .AddGameOverlay(overlayBounds)
                .AddDynamicText("", CairoFont.WhiteSmallText(), EnumTextOrientation.Left, textBounds, "text")
                .Compose()
            ;

            if (capi.Settings.Bool["tflcompass"]) TryOpen();
        }

        public void Every100ms(float dt)
        {
            if (!capi.Settings.Bool["tflcompass"]) return;
            BlockPos pos = capi.World.Player.Entity.Pos.AsBlockPos;
            EntityPlayer player = capi.World.Player.Entity;
            float yaw = GameMath.Mod(player.Pos.Yaw, 2 * GameMath.PI);
            float deg = (GameMath.RAD2DEG * yaw);
            float heading = (float)Math.Round(Climate.RotateAngle(deg, 90), 1);
            string facing = Direction[(int)((heading / 45) + 0.5) % 8];
            heading = (float)Math.Round(Climate.RotateAngle(heading, 180.0), 1); //because vs positions are weird

            string location = "Lat: " + Climate.GetLat(pos) + ", Lng: " + Climate.GetLng(pos) + "\n MASL: " + Climate.GetMASL(pos) + "\n Facing: " + facing + "\n Heading: " + heading + "\u00B0";
            string climate = "\n Month: " + Atlas.monthString + "\n Season: " + Atlas.seasonString + "\n Day Of Month: " + Atlas.dayOfMonth + "/" + Atlas.DaysPerMonth + "\n Temperature: " + Climate.GetBlockTemp(pos) + "\u00B0C"
                + "\n Average: " + Climate.AverageTemp(pos) + "\u00B0C";

            SingleComposer.GetDynamicText("text").SetNewText(location + climate);

            List<ElementBounds> boundsList = capi.Gui.GetDialogBoundsInArea(EnumDialogArea.RightTop);
            SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding;

            foreach (ElementBounds ebounds in boundsList)
            {
                if (ebounds == SingleComposer.Bounds) continue;
                ElementBounds bounds = ebounds;

                SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding + bounds.absY + bounds.OuterHeight;
                break;
            }
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            capi.Settings.Bool["tflcompass"] = false;
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            capi.Settings.Bool["tflcompass"] = true;
        }


    }
}
