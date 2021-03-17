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
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patcher), nameof(Patcher.GetRandomVehicleInfo)));
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
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patcher), nameof(Patcher.GetTransferVehicleService)));
                }
                else
                    yield return instruction;
            }
        }


        public static VehicleInfo GetRandomVehicleInfo(VehicleManager manager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, ushort buildingID, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            Mod.Logger.Debug($"{nameof(GetRandomVehicleInfo)}: \nsource: {buildingID}; target: {offer.Building}; {nameof(material)}: {material};");

            try
            {
                if (material == TransferManager.TransferReason.Goods && Check(buildingID, offer.Building))
                {
                    var transferIndex = (int)AccessTools.Method(typeof(VehicleManager), "GetTransferIndex").Invoke(null, new object[] { service, subService, level });
                    var fastList = (AccessTools.Field(typeof(VehicleManager), "m_transferVehicles").GetValue(manager) as FastList<ushort>[])[transferIndex];

                    var notLarge = new List<VehicleInfo>();
                    foreach (var index in fastList)
                    {
                        var vehicleInfo = PrefabCollection<VehicleInfo>.GetPrefab(index);
                        if (!vehicleInfo.m_isLargeVehicle)
                            notLarge.Add(vehicleInfo);
                    }

                    if (notLarge.Any())
                    {
                        var selectIndex = r.Int32((uint)notLarge.Count);
                        var selectVehicle = notLarge[selectIndex];
                        Mod.Logger.Debug($"VehicleSelected: {selectVehicle}");

                        return selectVehicle;
                    }
                    else
                        Mod.Logger.Debug($"No one not large vehicle");
                }
            }
            catch (Exception error)
            {
                Mod.Logger.Error("Cant select vehicle",error);
            }

            return manager.GetRandomVehicleInfo(ref r, service, subService, level);
        }

        public static VehicleInfo GetTransferVehicleService(TransferManager.TransferReason material, ItemClass.Level level, ref Randomizer randomizer, ushort buildingID, TransferManager.TransferOffer offer)
        {
            var vehicleInfo = WarehouseAI.GetTransferVehicleService(material, level, ref randomizer);
            return vehicleInfo == null ? null : GetRandomVehicleInfo(Singleton<VehicleManager>.instance, ref randomizer, vehicleInfo.GetService(), vehicleInfo.GetSubService(), level, buildingID, material, offer);
        }

        public static bool Check(ushort sourceBuildingId, ushort targetBuildingId)
        {
            var sourceBuildingInfo = GetBuildingInfo(sourceBuildingId);
            var targetBuildingInfo = GetBuildingInfo(targetBuildingId);

            var sourceType = GetSourceBuildings(sourceBuildingInfo);
            var targetType = GetTargetBuildings(targetBuildingInfo);

            var buildingLength = targetBuildingInfo.m_cellLength;
            var buildingWidth = targetBuildingInfo.m_cellWidth;

            var result = sourceType != 0 && targetType != 0 && Settings.Rules.Any(r => (r.SourceBuildings & sourceType) != 0 && (r.TargetBuildings & targetType) != 0 && (!r.UseSize || (buildingLength <= r.MaxLength && buildingWidth <= r.MaxWidth)));

            //Logger.LogDebug(() => $"{nameof(Check)}: {nameof(sourceType)}={sourceType}; {nameof(targetType)}={targetType}; {nameof(buildingLength)}={buildingLength}; {nameof(buildingWidth)}={buildingWidth}; {nameof(result)}={result};");

            return result;
        }
        static BuildingInfo GetBuildingInfo(ushort buildingId) => Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].Info;

        static SourceBuildingTypes GetSourceBuildings(BuildingInfo buildingInfo) => buildingInfo.m_class.m_service switch
        {
            ItemClass.Service.Road => SourceBuildingTypes.Outside,
            ItemClass.Service.PlayerIndustry => SourceBuildingTypes.Warehouse,
            ItemClass.Service.Industrial => SourceBuildingTypes.Industry,
            _ => 0,
        };

        static TargetBuildingTypes GetTargetBuildings(BuildingInfo buildingInfo)
        {
            if (buildingInfo.m_class.m_service != ItemClass.Service.Commercial)
                return 0;

            return buildingInfo.m_class.m_subService switch
            {
                ItemClass.SubService.CommercialLow => TargetBuildingTypes.Low,
                ItemClass.SubService.CommercialHigh => TargetBuildingTypes.High,
                ItemClass.SubService.CommercialEco => TargetBuildingTypes.Eco,
                ItemClass.SubService.CommercialLeisure => TargetBuildingTypes.Leisure,
                ItemClass.SubService.CommercialTourist => TargetBuildingTypes.Tourist,
                _ => 0,
            };
        }
    }
}
