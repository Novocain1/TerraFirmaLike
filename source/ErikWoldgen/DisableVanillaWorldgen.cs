using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VSRidged
{
    public class DisableVanillaWorldGen : ModSystem
    {
        private ICoreServerAPI api;

        public override double ExecuteOrder()
        {
            return 0;
        }

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            api.Event.ServerRunPhase(EnumServerRunPhase.LoadGamePre, WipeVanillaWorldgenHandlers);
        }

        void WipeVanillaWorldgenHandlers()
        {
            IWorldGenHandler handlergroup = this.api.Event.GetRegisteredWorldGenHandlers(EnumPlayStyle.SurviveAndBuild);
            foreach (var handlers in handlergroup.OnChunkColumnGen)
            {
                for (int i = 0; handlers != null && i < handlers.Count; i++)
                {
                    if (handlers[i].Target.GetType().Namespace == "Vintagestory.ServerMods" && (handlers[i].Target.GetType().Name == "GenTerra" || handlers[i].Target.GetType().Name == "GenStructures"))
                    {
                        handlers.RemoveAt(i);
                        i--;
                    }
                }
            }

            {
                var handlers = handlergroup.OnMapRegionGen;
                for (int i = 0; handlers != null && i < handlers.Count; i++)
                {
                    if (handlers[i].Target.GetType().Namespace == "Vintagestory.ServerMods" && handlers[i].Target.GetType().Name == "GenMaps")
                    {
                        handlers.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }
}
