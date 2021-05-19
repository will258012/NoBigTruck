using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using HarmonyLib;
using ModsCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NoBigTruck
{
    public static class Manager
    {
        private static MethodInfo GetTransferIndexDelegate { get; } = AccessTools.Method(typeof(VehicleManager), "GetTransferIndex");
        private static FieldInfo TransferVehiclesDelegate { get; } = AccessTools.Field(typeof(VehicleManager), "m_transferVehicles");
        private static Dictionary<int, List<VehicleInfo>> NoBigTrucks { get; } = new Dictionary<int, List<VehicleInfo>>();

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

                var fastList = (TransferVehiclesDelegate.GetValue(Singleton<VehicleManager>.instance) as FastList<ushort>[])[transferIndex];
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

        public static VehicleInfo GetRandomVehicleInfo(VehicleManager manager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, ushort buildingID, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
#if DEBUG
            SingletonMod<Mod>.Logger.Debug($"{nameof(GetRandomVehicleInfo)}: \nsource: {buildingID}; target: {offer.Building}; {nameof(material)}: {material};");
#endif
            if (material == TransferManager.TransferReason.Goods && CheckRules(buildingID, offer.Building))
            {
                var transferIndex = (int)GetTransferIndexDelegate.Invoke(null, new object[] { service, subService, level });
                var noBigTrucks = GetVehicle(transferIndex);

                if (noBigTrucks.Any())
                {
                    var selectIndex = r.Int32((uint)noBigTrucks.Count);
                    var selectVehicle = noBigTrucks[selectIndex];
#if DEBUG
                    SingletonMod<Mod>.Logger.Debug($"VehicleSelected: {selectVehicle}");
#endif
                    return selectVehicle;
                }
#if DEBUG
                else
                    SingletonMod<Mod>.Logger.Debug($"No one not large vehicle");
#endif
            }

            return manager.GetRandomVehicleInfo(ref r, service, subService, level);
        }

        public static VehicleInfo GetTransferVehicleService(TransferManager.TransferReason material, ItemClass.Level level, ref Randomizer randomizer, ushort buildingID, TransferManager.TransferOffer offer)
        {
            var vehicleInfo = WarehouseAI.GetTransferVehicleService(material, level, ref randomizer);

            if (vehicleInfo == null)
                return null;
            else
                return GetRandomVehicleInfo(Singleton<VehicleManager>.instance, ref randomizer, vehicleInfo.GetService(), vehicleInfo.GetSubService(), level, buildingID, material, offer);
        }

        public static bool CheckRules(ushort sourceBuildingId, ushort targetBuildingId)
        {
            var sourceType = GetSourceBuildings(sourceBuildingId, out var _);
            var targetType = GetTargetBuildings(targetBuildingId, out var targetBuildingInfo);

            var buildingLength = targetBuildingInfo.m_cellLength;
            var buildingWidth = targetBuildingInfo.m_cellWidth;

            var result = sourceType != 0 && targetType != 0 && Settings.Rules.Any(CheckRule);
#if DEBUG
            SingletonMod<Mod>.Logger.Debug($"{nameof(CheckRules)}: {nameof(sourceType)}={sourceType}; {nameof(targetType)}={targetType}; {nameof(buildingLength)}={buildingLength}; {nameof(buildingWidth)}={buildingWidth}; {nameof(result)}={result};");
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
                ItemClass.Service.Road => SourceBuildingTypes.Outside,
                ItemClass.Service.PlayerIndustry => SourceBuildingTypes.Warehouse,
                ItemClass.Service.Industrial => SourceBuildingTypes.Industry,
                _ => 0,
            };
        }
        static TargetBuildingTypes GetTargetBuildings(ushort id, out BuildingInfo buildingInfo)
        {
            buildingInfo = GetBuildingInfo(id);

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
