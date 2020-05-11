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

        public void OnSettingsUI(UIHelperBase helper)
        {
            if (helper is UIHelper uiHelper && uiHelper.self is UIScrollablePanel panel)
            {
                panel.autoLayoutPadding = new RectOffset(0, 0, 0, 20);

                Options.Init(panel);

                var addButton = panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsButtonTemplate")) as UIButton;
                addButton.text = "Add new rule";
                addButton.eventClick += (_,__) => Options.Rules.NewRule();
            }
            else
                Debug.LogWarning("Can't create options panel");
        }

        string PanelInfo(UIScrollablePanel panel) => $"{panel.autoLayoutPadding};{panel.scrollPadding}";
    }
}
