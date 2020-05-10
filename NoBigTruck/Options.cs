using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;

namespace NoBigTruck
{
    public static class Options
    {
        public static string ConfigFile => $"{nameof(NoBigTruck)}.xml";        
        public static RuleSet Rules { get; set; }

        private static bool IsLoaded { get; set; } = false;

        public static bool Check(ushort sourceBuildingId, ushort targetBuildingId)
        {
            var sourceBuildingInfo = GetBuildingInfo(sourceBuildingId);
            var targetBuildingInfo = GetBuildingInfo(targetBuildingId);

            var sourceType = GetSourceBuildings(sourceBuildingInfo);
            var targetType = GetTargetBuildings(targetBuildingInfo);

            return sourceType != 0 && targetType != 0 && Rules.Any(r => (r.Source & sourceType) != 0 && (r.Target & targetType) != 0);
        }
        static BuildingInfo GetBuildingInfo(ushort buildingId) => Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].Info;

        static SourceBuildings GetSourceBuildings(BuildingInfo buildingInfo)
        {
            switch (buildingInfo.m_class.m_service)
            {
                case ItemClass.Service.Road:
                    return SourceBuildings.Outside;
                case ItemClass.Service.PlayerIndustry:
                    return SourceBuildings.Warehouse;
                case ItemClass.Service.Industrial:
                    return SourceBuildings.Industry;
                default:
                    return 0;
            }
        }
        static TargetBuildings GetTargetBuildings(BuildingInfo buildingInfo)
        {
            if (buildingInfo.m_class.m_service != ItemClass.Service.Commercial)
                return 0;

            switch (buildingInfo.m_class.m_subService)
            {
                case ItemClass.SubService.CommercialLow:
                    return TargetBuildings.Low;
                case ItemClass.SubService.CommercialHigh:
                    return TargetBuildings.High;
                case ItemClass.SubService.CommercialEco:
                    return TargetBuildings.Eco;
                case ItemClass.SubService.CommercialLeisure:
                    return TargetBuildings.Leisure;
                case ItemClass.SubService.CommercialTourist:
                    return TargetBuildings.Tourist;
                default:
                    return 0;
            }
        }


