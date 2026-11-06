using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Névjegyzék
{
    public static class LanguageManager
    {
        public static readonly Dictionary<string, string> SupportedLanguages = new()
        {
            ["hu"] = "Magyar",
            ["en"] = "English"
        };

        public static string CurrentLanguageCode { get; private set; } = LoadPersistedLanguage();
        public static event Action LanguageChanged;

        public static void ApplyLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                languageCode = "hu";
            }

            var normalizedCode = SupportedLanguages.ContainsKey(languageCode)
                ? languageCode
                : "hu";

            if (CurrentLanguageCode == normalizedCode)
            {
                UpdateCulture(normalizedCode);
                return;
            }

            CurrentLanguageCode = normalizedCode;
            AppConfig.SaveLanguage(normalizedCode);
            UpdateCulture(normalizedCode);
            LanguageChanged?.Invoke();
        }

        public static void InitializeLanguage()
        {
            var persistedLanguageCode = LoadPersistedLanguage();
            ApplyLanguage(persistedLanguageCode);
        }

        private static void UpdateCulture(string languageCode)
        {
            var app = Application.Current;
            if (app == null)
            {
                return;
            }

            var uri = new Uri($"pack://application:,,,/Resources/Localization/Strings.{languageCode}.xaml");
            var dictionary = new ResourceDictionary { Source = uri };

            var existing = app.Resources.MergedDictionaries
                .Where(x => x.Source != null && x.Source.OriginalString.Contains("Strings."))
                .ToList();

            foreach (var item in existing)
            {
                app.Resources.MergedDictionaries.Remove(item);
            }

            app.Resources.MergedDictionaries.Add(dictionary);

            CultureInfo.CurrentCulture = new CultureInfo(languageCode);
            CultureInfo.CurrentUICulture = new CultureInfo(languageCode);
        }

        private static string LoadPersistedLanguage()
        {
            try
            {
                var settings = AppConfig.Load();
                return SupportedLanguages.ContainsKey(settings.CurrentLanguageCode)
                    ? settings.CurrentLanguageCode
                    : "hu";
            }
            catch
            {
                return "hu";
            }
        }
    }
}