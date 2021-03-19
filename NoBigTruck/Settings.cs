using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.UI;
using ICities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NoBigTruck
{
    public static class Settings
    {
        public static string ConfigFile => Path.Combine(DataLocation.localApplicationData, $"{nameof(NoBigTruck)}.xml");
        private static bool IsLoaded { get; set; } = false;
        public static List<Rule> Rules { get; } = new List<Rule>();
        static AddRuleButton AddButton { get; set; }

        public static void OnSettingsUI(UIHelperBase helper)
        {
            var mainPanel = (helper as UIHelper).self as UIScrollablePanel;
            mainPanel.autoLayoutPadding = new RectOffset(100, 100, 0, 25);
            mainPanel.eventSizeChanged += MainPanelSizeChanged;

            Load();

            AddAddRuleButton(mainPanel);
            AddRulePanels(mainPanel);
        }

        private static void AddAddRuleButton(UIScrollablePanel mainPanel)
        {
            AddButton = mainPanel.AddUIComponent<AddRuleButton>();
            AddButton.Text = "Add new rule";
            AddButton.Init();
            AddButton.OnButtonClick += AddRule;

            void AddRule()
            {
                var rule = new Rule();
                Settings.AddRule(rule);
                AddRulePanel(rule, mainPanel);
            }
        }
        private static void AddRulePanels(UIScrollablePanel mainPanel)
        {
            foreach (var rule in Rules)
                AddRulePanel(rule, mainPanel);
        }
        private static void AddRulePanel(Rule rule, UIScrollablePanel mainPanel)
        {
            var rulePanel = mainPanel.AddUIComponent<PropertyGroupPanel>();
            rulePanel.Init();
            AddButton.zOrder = rulePanel.zOrder + 1;

            rulePanel.StopLayout();
            var header = rulePanel.AddUIComponent<RuleHeaderPanel>();
            header.Init(30f);
            header.OnDelete += OnDeleteRule;

            rule.GetUIComponents(rulePanel);
            rulePanel.StartLayout();

            void OnDeleteRule()
            {
                mainPanel.RemoveUIComponent(rulePanel);
                UnityEngine.Object.Destroy(rulePanel);
                DeleteRule(rule);
            }
        }

        private static void MainPanelSizeChanged(UIComponent component, Vector2 value)
        {
            var mainPanel = component as UIScrollablePanel;
            foreach (var item in mainPanel.components)
                item.width = mainPanel.width - mainPanel.autoLayoutPadding.horizontal - mainPanel.scrollPadding.horizontal;
        }

        static void Load()
        {
            Rules.Clear();

            if (!File.Exists(ConfigFile))
            {
                Mod.Logger.Debug($"Config file not exist, create default");
                AddRule(new Rule());
                Save();
            }
            else
            {
                var file = XmlExtension.Load(ConfigFile);

                foreach (var config in file.Root.Elements(nameof(Rule)))
                {
                    var rule = new Rule();
                    rule.FromXml(config);
                    AddRule(rule);
                }
            }

            IsLoaded = true;

            Mod.Logger.Debug($"Config loaded: {Rules.Count} rules");
        }

        static void Save()
        {
            var config = new XElement("Config");

            foreach (var rule in Rules)
                config.Add(rule.ToXml());

            new XDocument(config).Save(ConfigFile);
        }
        static void AddRule(Rule rule)
        {
            rule.OnRuleChanged = RuleOnChanged;
            Rules.Add(rule);

            if (IsLoaded)
                Save();
        }
        static void DeleteRule(Rule rule)
        {
            Rules.Remove(rule);
            Save();
        }
        private static void RuleOnChanged() => Save();
    }

    public class Rule
    {
        public Action OnRuleChanged { private get; set; }

        public PropertyEnumValue<SourceBuildingTypes> SourceBuildings { get; }
        public PropertyEnumValue<TargetBuildingTypes> TargetBuildings { get; }
        public PropertyBoolValue UseSize { get; }
        public PropertyValue<int> MaxLength { get; }
        public PropertyValue<int> MaxWidth { get; }

        public Rule(Action onRuleChanged = null)
        {
            OnRuleChanged = onRuleChanged;

            SourceBuildings = new PropertyEnumValue<SourceBuildingTypes>("Source", RuleChanged, SourceBuildingTypes.All);
            TargetBuildings = new PropertyEnumValue<TargetBuildingTypes>("Target", RuleChanged, TargetBuildingTypes.All);
            UseSize = new PropertyBoolValue("UseSize", RuleChanged, false);
            MaxLength = new PropertyValue<int>("MaxLength", RuleChanged, 4);
            MaxWidth = new PropertyValue<int>("MaxWidth", RuleChanged, 4);
        }

        private void RuleChanged() => OnRuleChanged?.Invoke();

        public virtual List<EditorItem> GetUIComponents(UIComponent parent)
        {
            var components = new List<EditorItem>();

            components.Add(AddSourceBuildingProperty(parent));
            components.Add(AddTargetBuildingProperty(parent));

            var useSize = AddUseSizeProperty(parent);
            var maxLength = AddSizeProperty(parent, MaxLength, "Max buildings length");
            var maxWidth = AddSizeProperty(parent, MaxLength, "Max buildings width");

            components.Add(useSize);
            components.Add(maxLength);
            components.Add(maxWidth);

            useSize.OnSelectObjectChanged += ChangeSizeVisible;
            ChangeSizeVisible(useSize.SelectedObject);

            void ChangeSizeVisible(bool useSize)
            {
                maxLength.isVisible = useSize;
                maxWidth.isVisible = useSize;
            }

            return components;
        }

        private SourceBuildingPropertyPanel AddSourceBuildingProperty(UIComponent parent)
        {
            var sourceBuildingProperty = parent.AddUIComponent<SourceBuildingPropertyPanel>();
            sourceBuildingProperty.Text = "Source buildings type";
            sourceBuildingProperty.Init();
            sourceBuildingProperty.SelectedObject = SourceBuildings;
            sourceBuildingProperty.OnSelectObjectChanged += (value) => SourceBuildings.Value = value;

            return sourceBuildingProperty;
        }
        private TargetBuildingPropertyPanel AddTargetBuildingProperty(UIComponent parent)
        {
            var targetBuildingProperty = parent.AddUIComponent<TargetBuildingPropertyPanel>();
            targetBuildingProperty.Text = "Target buildings type";
            targetBuildingProperty.Init();
            targetBuildingProperty.SelectedObject = TargetBuildings;
            targetBuildingProperty.OnSelectObjectChanged += (value) => TargetBuildings.Value = value;

            return targetBuildingProperty;
        }
        protected BoolListPropertyPanel AddUseSizeProperty(UIComponent parent)
        {
            var useSizeProperty = parent.AddUIComponent<BoolListPropertyPanel>();
            useSizeProperty.Text = "Check target buildings size";
            useSizeProperty.Init("No", "Yes");
            useSizeProperty.SelectedObject = UseSize;
            useSizeProperty.OnSelectObjectChanged += (bool value) => UseSize.Value = value;
            return useSizeProperty;
        }
        protected IntPropertyPanel AddSizeProperty(UIComponent parent, PropertyValue<int> property, string text)
        {
            var lineCountProperty = parent.AddUIComponent<IntPropertyPanel>();
            lineCountProperty.Text = text;
            lineCountProperty.UseWheel = true;
            lineCountProperty.WheelStep = 1;
            lineCountProperty.CheckMin = true;
            lineCountProperty.MinValue = 1;
            lineCountProperty.Init();
            lineCountProperty.Value = property;
            lineCountProperty.OnValueChanged += (int value) => property.Value = value;

            return lineCountProperty;
        }

        public void FromXml(XElement config)
        {
            SourceBuildings.FromXml(config, SourceBuildings);
            TargetBuildings.FromXml(config, TargetBuildings);
            UseSize.FromXml(config, UseSize);
            MaxLength.FromXml(config, MaxLength);
            MaxWidth.FromXml(config, MaxWidth);
        }
        public XElement ToXml()
        {
            var config = new XElement("Rule");

            SourceBuildings.ToXml(config);
            TargetBuildings.ToXml(config);
            UseSize.ToXml(config);
            MaxLength.ToXml(config);
            MaxWidth.ToXml(config);

            return config;
        }
    }

    [Flags]
    public enum SourceBuildingTypes
    {
        Industry = 1,
        Outside = 2,
        Warehouse = 4,

        [NotVisible]
        All = Industry | Outside | Warehouse,
    }

    [Flags]
    public enum TargetBuildingTypes
    {
        Low = 1,
        High = 2,
        Eco = 4,
        Leisure = 8,
        Tourist = 16,

        [NotVisible]
        All = Low | High | Eco | Leisure | Tourist,
    }

    public class SourceBuildingPropertyPanel : EnumMultyPropertyPanel<SourceBuildingTypes, SourceBuildingPropertyPanel.SourceBuildingSegmented>
    {
        protected override string GetDescription(SourceBuildingTypes value) => value.ToString();
        protected override bool IsEqual(SourceBuildingTypes first, SourceBuildingTypes second) => first == second;

        public class SourceBuildingSegmented : UIMultySegmented<SourceBuildingTypes> { }
    }
    public class TargetBuildingPropertyPanel : EnumMultyPropertyPanel<TargetBuildingTypes, TargetBuildingPropertyPanel.TargetBuildingSegmented>
    {
        protected override string GetDescription(TargetBuildingTypes value) => value.ToString();
        protected override bool IsEqual(TargetBuildingTypes first, TargetBuildingTypes second) => first == second;

        public class TargetBuildingSegmented : UIMultySegmented<TargetBuildingTypes> { }
    }
    public class AddRuleButton : ButtonPanel
    {
        public AddRuleButton()
        {
            Button.textScale = 1f;
        }
        protected override void SetSize() => Button.size = size;
    }
    public class RuleHeaderPanel : HeaderPanel { }
}
