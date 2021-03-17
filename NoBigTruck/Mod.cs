using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using ModsCommon;
using ModsCommon.UI;
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
    public class Mod : BasePatcherMod<Mod, Patcher>
    {
        protected override string ModName => "No Big Truck";
        protected override string ModDescription => "Large trucks dont deliver goods to stores";
        protected override string ModId => nameof(NoBigTruck);
        protected override Version ModVersion => Assembly.GetExecutingAssembly().GetName().Version;
        protected override bool ModIsBeta => false;

        protected override List<Version> ModVersions { get; } = new List<Version>
        {
            new Version("1.0"),
        };

        public override string WorkshopUrl => "https://steamcommunity.com/sharedfiles/filedetails/?id=2069057130";
        protected override string ModLocale => throw new NotImplementedException();

        protected override Patcher CreatePatcher() => new Patcher(this);

        protected override void GetSettings(UIHelperBase helper) => Settings.OnSettingsUI(helper);

        public override void OnLoadedError()
        {
            var messageBox = MessageBoxBase.ShowModal<OneButtonMessageBox>();
            messageBox.MessageText = "Mod loaded with error";
        }
    }
}