        public static void Init(UIComponent uiParent)
        {
            var ui = uiParent.AddUIComponent<UIRulePanel>();
            ui.Init();

            Rules = new RuleSet(ui);

            Load();
        }
        static void Load()
        {
            try
            {
                var xdoc = new XmlDocument();

                using (FileStream stream = new FileStream(ConfigFile, FileMode.Open))
                {
                    xdoc.Load(stream);

                    foreach (var element in xdoc.DocumentElement.ChildNodes.OfType<XmlElement>().Where(i => i.Name == nameof(Rule)))
                    {
                        var source = int.TryParse(element.GetAttribute(nameof(Rule.Source)), out int tempSource) ? (SourceBuildings)tempSource : Rule.DefaultSource;
                        var target = int.TryParse(element.GetAttribute(nameof(Rule.Target)), out int tempTarget) ? (TargetBuildings)tempTarget : Rule.DefaultTarget;
                        var useSize = bool.TryParse(element.GetAttribute(nameof(Rule.UseSize)), out bool tempUseSize) ? tempUseSize : Rule.DefaultUseSize;
                        var minLength = int.TryParse(element.GetAttribute(nameof(Rule.MinLength)), out int tempMinLength) ? tempMinLength : Rule.DefaultMinLength;
                        var minWidth = int.TryParse(element.GetAttribute(nameof(Rule.MinWidth)), out int tempMinWidth) ? tempMinWidth : Rule.DefaultMinWidth;

                        Rules.NewRule(source, target, useSize, minLength, minWidth);
                    }
                }

                IsLoaded = true;

                Debug.Log($"{nameof(NoBigTruck)}: config loaded: {Rules.Count} rules");
            }
            catch (Exception error)
            {
                Debug.LogError($"{nameof(NoBigTruck)}: Cant load config: {error.Message}\n{error.StackTrace}");
            }
        }
        static void Save()
        {
            try
            {
                var xdoc = new XmlDocument();

                var xmlDeclaration = xdoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xdoc.AppendChild(xmlDeclaration);

                var xroot = xdoc.CreateElement("Config");

                foreach (var rule in Rules)
                {
                    var xrule = xdoc.CreateElement(nameof(Rule));

                    AddAttr(xrule, nameof(Rule.Source), ((int)rule.Source).ToString());
                    AddAttr(xrule, nameof(Rule.Target), ((int)rule.Target).ToString());
                    AddAttr(xrule, nameof(Rule.UseSize), rule.UseSize.ToString());
                    AddAttr(xrule, nameof(Rule.MinLength), rule.MinLength.ToString());
                    AddAttr(xrule, nameof(Rule.MinWidth), rule.MinWidth.ToString());

                    xroot.AppendChild(xrule);
                }

                xdoc.AppendChild(xroot);

                using (FileStream stream = new FileStream(ConfigFile, FileMode.Create))
                {
                    xdoc.Save(stream);
                }

                Debug.Log($"{nameof(NoBigTruck)}: config saved");

                void AddAttr(XmlElement element, string name, string value)
                {
                    var attr = xdoc.CreateAttribute(name);
                    var attrValue = xdoc.CreateTextNode(value);
                    attr.AppendChild(attrValue);
                    element.Attributes.Append(attr);
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"{nameof(NoBigTruck)}: Cant save config: {error.Message}\n{error.StackTrace}");
            }
        }
    }

    public class RuleSet : List<Rule>
    {
        public static UIRulePanel UI { get; private set; }

        public event Action OnChange;

        public RuleSet(UIRulePanel ui)
        {
            UI = ui;
        }

        public void NewRule() => NewRule(Rule.DefaultSource, Rule.DefaultTarget, Rule.DefaultUseSize, Rule.DefaultMinLength, Rule.DefaultMinWidth);
        public void NewRule(SourceBuildings source, TargetBuildings target, bool useSize, int minLength, int minWidth)
        {
            var ruleUI = UI.AddUIComponent<UIRule>();
            ruleUI.Init();

            var rule = new Rule(ruleUI)
            {
                Source = source,
                Target = target,
                UseSize = useSize,
                MinLength = minLength,
                MinWidth = minWidth,
            };
            rule.OnChanged += RuleOnChanged;

            Add(rule);

            OnChange?.Invoke();
        }
        public void DeleteRule(Rule rule)
        {
            Remove(rule);
            UI.RemoveUIComponent(rule.UI);

            OnChange?.Invoke();
        }

        private void RuleOnChanged(Rule rule) => OnChange?.Invoke();
    }

    public class Rule
    {
        public static SourceBuildings DefaultSource => default;
        public static TargetBuildings DefaultTarget => default;
        public static bool DefaultUseSize => false;
        public static int DefaultMinLength => 4;
        public static int DefaultMinWidth => 4;

        public SourceBuildings Source
        {
            get => UI.SourceEnum.Value;
            set => UI.SourceEnum.Value = value;
        }
        public TargetBuildings Target
        {
            get => UI.TargetEnum.Value;
            set => UI.TargetEnum.Value = value;
        }
        public bool UseSize
        {
            get => UI.UseSizeCheckbox.IsChecked;
            set => UI.UseSizeCheckbox.IsChecked = value;
        }
        public int MinLength
        {
            get => (int)UI.LengthSlider.value;
            set => UI.LengthSlider.value = value;
        }
        public int MinWidth
        {
            get => (int)UI.WidthSlider.value;
            set => UI.WidthSlider.value = value;
        }

        public UIRule UI { get; }

        public event Action<Rule> OnChanged;

        public Rule(UIRule ui)
        {
            UI = ui;

            UI.SourceEnum.OnChanged += (_, __) => OnChanged?.Invoke(this);
            UI.TargetEnum.OnChanged += (_, __) => OnChanged?.Invoke(this);
            UI.UseSizeCheckbox.OnChanged += (_, __) => OnChanged?.Invoke(this);
            UI.LengthSlider.eventValueChanged += (_, __) => OnChanged?.Invoke(this);
            UI.WidthSlider.eventValueChanged += (_, __) => OnChanged?.Invoke(this);
        }
    }

    public class UIRulePanel : UIVerticalPanel
    {
        public UIRulePanel()
        {
            autoLayoutPadding = new RectOffset(0, 0, 0, 10);
        }
        public void Init()
        {
            var parent = this.parent as UIScrollablePanel;
            size = new Vector2(parent.width - 2 * parent.scrollPadding.horizontal, parent.height);
        }
    }
    public class UIRule : UIVerticalPanel
    {
        public UISourceGroup SourceEnum { get; private set; }
        public UITargetGroup TargetEnum { get; private set; }
        public UICustomCheckBox UseSizeCheckbox { get; private set; }
        public UISlider LengthSlider { get; private set; }
        public UISlider WidthSlider { get; private set; }

        public void Init()
        {
            backgroundSprite = "GenericPanel";

            autoLayoutPadding = new RectOffset(0, 0, 0, 15);
            padding = new RectOffset(5, 5, 5, 5);
            size = new Vector2(parent.width, 0);

            var sourceGroup = AddGroup("Source");
            SourceEnum = sourceGroup.AddUIComponent<UISourceGroup>();

            var targetGroup = AddGroup("Target");
            TargetEnum = targetGroup.AddUIComponent<UITargetGroup>();

            UseSizeCheckbox = targetGroup.AddUIComponent<UICustomCheckBox>();
            UseSizeCheckbox.Text = "Use building size";
            UseSizeCheckbox.OnChanged += UseSizeChanged;

            var sizeGroup = targetGroup.AddUIComponent<UIHorizontalPanel>();
            sizeGroup.autoLayoutPadding = new RectOffset(0, 5, 0, 0);
            LengthSlider = AddSlider(sizeGroup, "Length");
            WidthSlider = AddSlider(sizeGroup, "Width");
            UseSizeChanged(null, false);

            var buttonPanel = AddUIComponent<UIVerticalPanel>();

            var deleteButton = buttonPanel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsButtonTemplate")) as UIButton;
            deleteButton.text = "Delete rule";
            deleteButton.eventClick += Delete;

            var endSpace = buttonPanel.AddUIComponent<UIPanel>();
            endSpace.height = padding.bottom;
            endSpace.isInteractive = false;
        }
        public void Delete(UIComponent component, UIMouseEventParameter eventParam)
        {
            (parent as UIRulePanel).RemoveRule(this);
            Destroy(gameObject);
        }
        public void UseSizeChanged(UIComponent component, bool use)
        {
            LengthSlider.isVisible = use;
            WidthSlider.isVisible = use;
        }

        public UIPanel AddGroup(string text)
        {
            var group = AddUIComponent<UIVerticalPanel>();
            group.autoLayoutPadding = new RectOffset(0, 0, 0, 5);
            //group.backgroundSprite = "GenericPanel";//"ContentManagerItemBackground";
            group.size = new Vector2(width - padding.horizontal, 0);

            var lable = group.AddUIComponent<UILabel>();
            lable.text = text;
            lable.textScale = 1.5f;

            var content = group.AddUIComponent<UIVerticalPanel>();
            content.autoLayoutPadding = new RectOffset(15, 0, 0, 5);
            //content.backgroundSprite = "GenericPanel";
            content.size = new Vector2(group.width - group.padding.horizontal, 0);

            return content;
        }
        public UISlider AddSlider(UIPanel parent, string title)
        {
            var panel = parent.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate")) as UIPanel;
            var slider = panel.Find<UISlider>("Slider");
            var label = panel.Find<UILabel>("Label");

            slider.eventValueChanged += (_, value) => label.text = $"{title} less then {value}";

            slider.minValue = 1;
            slider.maxValue = 20;
            slider.stepSize = 1;

            label.textScale = 1f;

            return slider;
        }
    }
    public class UISourceGroup : UIEnumGroup<SourceBuildings>
    {
        protected override SourceBuildings GetValue() => Options.Aggregate(default(SourceBuildings), (res, option) => res | (option.Value.IsChecked ? option.Key : default(SourceBuildings)));

        protected override void SetValue(SourceBuildings value)
        {
            foreach (var enumValue in Enum.GetValues(typeof(SourceBuildings)).OfType<SourceBuildings>())
            {
                Options[enumValue].IsChecked = (value & enumValue) != 0;
            }
        }
    }
    public class UITargetGroup : UIEnumGroup<TargetBuildings>
    {
        protected override TargetBuildings GetValue() => Options.Aggregate(default(TargetBuildings), (res, option) => res | (option.Value.IsChecked ? option.Key : default(TargetBuildings)));

        protected override void SetValue(TargetBuildings value)
        {
            foreach (var enumValue in Enum.GetValues(typeof(TargetBuildings)).OfType<TargetBuildings>())
            {
                Options[enumValue].IsChecked = (value & enumValue) != 0;
            }
        }
    }
    public abstract class UIEnumGroup<T> : UIHorizontalPanel where T : Enum
    {
        private T _value;
        public T Value
        {
            get => _value;
            set => SetValue(value);
        }

        protected abstract T GetValue();
        protected abstract void SetValue(T value);

        private UICustomCheckBox All { get; }
        protected Dictionary<T, UICustomCheckBox> Options { get; } = new Dictionary<T, UICustomCheckBox>();

        public event PropertyChangedEventHandler<T> OnChanged;

        public UIEnumGroup()
        {
            Debug.Log($"{nameof(UIEnumGroup<T>)}: Add enum group UI");

            autoLayoutPadding = new RectOffset(0, 10, 0, 0);
            //backgroundSprite = "GenericPanel";

            All = AddUIComponent<UICustomCheckBox>();
            All.Text = nameof(All);
            All.OnChanged += AllCheckChanged;

            foreach (var enumValue in Enum.GetValues(typeof(T)).OfType<T>())
            {
                var option = AddUIComponent<UICustomCheckBox>();
                option.Text = enumValue.ToString();
                option.OnChanged += CheckChanged;

                Options[enumValue] = option;
            }
        }

        protected virtual void AllCheckChanged(UIComponent component, bool isChecked)
        {
            foreach (var option in Options.Values)
            {
                option.OnChanged -= CheckChanged;
                option.IsChecked = isChecked;
                option.OnChanged += CheckChanged;
            }

            _value = GetValue();
            OnChanged?.Invoke(this, Value);
        }
        protected virtual void CheckChanged(UIComponent component, bool isChecked)
        {
            All.OnChanged -= AllCheckChanged;
            All.IsChecked = Options.Values.All(i => i.IsChecked);
            All.OnChanged += AllCheckChanged;

            _value = GetValue();
            OnChanged?.Invoke(this, Value);
        }
    }

    public class UIVerticalPanel : UIPanel
    {
        public UIVerticalPanel()
        {
            autoLayout = true;
            autoFitChildrenVertically = true;
            autoLayoutDirection = LayoutDirection.Vertical;
        }
    }
    public class UIHorizontalPanel : UIPanel
    {
        public UIHorizontalPanel()
        {
            autoLayout = true;
            autoFitChildrenVertically = true;
            autoFitChildrenHorizontally = true;
            autoLayoutDirection = LayoutDirection.Horizontal;
        }
    }
    public class UICustomCheckBox : UIHorizontalPanel
    {
        private UICheckBox CheckBox { get; }
        private UILabel Label { get; set; }

        public string Text
        {
            set
            {
                Label.text = value;
                CheckBox.size = new Vector2(20, Label.size.y);
            }
        }
        public bool IsChecked
        {
            get => CheckBox.isChecked;
            set => CheckBox.isChecked = value;
        }

        public UICustomCheckBox()
        {
            //backgroundSprite = "GenericPanel";

            CheckBox = AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsCheckBoxTemplate")) as UICheckBox;
            CheckBox.text = string.Empty;
            CheckBox.eventCheckChanged += (component, value) => OnChanged?.Invoke(this, value);

            Label = AddUIComponent<UILabel>();
            Label.textScale = 1f;

            Text = string.Empty;
        }

        public event PropertyChangedEventHandler<bool> OnChanged;
    }

    [Flags]
    public enum SourceBuildings
    {
        Industry = 1,
        Outside = 2,
        Warehouse = 4,
    }

    [Flags]
    public enum TargetBuildings
    {
        Low = 1,
        High = 2,
        Eco = 4,
        Leisure = 8,
        Tourist = 16,
    }
}
