﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFirmaLike.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using VSRidged;

namespace TerraFirmaLike.Blocks
{
    class BlockSand : Block
    {
        
        BlockPos[] cardinal;
        BlockPos[] circle;
        private ushort[] beachCodesLittleRainfall;
        private ushort[] beachCodesHighRainfall;

        private NoiseGeoProvince noiseGeoProvince;
        public NormalizedSimplexNoise noisegenTypes;
        public NormalizedSimplexNoise noisegenScale;
        bool a = true;

        const int radius = 8;
        IBulkBlockAccessor bbA;
        public override void OnLoaded(ICoreAPI api)
        {
            if (api.World.Side.IsServer() && a)
            {
                noiseGeoProvince = new NoiseGeoProvince(api.World.Seed, api as ICoreServerAPI);

                int woctaves = 3;
                float types = 0.1f * TerraGenConfig.geoProvMapScale;
                float scales = 0.5f * TerraGenConfig.geoProvMapScale;
                float wpersistence = 0.9f;
                noisegenTypes = NormalizedSimplexNoise.FromDefaultOctaves(woctaves, 1 / types, wpersistence, api.World.Seed + 1231296);
                noisegenScale = NormalizedSimplexNoise.FromDefaultOctaves(woctaves, 1 / scales, wpersistence, api.World.Seed + 4287962);
                a = false;
            }

            beachCodesLittleRainfall = new ushort[]
            {
                api.World.BlockAccessor.GetBlock(new AssetLocation("gravel-" + LastCodePart())).BlockId,
                BlockId,
                0,
            };

            beachCodesHighRainfall = new ushort[]
            {
                BlockId,
                api.World.BlockAccessor.GetBlock(new AssetLocation("mud-" + LastCodePart())).BlockId,
                0,
            };

            cardinal = AreaMethods.CardinalOffsetList().ToArray();
            bbA = api.World.BulkBlockAccessor;

            base.OnLoaded(api);
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace)
        {
            Block block = blockAccessor.GetBlock(pos);
            BlockPos dPos = new BlockPos(pos.X, pos.Y - 1, pos.Z);

            ushort[] rockids = blockAccessor.GetMapChunkAtBlockPos(pos).TopRockIdMap;
            Block idBlock = blockAccessor.GetBlock(rockids[(int)(noisegenTypes.Noise(pos.X, pos.Y, pos.Z) * (rockids.Length - 1))]);
            Block dBlock = blockAccessor.GetBlock(dPos);

            if (this == dBlock) return false;


            if (idBlock.LastCodePart() == LastCodePart())
            {
                foreach (var v in cardinal)
                {
                    BlockPos rPos = new BlockPos(dPos.X + v.X, dPos.Y, dPos.Z + v.Z);
                    Block rBlock = blockAccessor.GetBlock(rPos);

                    if (rBlock.IsWater())
                    {
                        MakeBeach(dPos, blockAccessor, rPos);
                        return true;
                    }
                }
            }

            return false;
        }

        public void MakeBeach(BlockPos pos, IBlockAccessor bA, BlockPos rPos)
        {
            circle = AreaMethods.CircularOffsetList((int)(radius * noisegenScale.Noise(pos.X, pos.Y, pos.Z))).ToArray();
            foreach (BlockPos val in circle)
            {
                BlockPos tPos = new BlockPos(pos.X + val.X, pos.Y + val.Y, pos.Z + val.Z);
                Block block = bA.GetBlock(tPos);
                float distance = tPos.DistanceTo(rPos);

                double noise = noisegenTypes.Noise(tPos.X, tPos.Y, tPos.Z);
                if (NotReplacable(block)) continue;
                ushort id = BlockId;

                if (!GetID(bA, noise, pos, ref id)) continue;

                BlockPos uPos = tPos.UpCopy();
                Block uBlock = bA.GetBlock(uPos);

                for (int i = 0; i < 4; i++)
                {
                    BlockPos ipos = uPos.AddCopy(0, i, 0);
                    Block iblock = bA.GetBlock(ipos);

                    if (iblock is BlockPlant && !iblock.IsWater()) { bA.SetBlock(0, ipos); break; };
                    if (NotReplacable(iblock)) break;

                    if (iblock.CollisionBoxes != null) bA.SetBlock(id, ipos);
                }

                bA.SetBlock(id, tPos);
                bA.SetBlock(id, tPos.Add(new BlockPos(0, -1, 0)));
            }
        }

        public bool GetID(IBlockAccessor bA, double noise, BlockPos pos, ref ushort id)
        {
            if (bA.GetClimateAt(pos).Rainfall < 0.65)
            {
                id = beachCodesLittleRainfall[(int)Math.Round((noise * (beachCodesLittleRainfall.Length - 1)))];
                if (id == 0) return false;
                return true;
            }
            else
            {
                id = beachCodesHighRainfall[(int)Math.Round((noise * (beachCodesHighRainfall.Length - 1)))];
                if (id == 0) return false;
                return true;
            }
        }

        public bool NotReplacable(Block block)
        {
            bool rep = (block.Id == 0 || block.IsLiquid() || block == this || block.IsPlant() || block.FirstCodePart() == "log" || block.FirstCodePart() == "rawclay");
            return rep;
        }
    }

    public static class BlockBooleans
    {
        public static bool IsPlant(this Block block)
        {
            return (block is BlockPlant || block is BlockCrop || block is BlockSeaweed || block is BlockBamboo || block is BlockReeds || block is BlockWaterLily || block is BlockCactus || block is BlockBerryBush);
        }
    }
}
