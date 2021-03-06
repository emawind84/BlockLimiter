﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlockLimiter;
using BlockLimiter.ProcessHandlers;
using Sandbox.Game.Entities;
using BlockLimiter.Utility;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using BlockLimiter.Settings;
using Torch;
using VRage.Network;
using NLog;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Scripting;
using VRageRender;


namespace BlockLimiter.Patch
{
    [PatchShim]
    public static class BuildBlockPatch
    {
     
        public static void Patch(PatchContext ctx)
        {
            var t = typeof(MyCubeGrid);
            var aMethod = t.GetMethod("BuildBlocksRequest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            ctx.GetPattern(aMethod).Prefixes.Add(typeof(BuildBlockPatch).GetMethod(nameof(BuildBlocksRequest),BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
            var bMethod = t.GetMethod("BuildBlocksAreaRequest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            ctx.GetPattern(bMethod).Prefixes.Add(typeof(BuildBlockPatch).GetMethod(nameof(BuildBlocksArea),BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
        }
     

        /// <summary>
        /// Checks blocks being built in creative with multiblock placement.
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        private static bool BuildBlocksArea(MyCubeGrid __instance, MyCubeGrid.MyBlockBuildArea area)
        {
            if (!BlockLimiterConfig.Instance.EnableLimits) return true;

            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(area.DefinitionId);
            var grid = __instance;

            int blocksToBuild = (int) area.BuildAreaSize.X * (int) area.BuildAreaSize.Y * (int) area.BuildAreaSize.Z;


            if (grid == null)
            {
                BlockLimiter.Instance.Log.Debug("Null grid in BuildBlockHandler");
                return true;
            }

            var remoteUserId = MyEventContext.Current.Sender.Value;
            var playerId = Utilities.GetPlayerIdFromSteamId(remoteUserId);

            
            if (!Block.IsWithinLimits(def, playerId, grid.EntityId, blocksToBuild))
            {
                    BlockLimiter.Instance.Log.Info($"Blocked {Utilities.GetPlayerNameFromSteamId(remoteUserId)} from placing {def.ToString().Substring(16)} due to limits");

                    var mi = new List<string>(){"test"};
                //ModCommunication.SendMessageTo(new NotificationMessage($"You've reach your limit for {b}",5000,MyFontEnum.Red),remoteUserId );
                var msg = Utilities.GetMessage(BlockLimiterConfig.Instance.DenyMessage,new List<string>(){def.ToString().Substring(16)});

                MyVisualScriptLogicProvider.SendChatMessage($"{msg}",BlockLimiterConfig.Instance.ServerName,playerId,MyFontEnum.Red);
                Utilities.SendFailSound(remoteUserId);
                Utilities.ValidationFailed();

                return false;
            }
            
            
            return true;


        }


        /// <summary>
        /// Checks blocks being placed on grids.
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="locations"></param>
        /// <returns></returns>
        private static bool BuildBlocksRequest(MyCubeGrid __instance, HashSet<MyCubeGrid.MyBlockLocation> locations)
        {
            
            if (!BlockLimiterConfig.Instance.EnableLimits) return true;

            var grid = __instance;
            if (grid == null)
            {
                BlockLimiter.Instance.Log.Debug("Null grid in BuildBlockHandler");
                return true;
            }


            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(locations.FirstOrDefault().BlockDefinition);

            if (def == null) return true;
            
           
            var remoteUserId = MyEventContext.Current.Sender.Value;
            var playerId = Utilities.GetPlayerIdFromSteamId(remoteUserId);


            if (!Block.IsWithinLimits(def, playerId, grid.EntityId))
            {
                    BlockLimiter.Instance.Log.Info($"Blocked {Utilities.GetPlayerNameFromSteamId(remoteUserId)} from placing {def.ToString().Substring(16)} due to limits");
                    var msg = Utilities.GetMessage(BlockLimiterConfig.Instance.DenyMessage,new List<string>(){def.ToString().Substring(16)});
                MyVisualScriptLogicProvider.SendChatMessage($"{msg}",BlockLimiterConfig.Instance.ServerName,playerId,MyFontEnum.Red);
                Utilities.SendFailSound(remoteUserId);
                Utilities.ValidationFailed();
                return false;
            }
            
            return true;
            }
            

    }
}
