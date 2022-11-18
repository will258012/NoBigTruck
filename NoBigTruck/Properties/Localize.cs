namespace NoBigTruck
{
	public class Localize
	{
		public static System.Globalization.CultureInfo Culture {get; set;}
		public static ModsCommon.LocalizeManager LocaleManager {get;} = new ModsCommon.LocalizeManager("Localize", typeof(Localize).Assembly);

		/// <summary>
		/// Add new rule
		/// </summary>
		public static string AddNewRule => LocaleManager.GetString("AddNewRule", Culture);

		/// <summary>
		/// Check target building's size
		/// </summary>
		public static string CheckTargetSize => LocaleManager.GetString("CheckTargetSize", Culture);

		/// <summary>
		/// Max building's length
		/// </summary>
		public static string MaxLength => LocaleManager.GetString("MaxLength", Culture);

		/// <summary>
		/// Max building's width
		/// </summary>
		public static string MaxWidth => LocaleManager.GetString("MaxWidth", Culture);

		/// <summary>
		/// Big trucks dont deliver goods to stores
		/// </summary>
		public static string Mod_Description => LocaleManager.GetString("Mod_Description", Culture);

		/// <summary>
		/// [NEW] Added missing dependencies checker.
		/// </summary>
		public static string Mod_WhatsNewMessage1_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_1", Culture);

		/// <summary>
		/// [FIXED] Fixed errors that caused the mod to not work.
		/// </summary>
		public static string Mod_WhatsNewMessage1_2 => LocaleManager.GetString("Mod_WhatsNewMessage1_2", Culture);

		/// <summary>
		/// [TRANSLATION] Added Spanish translations.
		/// </summary>
		public static string Mod_WhatsNewMessage1_2_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_2_1", Culture);

		/// <summary>
		/// [TRANSLATION] Added Korean translation.
		/// </summary>
		public static string Mod_WhatsNewMessage1_2_2 => LocaleManager.GetString("Mod_WhatsNewMessage1_2_2", Culture);

		/// <summary>
		/// [TRANSLATION] Added Hungarian translation.
		/// </summary>
		public static string Mod_WhatsNewMessage1_2_3 => LocaleManager.GetString("Mod_WhatsNewMessage1_2_3", Culture);

		/// <summary>
		/// [TRANSLATION] Added Danish, Portuguese and Turkish translations
		/// </summary>
		public static string Mod_WhatsNewMessage1_2_4 => LocaleManager.GetString("Mod_WhatsNewMessage1_2_4", Culture);

		/// <summary>
		/// [UPDATED] Added Plazas & Promenades DLC support.
		/// </summary>
		public static string Mod_WhatsNewMessage1_3 => LocaleManager.GetString("Mod_WhatsNewMessage1_3", Culture);

		/// <summary>
		/// Rules
		/// </summary>
		public static string RulesTab => LocaleManager.GetString("RulesTab", Culture);

		/// <summary>
		/// Source building's type
		/// </summary>
		public static string Source => LocaleManager.GetString("Source", Culture);

		/// <summary>
		/// Industry
		/// </summary>
		public static string SourceIndustry => LocaleManager.GetString("SourceIndustry", Culture);

		/// <summary>
		/// Outside
		/// </summary>
		public static string SourceOutside => LocaleManager.GetString("SourceOutside", Culture);

		/// <summary>
		/// Warehouse
		/// </summary>
		public static string SourceWarehouse => LocaleManager.GetString("SourceWarehouse", Culture);

		/// <summary>
		/// Target building's type
		/// </summary>
		public static string Target => LocaleManager.GetString("Target", Culture);

		/// <summary>
		/// Eco
		/// </summary>
		public static string TargetEco => LocaleManager.GetString("TargetEco", Culture);

		/// <summary>
		/// High
		/// </summary>
		public static string TargetHigh => LocaleManager.GetString("TargetHigh", Culture);

		/// <summary>
		/// Leisure
		/// </summary>
		public static string TargetLeisure => LocaleManager.GetString("TargetLeisure", Culture);

		/// <summary>
		/// Low
		/// </summary>
		public static string TargetLow => LocaleManager.GetString("TargetLow", Culture);

		/// <summary>
		/// Tourist
		/// </summary>
		public static string TargetTourist => LocaleManager.GetString("TargetTourist", Culture);
	}
}