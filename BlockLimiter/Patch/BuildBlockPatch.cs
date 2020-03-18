﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static event Action<MySlimBlock> OnBlockAdded;

        
        public static void Patch(PatchContext ctx)
        {
            var t = typeof(MyCubeGrid);
            var bBr = t.GetMethod("BuildBlocksRequest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            ctx.GetPattern(bBr).Prefixes.Add(typeof(BuildBlockPatch).GetMethod(nameof(BuildBlocksRequest),BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
            var bbar = t.GetMethod("BuildBlocksAreaRequest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            ctx.GetPattern(bbar).Prefixes.Add(typeof(BuildBlockPatch).GetMethod(nameof(BuildBlocksArea),BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
        }

        private static bool BuildBlocksArea(MyCubeGrid __instance, MyCubeGrid.MyBlockBuildArea area)
        {
            if (!BlockLimiterConfig.Instance.EnableLimits) return true;

            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(area.DefinitionId);
            var grid = __instance;
            if (grid == null)
            {
                Log.Debug("Null grid in BuildBlockHandler");
                return true;
            }

            var remoteUserId = MyEventContext.Current.Sender.Value;
            var playerId = Utilities.GetPlayerIdFromSteamId(remoteUserId);

            if (Block.AllowBlock(def, playerId, grid))
            {
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    MySandboxGame.Static.Invoke(() =>
                    {
                        Grid.UpdateLimit(grid);
                        if (MySession.Static.Players.TryGetPlayerBySteamId(remoteUserId) != null) 
                            Block.UpdatePlayerLimits(MySession.Static.Players.TryGetPlayerBySteamId(remoteUserId));
                    }, "BlockLimiter");
                });

                return true;
            }
            
            
            if (BlockLimiterConfig.Instance.EnableLog)
                Log.Info($"Blocked {Utilities.GetPlayerNameFromSteamId(remoteUserId)} from placing {area.DefinitionId.SubtypeId} due to limits");
            //ModCommunication.SendMessageTo(new NotificationMessage($"You've reach your limit for {b}",5000,MyFontEnum.Red),remoteUserId );
            MyVisualScriptLogicProvider.SendChatMessage($"Limit reached",BlockLimiterConfig.Instance.ServerName,playerId,MyFontEnum.Red);
            Utilities.SendFailSound(remoteUserId);
            Utilities.ValidationFailed();
            return false;


        }


        private static bool BuildBlocksRequest(MyCubeGrid __instance, HashSet<MyCubeGrid.MyBlockLocation> locations)
        {
            
            if (!BlockLimiterConfig.Instance.EnableLimits) return true;

            var grid = __instance;
            if (grid == null)
            {
                Log.Debug("Null grid in BuildBlockHandler");
                return true;
            }

            if (!locations.Any()) return false;
            
            var def = new HashSet<MyCubeBlockDefinition>();
            

            foreach (var item in locations)
            {
                def.Add(MyDefinitionManager.Static.GetCubeBlockDefinition(item.BlockDefinition));
            }
            
            var remoteUserId = MyEventContext.Current.Sender.Value;
            var playerId = Utilities.GetPlayerIdFromSteamId(remoteUserId);
            
            if (def.Any(x=>!Block.AllowBlock(x,playerId,grid)))
            {
                var b = def.Count;
                var x = locations.FirstOrDefault().BlockDefinition.SubtypeId.String;
                if (BlockLimiterConfig.Instance.EnableLog)
                    Log.Info($"Blocked {Utilities.GetPlayerNameFromSteamId(remoteUserId)} from placing {x} blocks due to limits");
                //ModCommunication.SendMessageTo(new NotificationMessage($"You've reach your limit for {b}",5000,MyFontEnum.Red),remoteUserId );
                MyVisualScriptLogicProvider.SendChatMessage($"Limit reached",BlockLimiterConfig.Instance.ServerName,playerId,MyFontEnum.Red);
                Utilities.SendFailSound(remoteUserId);
                Utilities.ValidationFailed();
                return false;
            }

            foreach (var block  in def)
            {
                Block.Add(block,playerId);
            }

            Task.Run(() =>
            {
                Thread.Sleep(100);
                MySandboxGame.Static.Invoke(() =>
                {
                    Grid.UpdateLimit(grid);
                }, "BlockLimiter");
            });

            return true;
            }
            

    }
}