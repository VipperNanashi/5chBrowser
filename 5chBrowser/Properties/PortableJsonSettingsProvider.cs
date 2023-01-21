using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace _5chBrowser.Properties
{
    public class PortableJsonSettingsProvider : SettingsProvider
    {
        public override string ApplicationName { get; set; }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
        {
            var settingsType = context["SettingsClassType"] as Type;
            var filePath = GetSettingFilePath(settingsType);

            var data = new JsonElement();
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                data = JsonSerializer.Deserialize<JsonElement>(json);
            }

            var col = new SettingsPropertyValueCollection();
            foreach (SettingsProperty prop in collection)
            {
                var val = new SettingsPropertyValue(prop);
                try
                {
                    val.SerializedValue = data.GetProperty(prop.Name).Deserialize(prop.PropertyType);
                }
                catch
                {
                    val.SerializedValue = prop.DefaultValue;
                }
                val.IsDirty = false;
                col.Add(val);
            }

            return col;
        }

        private string GetSettingFilePath(Type type)
        {
            var assembly = Assembly.GetEntryAssembly();
            var folderPath = Path.GetDirectoryName(assembly.Location);
            var filePath = Path.Combine(folderPath, type.Name + ".json");
            return filePath;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
        {
            var settingsType = context["SettingsClassType"] as Type;
            var filePath = GetSettingFilePath(settingsType);

            var settings = new Dictionary<string, object>();
            foreach (SettingsPropertyValue spv in collection)
                settings.Add(spv.Name, spv.SerializedValue);

            var json = ToJson(settings);
            File.WriteAllText(filePath, json);
        }

        private string ToJson(dynamic obj)
        {
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            });

            json = Regex.Replace(json, @"([^\\])\\u3000", "$1　");
            json = Regex.Replace(json, @"^\\u3000", "　");
            json = Regex.Replace(json, @"([^\\])\\u0022", "$1\\\"");
            json = Regex.Replace(json, @"^\\u0022", "\\\"");
            return json;
        }

        public override void Initialize(string pname, NameValueCollection config)
        {
            //設定プロバイダ名を指定する
            base.Initialize("PortableJsonSettingsProvider", config);
        }
    }
}
