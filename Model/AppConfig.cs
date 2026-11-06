using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Névjegyzék
{
    [XmlRoot("AppSettings")]
    public class AppSettingsData
    {
        [XmlElement("CurrentLanguageCode")]
        public string CurrentLanguageCode { get; set; } = "hu";

        [XmlArray("DataFiles")]
        [XmlArrayItem("FileName")]
        public List<string> FileNames { get; set; } = new()
        {
            "contacts.xml",
            "notes.xml",
            "groups.xml"
        };
    }

    public static class AppConfig
    {
        private const string DefaultConfigFileName = "appsettings.xml";

        public static string GetProjectRootDirectory()
        {
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
        }

        public static string GetConfigPath()
        {
            string projectRoot = GetProjectRootDirectory();
            string projectConfigPath = Path.Combine(projectRoot, DefaultConfigFileName);

            if (File.Exists(projectConfigPath))
            {
                return projectConfigPath;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultConfigFileName);
        }

        public static AppSettingsData CreateDefault()
        {
            return new AppSettingsData();
        }

        public static AppSettingsData Load()
        {
            string path = GetConfigPath();

            if (!File.Exists(path))
            {
                var defaultSettings = CreateDefault();
                Save(defaultSettings);
                return defaultSettings;
            }

            try
            {
                var serializer = new XmlSerializer(typeof(AppSettingsData));
                using var reader = new StreamReader(path);
                var settings = serializer.Deserialize(reader) as AppSettingsData;
                return settings ?? CreateDefault();
            }
            catch
            {
                var defaultSettings = CreateDefault();
                Save(defaultSettings);
                return defaultSettings;
            }
        }

        public static void Save(AppSettingsData settings)
        {
            if (settings == null)
            {
                settings = CreateDefault();
            }

            if (settings.FileNames == null || settings.FileNames.Count == 0)
            {
                settings.FileNames = new List<string> { "contacts.xml", "notes.xml", "groups.xml" };
            }

            string path = GetConfigPath();
            string? directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var serializer = new XmlSerializer(typeof(AppSettingsData));
            using var writer = new StreamWriter(path);
            serializer.Serialize(writer, settings);
        }

        public static void SaveLanguage(string languageCode)
        {
            var settings = Load();
            settings.CurrentLanguageCode = languageCode;
            Save(settings);
        }
    }
}