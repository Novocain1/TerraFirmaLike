using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TerraFirmaLike.BlockEntities
{
    class BlockEntityDrip : BlockEntity
    {
        public static SimpleParticleProperties drip;
        public static SimpleParticleProperties smoke;
        Vec3d midBPos;
        Block block;
        bool isLava = false;

        static BlockEntityDrip()
        {
            smoke = new SimpleParticleProperties(
                1, 1,
                ColorUtil.ToRgba(50, 2, 2, 2),
                new Vec3d(), new Vec3d(), //minPos maxPos
                new Vec3f(), new Vec3f(), //minV maxV
                1, 0, 0.4f, 0.8f, //life, gravity, minsize, maxsize
                EnumParticleModel.Quad
                );

            drip = new SimpleParticleProperties(
                1, 1,
                ColorUtil.ToRgba(100, 0, 0, 100),
                new Vec3d(), new Vec3d(), //minPos maxPos
                new Vec3f(), new Vec3f(), //minV maxV
                1, 1, 0.4f, 0.8f, //life, gravity, minsize, maxsize
                EnumParticleModel.Cube
                );
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            block = api.World.BlockAccessor.GetBlock(pos);
            isLava = block.FirstCodePart() == "lava";

            midBPos = (isLava ? pos.ToVec3d() + new Vec3d(0.5, 0.2, 0.5) : pos.ToVec3d() + new Vec3d(0.5, 0.5, 0.5));
            if (IsDrippable())
            {
                RegisterGameTickListener(OnTick, 500);
            }
        }

        public bool IsDrippable()
        {
            var bA = api.World.BlockAccessor;
            return bA.GetBlock(pos.X, pos.Y - 2, pos.Z).IsReplacableBy(block) && !bA.GetBlock(pos.X, pos.Y - 1, pos.Z).IsReplacableBy(block);
        }

        public void OnTick(float dt)
        {
            var bA = api.World.BlockAccessor;

            if (api.Side.IsClient() && IsDrippable())
            {
                if (isLava) SpawnSmoke();
                else SpawnDrips();
            }
        }
        public void SpawnSmoke()
        {
            double rngX = (double)api.World.Rand.Next(-50, 50) / 100;
            double rngZ = (double)api.World.Rand.Next(-50, 50) / 100;
            smoke.minPos.Set(midBPos.X + rngX, midBPos.Y - 1.54, midBPos.Z + rngZ);
            smoke.minVelocity.Set((float)rngX, (float)((rngX + rngZ) / 2) + 1, (float)rngZ);
            smoke.color = ColorUtil.ToRgba(api.World.Rand.Next(60, 100), 2, 2, 2);
            api.World.SpawnParticles(smoke);
        }

        public void SpawnDrips()
        {
            double rngX = (double)api.World.Rand.Next(-50, 50) / 100;
            double rngZ = (double)api.World.Rand.Next(-50, 50) / 100;
            drip.minPos.Set(midBPos.X + rngX, midBPos.Y - 1.54, midBPos.Z + rngZ);
            api.World.SpawnParticles(drip);
        }
    }
}
