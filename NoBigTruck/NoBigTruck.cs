using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using HarmonyLib;
using ICities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NoBigTruck
{
    public class ModInfo : IUserMod
    {
        public string Name => nameof(NoBigTruck);

        public string Description => "Large trucks do not deliver goods to stores";

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }

    }
    public static class Patcher
    {
        private const string HarmonyId = "NoBigTruck";

        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) return;

            UnityEngine.Debug.Log("Harmony 2 Example: Patching...");

            patched = true;

            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void UnpatchAll()
        {
            if (!patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);

            patched = false;

            UnityEngine.Debug.Log("Harmony 2 Example: Reverted...");
        }
    }

    // Random example patch
    [HarmonyPatch(typeof(IndustrialBuildingAI), nameof(IndustrialBuildingAI.StartTransfer))]
    public static class IndustrialBuildingAIStartTransferPatch
    {
        public static bool Prefix(IndustrialBuildingAI __instance, ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            UnityEngine.Debug.Log($"StartTransfer: {buildingID}\n{nameof(material)}: {material}; {nameof(offer.Building)}: {offer.Building}; {nameof(offer.Vehicle)}: {offer.Vehicle};");

            try
            {
                var manager = Singleton<BuildingManager>.instance;
                if (!(manager.m_buildings.m_buffer[offer.Building] is Building targetBuilding))
                    return true;

                if (!CheckItemClass(targetBuilding.Info.m_class))
                    return true;

                if (!(AccessTools.Method(typeof(IndustrialBuildingAI), "GetOutgoingTransferReason") is MethodInfo method) || !(method.Invoke(__instance, new object[0]) is TransferManager.TransferReason reasonMaterial))
                    return true;

                if (material != reasonMaterial)
                    return true;

                 if(!(GetRandomVehicleInfo(__instance.m_info.m_class.m_service, __instance.m_info.m_class.m_subService, (ItemClass.Level)data.m_level) is VehicleInfo randomVehicleInfo))
                    return true;

                Array16<Vehicle> vehicles = Singleton<VehicleManager>.instance.m_vehicles;
                if (Singleton<VehicleManager>.instance.CreateVehicle(out ushort vehicle, ref Singleton<SimulationManager>.instance.m_randomizer, randomVehicleInfo, data.m_position, material, transferToSource: false, transferToTarget: true))
                {
                    randomVehicleInfo.m_vehicleAI.SetSource(vehicle, ref vehicles.m_buffer[vehicle], buildingID);
                    randomVehicleInfo.m_vehicleAI.StartTransfer(vehicle, ref vehicles.m_buffer[vehicle], material, offer);
                    ushort building = offer.Building;
                    if (building != 0 && (Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].m_flags & Building.Flags.IncomingOutgoing) != 0)
                    {
                        randomVehicleInfo.m_vehicleAI.GetSize(vehicle, ref vehicles.m_buffer[vehicle], out int size, out int _);
                        CommonBuildingAI.ExportResource(buildingID, ref data, material, size);
                    }
                    data.m_outgoingProblemTimer = 0;
                }

                return false;
            }

            catch (Exception error)
            {
                UnityEngine.Debug.LogError(error.Message);
                return true;
            }
        }

        public static bool CheckItemClass(ItemClass itemClass)
        {
            if (itemClass.m_service != ItemClass.Service.Commercial)
                return false;

            switch (itemClass.m_subService)
            {
                case ItemClass.SubService.CommercialLow:
                case ItemClass.SubService.CommercialHigh:
                case ItemClass.SubService.CommercialEco:
                case ItemClass.SubService.CommercialLeisure:
                case ItemClass.SubService.CommercialTourist:
                    return true;
                default:
                    return false;
            }
        }
        
        public static VehicleInfo GetRandomVehicleInfo(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level)
        {
            if (!(AccessTools.Method(typeof(VehicleManager), "GetTransferIndex") is MethodInfo method) || !(method.Invoke(null, new object[] { service, subService, level }) is int transferIndex))
                return null;

            if (!(AccessTools.Field(typeof(VehicleManager), "m_transferVehicles") is FieldInfo field) || !(field.GetValue(Singleton<VehicleManager>.instance) is FastList<ushort>[] transferVehicles))
                return null;

            var fastList = transferVehicles[transferIndex];
            var shortList = new List<ushort>();
            foreach(var item in fastList)
            {
                var vehicleInfo = PrefabCollection<VehicleInfo>.GetPrefab(item);
                if (!vehicleInfo.m_isLargeVehicle)
                    shortList.Add(item);
            }
            if (!shortList.Any())
                return null;

            var index = Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)shortList.Count);
            UnityEngine.Debug.Log($"VehicleSelected: {shortList[index]}");

            return PrefabCollection<VehicleInfo>.GetPrefab(shortList[index]);
        }
    }

}
