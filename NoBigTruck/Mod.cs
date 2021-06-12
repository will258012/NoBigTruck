using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using static ColossalFramework.Plugins.PluginManager;

namespace NoBigTruck
{
    public class Mod : BasePatcherMod<Mod>
    {
        public override string NameRaw => "No Big Truck";
        public override string Description => !IsBeta ? Localize.Mod_Description : CommonLocalize.Mod_DescriptionBeta;

        protected override ulong StableWorkshopId => 2069057130ul;
        protected override ulong BetaWorkshopId => 2513186434ul;

        public override List<Version> Versions { get; } = new List<Version>
        {
            new Version("1.2"),
            new Version("1.1"),
            new Version("1.0"),
        };

#if BETA
        public override bool IsBeta => true;
#else
        public override bool IsBeta => false;
#endif
        protected override string IdRaw => nameof(NoBigTruck);

        protected override ResourceManager LocalizeManager => Localize.ResourceManager;
        protected override List<BaseDependencyInfo> DependencyInfos
        {
            get
            {
                var infos = base.DependencyInfos;

                var info = new NeedDependencyInfo(DependencyState.Enable, AVOSearcher, AVOName, AVOId);
                infos.Add(info);

                return infos;
            }
        }
        private static string AVOName => "Advanced Vehicle Options";
        private static ulong AVOId => 1548831935ul;
        private static PluginSearcher AVOSearcher { get; } = PluginUtilities.GetSearcher(AVOName, AVOId);
        public static PluginInfo AVO => PluginUtilities.GetPlugin(AVOSearcher);

        public override string GetLocalizeString(string str, CultureInfo culture = null) => Localize.ResourceManager.GetString(str, culture ?? Culture);
        protected override void GetSettings(UIHelperBase helper)
        {
            var settings = new Settings();
            settings.OnSettingsUI(helper);
        }
        protected override void SetCulture(CultureInfo culture) => Localize.Culture = culture;

        #region PATCHER

        protected override bool PatchProcess()
        {
            var success = true;

            success &= IndustrialBuildingAI_StartTransfer_Patch();
            success &= IndustrialExtractorAI_StartTransfer_Patch();
            success &= OutsideConnectionAI_StartConnectionTransferImpl_Patch();
            success &= WarehouseAI_StartTransfer_Patch();
            success &= CargoTruckAI_ChangeVehicleType_Patch();
            success &= VehicleManager_RefreshTransferVehicles_Patch();

            if (AVO is null)
                Logger.Debug("Advanced Vehicle Options not exist, skip patches");
            else
                AVOPatch(ref success);

            return success;
        }

        private bool IndustrialBuildingAI_StartTransfer_Patch()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.StartTransfer_Transpiler), typeof(IndustrialBuildingAI), nameof(IndustrialBuildingAI.StartTransfer));
        }
        private bool IndustrialExtractorAI_StartTransfer_Patch()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.StartTransfer_Transpiler), typeof(IndustrialExtractorAI), nameof(IndustrialExtractorAI.StartTransfer));
        }
        private bool OutsideConnectionAI_StartConnectionTransferImpl_Patch()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.StartTransfer_Transpiler), typeof(OutsideConnectionAI), "StartConnectionTransferImpl");
        }
        private bool WarehouseAI_StartTransfer_Patch()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.WarehouseAI_StartTransfer_Transpiler), typeof(WarehouseAI), nameof(WarehouseAI.StartTransfer));
        }
        private bool CargoTruckAI_ChangeVehicleType_Patch()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.CargoTruckAI_ChangeVehicleType_Transpiler), typeof(CargoTruckAI), nameof(CargoTruckAI.ChangeVehicleType), new Type[] { typeof(VehicleInfo), typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(PathUnit.Position), typeof(uint)});
        }
        private bool VehicleManager_RefreshTransferVehicles_Patch()
        {
            return AddPostfix(typeof(Manager), nameof(Manager.RefreshTransferVehicles), typeof(VehicleManager), nameof(VehicleManager.RefreshTransferVehicles));
        }
        private void AVOPatch(ref bool success)
        {
            success &= AddPostfix(typeof(Manager), nameof(Manager.AVOCheckChanged), Type.GetType("AdvancedVehicleOptionsUID.GUI.UIOptionPanel"), "OnCheckChanged");
            success &= AddPrefix(typeof(Patcher), nameof(Patcher.NBTCheckPrefix), Type.GetType("AdvancedVehicleOptionsUID.Compatibility.NoBigTruckCompatibilityPatch"), "IsNBTActive");
        }

        #endregion
    }
    public class LoadingExtension : BaseLoadingExtension<Mod> { }
    public static class Patcher
    {
        private static MethodInfo ReplaceMethod { get; } = AccessTools.Method(typeof(VehicleManager), nameof(VehicleManager.GetRandomVehicleInfo), new Type[] { typeof(Randomizer).MakeByRefType(), typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level)});
        public static IEnumerable<CodeInstruction> StartTransfer_Transpiler(MethodBase original, ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand == ReplaceMethod)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, original.IsStatic ? 0 : 1);
                    yield return new CodeInstruction(OpCodes.Ldarga_S, original.IsStatic ? 3 : 4);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TransferManager.TransferOffer), nameof(TransferManager.TransferOffer.Building)));
                    yield return new CodeInstruction(OpCodes.Ldarg_S, original.IsStatic ? 2 : 3);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Manager), nameof(Manager.GetRandomVehicleInfo)));
                }
                else
                    yield return instruction;
            }
        }
        public static IEnumerable<CodeInstruction> WarehouseAI_StartTransfer_Transpiler(MethodBase original, ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            var method = AccessTools.Method(typeof(WarehouseAI), nameof(WarehouseAI.GetTransferVehicleService));
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand == method)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarga_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TransferManager.TransferOffer), nameof(TransferManager.TransferOffer.Building)));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Manager), nameof(Manager.GetTransferVehicleService)));
                }
                else
                    yield return instruction;
            }
        }
        public static IEnumerable<CodeInstruction> CargoTruckAI_ChangeVehicleType_Transpiler(MethodBase original, ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand == ReplaceMethod)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vehicle), nameof(Vehicle.m_transferType)));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Manager), nameof(Manager.GetRandomVehicleInfo)));
                }
                else
                    yield return instruction;
            }
        }

        public static bool NBTCheckPrefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}
