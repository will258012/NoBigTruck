using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using HarmonyLib;
using ModsCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NoBigTruck
{
    public static class Manager
    {
        private delegate int GetTransferIndexDelegate(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level);
        private delegate FastList<ushort>[] GetTransferVehiclesDelegate(VehicleManager manager);

        private static GetTransferIndexDelegate GetTransferIndex { get; } 
        private static GetTransferVehiclesDelegate GetTransferVehicles { get; }

        private static Dictionary<int, List<VehicleInfo>> NoBigTrucks { get; } = new Dictionary<int, List<VehicleInfo>>();

        static Manager()
        {
            GetTransferIndex = AccessTools.MethodDelegate<GetTransferIndexDelegate>(AccessTools.Method(typeof(VehicleManager), "GetTransferIndex"));

            var definition = new DynamicMethod("GetTransferVehicles", typeof(FastList<ushort>[]), new Type[1] { typeof(VehicleManager) }, true);
            var generator = definition.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, AccessTools.Field(typeof(VehicleManager), "m_transferVehicles"));
            generator.Emit(OpCodes.Ret);

            GetTransferVehicles = (GetTransferVehiclesDelegate)definition.CreateDelegate(typeof(GetTransferVehiclesDelegate));
        }

        public static void RefreshTransferVehicles()
        {
#if DEBUG
            SingletonMod<Mod>.Logger.Debug(nameof(RefreshTransferVehicles));
#endif
            NoBigTrucks.Clear();
        }
        public static void AVOCheckChanged(UIComponent component, UICheckBox ___m_isLargeVehicle)
        {
            if (component == ___m_isLargeVehicle)
                RefreshTransferVehicles();
        }

        private static List<VehicleInfo> GetVehicle(int transferIndex)
        {
            if (!NoBigTrucks.TryGetValue(transferIndex, out List<VehicleInfo> vehicles))
            {
                vehicles = new List<VehicleInfo>();
                NoBigTrucks[transferIndex] = vehicles;

                var fastList = GetTransferVehicles(Singleton<VehicleManager>.instance)[transferIndex];
                foreach (var index in fastList)
                {
                    var vehicleInfo = PrefabCollection<VehicleInfo>.GetPrefab(index);
                    if (!vehicleInfo.m_isLargeVehicle)
                        vehicles.Add(vehicleInfo);
                }
#if DEBUG
                SingletonMod<Mod>.Logger.Debug($"Transfer index {transferIndex}: {vehicles.Count} Vehicles");
#endif
            }

            return vehicles;
        }
        public static VehicleInfo GetRandomVehicleInfo(VehicleManager manager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, ushort sourceBuildingId, ushort targetBuildingId, TransferManager.TransferReason material)
        {
            if (material == TransferManager.TransferReason.Goods)
            {

                var selectVehicle = default(VehicleInfo);
#if DEBUG
                var log = $"\n\tGet vehicle: source={sourceBuildingId}; target={targetBuildingId};";
                if (CheckRules(sourceBuildingId, targetBuildingId, ref log))
#else
                if (CheckRules(sourceBuildingId, targetBuildingId))
#endif
                {
                    var transferIndex = GetTransferIndex(service, subService, level);
                    var noBigTrucks = GetVehicle(transferIndex);

                    if (noBigTrucks.Any())
                    {
                        var selectIndex = r.Int32((uint)noBigTrucks.Count);
                        selectVehicle = noBigTrucks[selectIndex];
                    }
#if DEBUG
                    else
                        log += "No one no big truck";
#endif
                }

                selectVehicle ??= manager.GetRandomVehicleInfo(ref r, service, subService, level);
#if DEBUG
                log += $"\n\t{(selectVehicle != null ? $"Selected vehicle: { selectVehicle.name}" : "Vehicle not found")}";
                SingletonMod<Mod>.Logger.Debug(log);
#endif
                return selectVehicle;
            }
            else
                return manager.GetRandomVehicleInfo(ref r, service, subService, level);
        }

        public static VehicleInfo GetTransferVehicleService(TransferManager.TransferReason material, ItemClass.Level level, ref Randomizer randomizer, ushort targetBuildingId, ushort sourceBuildingId)
        {
            var vehicleInfo = WarehouseAI.GetTransferVehicleService(material, level, ref randomizer);

            if (vehicleInfo == null)
                return null;
            else
                return GetRandomVehicleInfo(Singleton<VehicleManager>.instance, ref randomizer, vehicleInfo.GetService(), vehicleInfo.GetSubService(), level, targetBuildingId, sourceBuildingId, material);
        }

#if DEBUG
        public static bool CheckRules(ushort sourceBuildingId, ushort targetBuildingId, ref string log)
#else
        public static bool CheckRules(ushort sourceBuildingId, ushort targetBuildingId)
#endif
        {
            var sourceType = GetSourceBuildings(sourceBuildingId, out var _);
            var targetType = GetTargetBuildings(targetBuildingId, out var targetBuildingInfo);

            var buildingLength = targetBuildingInfo.m_cellLength;
            var buildingWidth = targetBuildingInfo.m_cellWidth;

            var result = SourceBuildingTypes.All.IsFlagSet(sourceType) && TargetBuildingTypes.All.IsFlagSet(targetType) && Settings.Rules.Any(CheckRule);
#if DEBUG
            log += $"\n\tCheck rules: result={result}; source={sourceType}; target={targetType}; length={buildingLength}; width={buildingWidth}; ";
#endif
            return result;

            bool CheckRule(Rule rule)
            {
                if ((rule.SourceBuildings & sourceType) == 0 || (rule.TargetBuildings & targetType) == 0)
                    return false;
                else if (!rule.UseSize)
                    return true;
                else
                    return buildingLength <= rule.MaxLength && buildingWidth <= rule.MaxWidth;
            }
        }
        static BuildingInfo GetBuildingInfo(ushort buildingId) => Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].Info;
        static SourceBuildingTypes GetSourceBuildings(ushort id, out BuildingInfo buildingInfo)
        {
            buildingInfo = GetBuildingInfo(id);

            return buildingInfo.m_class.m_service switch
            {
                ItemClass.Service.Road or ItemClass.Service.PublicTransport => SourceBuildingTypes.Outside,
                ItemClass.Service.PlayerIndustry => SourceBuildingTypes.Warehouse,
                ItemClass.Service.Industrial => SourceBuildingTypes.Industry,
                _ => SourceBuildingTypes.None,
            };
        }
        static TargetBuildingTypes GetTargetBuildings(ushort id, out BuildingInfo buildingInfo)
        {
            buildingInfo = GetBuildingInfo(id);

            return buildingInfo.m_class switch
            {
                { m_subService: ItemClass.SubService.CommercialLow } => TargetBuildingTypes.Low,
                { m_subService: ItemClass.SubService.CommercialHigh } => TargetBuildingTypes.High,
                { m_subService: ItemClass.SubService.CommercialEco } => TargetBuildingTypes.Eco,
                { m_subService: ItemClass.SubService.CommercialLeisure } => TargetBuildingTypes.Leisure,
                { m_subService: ItemClass.SubService.CommercialTourist } => TargetBuildingTypes.Tourist,
                { m_service: ItemClass.Service.Road } or { m_service: ItemClass.Service.PublicTransport} => TargetBuildingTypes.Outside,
                { m_service: ItemClass.Service.PlayerIndustry } => TargetBuildingTypes.Warehouse,
                { m_service: ItemClass.Service.Industrial } => TargetBuildingTypes.Industry,
                { m_service: ItemClass.Service.Disaster } => TargetBuildingTypes.Disaster,
                _ => TargetBuildingTypes.None,
            };
        }
    }
}
