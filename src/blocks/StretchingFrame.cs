﻿using AncientTools.BlockEntity;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AncientTools.Blocks
{
    class BlockStretchingFrame: Block
    {
        WorldInteraction[] emptyInteractions = null;
        WorldInteraction[] knifeInteractions = null;
        WorldInteraction[] skinningFinishedInteractions = null;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            List<ItemStack> soakedHides = new List<ItemStack>();
            List<ItemStack> knives = new List<ItemStack>();

            foreach (Item item in api.World.Items)
            {
                if (item.Code == null)
                    continue;

                if (item.FirstCodePart() == "hide" && item.FirstCodePart(1) == "soaked")
                {
                    soakedHides.Add(new ItemStack(item));
                }
                else if(item.FirstCodePart() == "knife")
                {
                    knives.Add(new ItemStack(item));
                }
            }

            emptyInteractions = ObjectCacheUtil.GetOrCreate(api, "emptyStretchingFrameInteractions", () =>
            {
                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "ancienttools:blockhelp-place-hide",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = soakedHides.ToArray()
                    }
                };
            });

            knifeInteractions = ObjectCacheUtil.GetOrCreate(api, "knifeStretchingFrameInteractions", () =>
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "ancienttools:blockhelp-skin-hide",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak",
                        Itemstacks = knives.ToArray()
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "ancienttools:blockhelp-take-hide",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                    }
                };
            });

            skinningFinishedInteractions = ObjectCacheUtil.GetOrCreate(api, "finishedSkinningInteractions", () =>
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "ancienttools:blockhelp-take-hide",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                    }
                };
            });
        }
        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            return String.Empty;
        }
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (world.BlockAccessor.GetBlockEntity(selection.Position) is BEStretchingFrame stretchingFrameEntity)
            {
                if (stretchingFrameEntity.HideSlot.Empty)
                    return emptyInteractions;
                else if (stretchingFrameEntity.HideSlot.Itemstack.Item.FirstCodePart(1) == "soaked")
                    return knifeInteractions;
                else
                    return skinningFinishedInteractions;
            }

            return null;
        }
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            string[] blockCode = this.Code.Path.Split('-');

            blockCode[blockCode.Length - 1] = GetBlockRotation(blockSel.Face, byPlayer.Entity.Pos.XYZInt, blockSel.Position.ToVec3i());

            int id = world.GetBlock(new AssetLocation("ancienttools", string.Join("-", blockCode))).Id;

            try
            {
                world.BlockAccessor.SetBlock(id, blockSel.Position);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel == null)
                return false;

            if(world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEStretchingFrame stretchingFrameEntity)
            {
                if(byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
                {
                    stretchingFrameEntity.TryGiveHide(byPlayer);
                    return true;
                }
                else if(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.FirstCodePart() == "hide")
                {
                    stretchingFrameEntity.TryPlaceHide(byPlayer.InventoryManager.ActiveHotbarSlot);
                    return true;
                }
                else if(byPlayer.InventoryManager.ActiveTool == EnumTool.Knife && byPlayer.Entity.Controls.Sneak)
                {
                    if(!stretchingFrameEntity.HideSlot.Empty)
                        if(stretchingFrameEntity.HideSlot.Itemstack.Item.FirstCodePart(1) == "soaked")
                        {
                            return stretchingFrameEntity.BeginPrepareHide(byPlayer);
                        }
                }

            }
            
            return false;
        }
        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel == null)
                return false;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEStretchingFrame stretchingFrameEntity)
            {
                if (byPlayer.InventoryManager.ActiveTool == EnumTool.Knife && byPlayer.Entity.Controls.Sneak)
                {
                    if (stretchingFrameEntity.HideSlot.Itemstack.Item.FirstCodePart(1) == "soaked")
                    {
                        return stretchingFrameEntity.PrepareHide(byPlayer);
                    }
                }
            }

            return false;
        }
        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel == null)
                return;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEStretchingFrame stretchingFrameEntity)
            {
                stretchingFrameEntity.Reset();
            }
        }
        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            if (blockSel == null)
                return true;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEStretchingFrame stretchingFrameEntity)
            {
                stretchingFrameEntity.Reset();
            }

            return true;
        }
        private string GetBlockRotation(BlockFacing selectedFace, Vec3i playerPos, Vec3i blockPos)
        {
            //-- Align based on the horizontal face clicked --//
            if (selectedFace == BlockFacing.EAST)
                return "east";
            else if (selectedFace == BlockFacing.WEST)
                return "west";
            else if (selectedFace == BlockFacing.NORTH)
                return "north";
            else if (selectedFace == BlockFacing.SOUTH)
                return "south";
            else
            {
                //-- Otherwise, if the rack is placed on a vertical face, align it based on the angle of the players position against the block position --//
                playerPos.Y = 0;
                blockPos.Y = 0;

                Vec3f rotateBasedOnPlayer = new Vec3f(playerPos.X - blockPos.X, 0, playerPos.Z - blockPos.Z);

                rotateBasedOnPlayer.Normalize();

                if (Math.Abs(rotateBasedOnPlayer.X) >= Math.Abs(rotateBasedOnPlayer.Z))
                    if (rotateBasedOnPlayer.X > 0)
                        return "east";
                    else
                        return "west";
                else
                    if (rotateBasedOnPlayer.Z > 0)
                        return "south";
                    else
                        return "north";
            }
        }
    }
}
