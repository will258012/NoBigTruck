using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace NoBigTruck
{
    public class ModInfo : IUserMod
    {
        public string Name => $"{nameof(NoBigTruck)} {Version} [BETA]";
        public string Description => "Large trucks dont deliver goods to stores";
        public string Version => Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).OfType<AssemblyFileVersionAttribute>().FirstOrDefault() is AssemblyFileVersionAttribute versionAttribute ? versionAttribute.Version : string.Empty;

        public void OnEnabled()
        {
            Logger.LogInfo(() => nameof(OnEnabled));

            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());

            Options.Init();
        }

        public void OnDisabled()
        {
            Logger.LogInfo(() => nameof(OnDisabled));

            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            Logger.LogInfo(() => nameof(OnSettingsUI));

            if (helper is UIHelper uiHelper && uiHelper.self is UIScrollablePanel panel)
            {
                panel.autoLayoutPadding = new RectOffset(0, 0, 0, 20);
                Options.GetUI(panel);
            }
            else
                Logger.LogError(() => "Can't create options panel");
        }
    }
}
