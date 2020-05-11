using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using ColossalFramework.IO;
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
        public static string ConfigFile => Path.Combine(DataLocation.localApplicationData, $"{nameof(NoBigTruck)}.xml");
        static List<Rule> Rules { get; set; }
        static UIOptionsPanel UIPanel { get; set; }

        private static bool IsLoaded { get; set; } = false;

        public static bool Check(ushort sourceBuildingId, ushort targetBuildingId)
        {
            var sourceBuildingInfo = GetBuildingInfo(sourceBuildingId);
            var targetBuildingInfo = GetBuildingInfo(targetBuildingId);

            var sourceType = GetSourceBuildings(sourceBuildingInfo);
            var targetType = GetTargetBuildings(targetBuildingInfo);

            var buildingLength = targetBuildingInfo.m_cellLength;
            var buildingWidth = targetBuildingInfo.m_cellWidth;

            var result = sourceType != 0 && targetType != 0 && Rules.Any(r => (r.Source & sourceType) != 0 && (r.Target & targetType) != 0 && (!r.UseSize || (buildingLength <= r.MaxLength && buildingWidth <= r.MaxWidth)));

            Logger.LogDebug(() => $"{nameof(Check)}: {nameof(sourceType)}={sourceType}; {nameof(targetType)}={targetType}; {nameof(buildingLength)}={buildingLength}; {nameof(buildingWidth)}={buildingWidth}; {nameof(result)}={result};");

            return result;
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


        public static void Init()
        {
            Logger.LogDebug(() => nameof(Init));

            Load();
        }
        static void Load()
        {
            Logger.LogInfo(() => $"Begin load config");

            try
            {
                if (IsLoaded)
                {
                    Logger.LogInfo(() => $"Config has already been loaded");
                    return;
                }

                Rules = new List<Rule>();

                if (!File.Exists(ConfigFile))
                {
                    Logger.LogInfo(() => $"Config file not exist, create default");
                    AddRule(new Rule());
                    Save();
                }
                else
                {
                    using (FileStream stream = new FileStream(ConfigFile, FileMode.Open))
                    {
                        var xdoc = new XmlDocument();
                        xdoc.Load(stream);

                        Logger.EnableDebug = bool.TryParse(xdoc.DocumentElement.GetAttribute(nameof(Logger.EnableDebug)), out bool tempEnableDebug) ? tempEnableDebug : false;

                        foreach (var element in xdoc.DocumentElement.ChildNodes.OfType<XmlElement>().Where(i => i.Name == nameof(Rule)))
                        {
                            var rule = new Rule()
                            {
                                Source = int.TryParse(element.GetAttribute(nameof(Rule.Source)), out int tempSource) ? (SourceBuildings)tempSource : Rule.DefaultSource,
                                Target = int.TryParse(element.GetAttribute(nameof(Rule.Target)), out int tempTarget) ? (TargetBuildings)tempTarget : Rule.DefaultTarget,
                                UseSize = bool.TryParse(element.GetAttribute(nameof(Rule.UseSize)), out bool tempUseSize) ? tempUseSize : Rule.DefaultUseSize,
                                MaxLength = int.TryParse(element.GetAttribute(nameof(Rule.MaxLength)), out int tempMaxLength) ? tempMaxLength : Rule.DefaultMaxLength,
                                MaxWidth = int.TryParse(element.GetAttribute(nameof(Rule.MaxWidth)), out int tempMaxWidth) ? tempMaxWidth : Rule.DefaultMaxWidth,
                            };

                            AddRule(rule);
                        }
                    }
                }

                IsLoaded = true;

                Logger.LogInfo(() => $"Config loaded: {Rules.Count} rules");
            }
            catch (Exception error)
            {
                Logger.LogError(() => $"Cant load config", error);
            }
        }
        static void Save()
        {
            Logger.LogInfo(() => $"Begin save config");

            try
            {
                if (!IsLoaded)
                {
                    Logger.LogInfo(() => $"Config has not been loaded yet");
                    return;
                }

                var xdoc = new XmlDocument();

                var xmlDeclaration = xdoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xdoc.AppendChild(xmlDeclaration);

                var xroot = xdoc.CreateElement("Config");
                AddAttr(xroot, nameof(Logger.EnableDebug), Logger.EnableDebug.ToString());

                foreach (var rule in Rules)
                {
                    var xrule = xdoc.CreateElement(nameof(Rule));

                    AddAttr(xrule, nameof(Rule.Source), ((int)rule.Source).ToString());
                    AddAttr(xrule, nameof(Rule.Target), ((int)rule.Target).ToString());
                    AddAttr(xrule, nameof(Rule.UseSize), rule.UseSize.ToString());
                    AddAttr(xrule, nameof(Rule.MaxLength), rule.MaxLength.ToString());
                    AddAttr(xrule, nameof(Rule.MaxWidth), rule.MaxWidth.ToString());

                    xroot.AppendChild(xrule);
                }

                xdoc.AppendChild(xroot);

                using (FileStream stream = new FileStream(ConfigFile, FileMode.Create))
                {
                    xdoc.Save(stream);
                }

                Logger.LogInfo(() => $"Config saved");

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
                Logger.LogError(() => $"Cant save config", error);
            }
        }

        public static void GetUI(UIScrollablePanel parentPanel)
        {
            Logger.LogDebug(() => nameof(GetUI));

            UIPanel = parentPanel.AddUIComponent<UIOptionsPanel>();
            UIPanel.Init();

            foreach (var rule in Rules)
                UIPanel.AddUIRule(rule);
        }

        public static void AddRule(Rule rule)
        {
            Logger.LogDebug(() => $"{nameof(AddRule)}: {rule}");

            Rules.Add(rule);
            rule.OnChanged += RuleOnChanged;

            if(IsLoaded)
                Save();
        }
        public static void DeleteRule(Rule rule)
        {
            Logger.LogDebug(() => $"{nameof(DeleteRule)}: {rule}");

            Rules.Remove(rule);
            rule.OnChanged -= RuleOnChanged;

            if (IsLoaded)
                Save();
        }

        private static void RuleOnChanged(Rule rule) => Save();
    }
    public class Rule
    {
        public static SourceBuildings DefaultSource { get; } = SourceBuildings.Industry | SourceBuildings.Outside | SourceBuildings.Warehouse;
        public static TargetBuildings DefaultTarget { get; } = TargetBuildings.Low | TargetBuildings.High | TargetBuildings.Eco | TargetBuildings.Leisure | TargetBuildings.Tourist;
        public static bool DefaultUseSize => false;
        public static int DefaultMaxLength => 4;
        public static int DefaultMaxWidth => 4;


        private SourceBuildings _source = DefaultSource;
        private TargetBuildings _target = DefaultTarget;
        private bool _useSize = DefaultUseSize;
        private int _maxLength = DefaultMaxLength;
        private int _maxWidth = DefaultMaxWidth;

        public SourceBuildings Source
        {
            get => _source;
            set => Set(ref _source, value);
        }
        public TargetBuildings Target
        {
            get => _target;
            set => Set(ref _target, value);
        }
        public bool UseSize
        {
            get => _useSize;
            set => Set(ref _useSize, value);
        }
        public int MaxLength
        {
            get => _maxLength;
            set => Set(ref _maxLength, value);
        }
        public int MaxWidth
        {
            get => _maxWidth;
            set => Set(ref _maxWidth, value);
        }

        public event Action<Rule> OnChanged;

        private void Set<T>(ref T value, T newValue)
        {
            if (!newValue.Equals(value))
            {
                value = newValue;
                OnChanged?.Invoke(this);
            }
        }

        public override string ToString() => $"{nameof(Source)}={Source}; {nameof(Target)}={Target}; {nameof(UseSize)}={UseSize}; {nameof(MaxLength)}={MaxLength}; {nameof(MaxWidth)}={MaxWidth};";
    }

    public class UIOptionsPanel : UIVerticalPanel
    {
        public UIVerticalPanel RulePanel { get; private set; }
        public UIButton AddButton { get; private set; }

        public void Init()
        {
            var parent = this.parent as UIScrollablePanel;
            size = new Vector2(parent.width - 2 * parent.scrollPadding.horizontal, parent.height);
            autoLayoutPadding = new RectOffset(0, 0, 0, 10);

            RulePanel = AddUIComponent<UIVerticalPanel>();
            RulePanel.autoLayoutPadding = new RectOffset(0, 0, 0, 10);
            RulePanel.size = size;

            AddButton = AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsButtonTemplate")) as UIButton;
            AddButton.text = "Add new rule";
            AddButton.eventClick += AddButtonClick;
        }

        private void AddButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            Logger.LogDebug(() => nameof(AddButtonClick));

            var rule = new Rule();
            Options.AddRule(rule);

            AddUIRule(rule);
        }

        public void AddUIRule(Rule rule)
        {
            Logger.LogDebug(() => nameof(AddUIRule));

            var uiRule = RulePanel.AddUIComponent<UIRule>();
            uiRule.Init(rule);

            uiRule.OnDelete += DeleteUIRule;
        }

        private void DeleteUIRule(UIRule uiRule)
        {
            Logger.LogDebug(() => nameof(DeleteUIRule));

            Options.DeleteRule(uiRule.Rule);

            RulePanel.RemoveUIComponent(uiRule);
            Destroy(uiRule.gameObject);
        }
    }
    public class UIRule : UIVerticalPanel
    {
        public Rule Rule { get; private set; }

        public UISourceGroup Source { get; private set; }
        public UITargetGroup Target { get; private set; }
        public UICustomCheckBox UseSize { get; private set; }
        public UICustomSlider MaxLength { get; private set; }
        public UICustomSlider MaxWidth { get; private set; }

        public event Action<UIRule> OnDelete;

        public void Init(Rule rule)
        {
            Rule = rule;

            backgroundSprite = "GenericPanel";

            autoLayoutPadding = new RectOffset(0, 0, 0, 15);
            padding = new RectOffset(5, 5, 5, 5);
            size = new Vector2(parent.width, 0);


            var sourceGroup = AddGroup("Source");
            Source = sourceGroup.AddUIComponent<UISourceGroup>();
            Source.Value = Rule.Source;
            Source.OnChanged += SourceOnChanged;


            var targetGroup = AddGroup("Target");
            Target = targetGroup.AddUIComponent<UITargetGroup>();
            Target.Value = Rule.Target;
            Target.OnChanged += TargetOnChanged;


            UseSize = targetGroup.AddUIComponent<UICustomCheckBox>();
            UseSize.Text = "Use building size";
            UseSize.IsChecked = Rule.UseSize;
            UseSize.OnChanged += UseSizeChanged;


            var sizeGroup = targetGroup.AddUIComponent<UIHorizontalPanel>();
            sizeGroup.autoLayoutPadding = new RectOffset(0, 5, 0, 0);

            MaxLength = AddSlider(sizeGroup, "length");
            MaxLength.Value = Rule.MaxLength;
            MaxLength.OnChanged += MaxLengthOnChanged;

            MaxWidth = AddSlider(sizeGroup, "width");
            MaxWidth.Value = Rule.MaxWidth;
            MaxWidth.OnChanged += MaxWidthOnChanged;

            SetVisible(Rule.UseSize);


            var buttonPanel = AddUIComponent<UIVerticalPanel>();

            var deleteButton = buttonPanel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsButtonTemplate")) as UIButton;
            deleteButton.text = "Delete rule";
            deleteButton.textScale = 1f;
            deleteButton.eventClick += Delete;

            var endSpace = buttonPanel.AddUIComponent<UIPanel>();
            endSpace.height = padding.bottom;
            endSpace.isInteractive = false;
        }

        private void SourceOnChanged(UIComponent component, SourceBuildings value) => Rule.Source = value;
        private void TargetOnChanged(UIComponent component, TargetBuildings value) => Rule.Target = value;
        public void UseSizeChanged(UIComponent component, bool value)
        {
            Rule.UseSize = value;
            SetVisible(value);
        }
        public void SetVisible(bool visible)
        {
            MaxLength.isVisible = visible;
            MaxWidth.isVisible = visible;
        }
        private void MaxLengthOnChanged(UIComponent component, float value) => Rule.MaxLength = (int)value;
        private void MaxWidthOnChanged(UIComponent component, float value) => Rule.MaxWidth = (int)value;


        public void Delete(UIComponent component, UIMouseEventParameter eventParam) => OnDelete?.Invoke(this);

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
        public UICustomSlider AddSlider(UIPanel parent, string title)
        {
            var slider = parent.AddUIComponent<UICustomSlider>();

            slider.Min = 1;
            slider.Max = 20;
            slider.Step = 1;

            slider.OnChanged += (_, value) => slider.Text = $"Max {title}: {value}";

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
    public class UICustomSlider : UIHorizontalPanel
    {
        private UISlider Slider { get; }
        private UILabel Label { get; set; }

        public float Value
        {
            get => Slider.value;
            set => Slider.value = value;
        }
        public float Min
        {
            get => Slider.minValue;
            set => Slider.minValue = value;
        }
        public float Max
        {
            get => Slider.maxValue;
            set => Slider.maxValue = value;
        }
        public float Step
        {
            get => Slider.stepSize;
            set => Slider.stepSize = value;
        }
        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }

        public event PropertyChangedEventHandler<float> OnChanged;

        public UICustomSlider()
        {
            var panel = AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate")) as UIPanel;

            Slider = panel.Find<UISlider>("Slider");
            Label = panel.Find<UILabel>("Label");
            Label.textScale = 1f;

            Slider.eventValueChanged += (component, value) => OnChanged?.Invoke(this, value);
        }
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
