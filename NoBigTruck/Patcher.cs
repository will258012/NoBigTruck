using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using HarmonyLib;
using ICities;
using ModsCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace NoBigTruck
{
    public class Patcher : Patcher<Mod>
    {
        public Patcher(BaseMod<Mod> mod) : base(mod) { }

        protected override bool PatchProcess()
        {
            var success = true;

            success &= IndustrialBuildingAIStartTransferPatch();
            success &= OutsideConnectionAIStartConnectionTransferImplPatch();
            success &= WarehouseAIStartTransferPatch();
            success &= VehicleManagerRefreshTransferVehiclesPatch();
            success &= AVOPatch();

            return success;
        }

        private bool IndustrialBuildingAIStartTransferPatch()
        {
            var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.BuildingDecorationLoadPathsTranspiler));

            return AddTranspiler(transpiler, typeof(BuildingDecoration), nameof(BuildingDecoration.LoadPaths));
        }
        private bool OutsideConnectionAIStartConnectionTransferImplPatch()
        {
            var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.BuildingDecorationLoadPathsTranspiler));

            return AddTranspiler(transpiler, typeof(OutsideConnectionAI), "StartConnectionTransferImpl");
        }
        private static IEnumerable<CodeInstruction> BuildingDecorationLoadPathsTranspiler(MethodBase original, ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand?.ToString().Contains(nameof(VehicleManager.GetRandomVehicleInfo)) == true)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, original.IsStatic ? 0 : 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, original.IsStatic ? 2 : 3);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, original.IsStatic ? 3 : 4);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Manager), nameof(Manager.GetRandomVehicleInfo)));
                }
                else
                    yield return instruction;
            }
        }

        private bool WarehouseAIStartTransferPatch()
        {
            var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.WarehouseAIStartTransferTranspiler));

            return AddTranspiler(transpiler, typeof(WarehouseAI), nameof(WarehouseAI.StartTransfer));
        }
        private static IEnumerable<CodeInstruction> WarehouseAIStartTransferTranspiler(MethodBase original, ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand?.ToString().Contains(nameof(WarehouseAI.GetTransferVehicleService)) == true)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Manager), nameof(Manager.GetTransferVehicleService)));
                }
                else
                    yield return instruction;
            }
        }

        private bool VehicleManagerRefreshTransferVehiclesPatch()
        {
            var postfix = AccessTools.Method(typeof(Manager), nameof(Manager.RefreshTransferVehicles));

            return AddPostfix(postfix, typeof(VehicleManager), nameof(VehicleManager.RefreshTransferVehicles));
        }
        private bool AVOPatch()
        {
            var postfix = AccessTools.Method(typeof(Manager), nameof(Manager.AVOCheckChanged));

            try { return AddPostfix(postfix, Type.GetType("AdvancedVehicleOptionsUID.GUI.UIOptionPanel"), "OnCheckChanged"); }
            catch { return true; }
        }
    }
}
