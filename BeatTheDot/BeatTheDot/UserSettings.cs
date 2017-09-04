using System;
using System.Collections.Generic;
using System.Text;

using Plugin.Settings;
using Plugin.Settings.Abstractions;
using System.Xml.Serialization;
using System.IO;

using BeatTheDot.Models;

namespace BeatTheDot
{
    public static class UserSettings
    {
        private const string SettingsKey = "Default";
        private static readonly string SettingsDefault = string.Empty;

        private static ISettings AppSettings = CrossSettings.Current;

        public static Settings Load()
        {
            var serialized = AppSettings.GetValueOrDefault(SettingsKey, SettingsDefault);

            if (String.IsNullOrWhiteSpace(serialized))
            {
                return new Settings();
            }

            var serializer = new XmlSerializer(typeof(Settings));
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms))
                {
                    sw.Write(serialized);
                    sw.Flush();
                    ms.Position = 0;

                    return serializer.Deserialize(ms) as Settings;
                }
            }
        }

        public static void Save(Settings value)
        {
            var serializer = new XmlSerializer(typeof(Settings));
            using (var ms = new MemoryStream())
            {
                serializer.Serialize(ms, value);
                ms.Flush();
                ms.Position = 0;

                using (var sr = new StreamReader(ms))
                {
                    AppSettings.AddOrUpdateValue(SettingsKey, sr.ReadToEnd());
                }
            }
        }
    }
}
