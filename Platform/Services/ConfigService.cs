using Platform.Model;
using Platform.Utils;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Services
{
    public interface IConfigService
    {
        void LoadConfig();
        void SaveConfig();
    }

    class ConfigService : IConfigService
    {
        private readonly ConfigModel configModel;

        public ConfigService(ConfigModel _configModel)
        {
            configModel = _configModel;
            InitConfig();
        }

        public void InitConfig() {
            DebugLogger.Log(3, "[DEBUG] Initializing Config...");

            // Create Plugin Directory to Load Model resources
            if (!Directory.Exists(configModel.pluginPath)) Directory.CreateDirectory(configModel.pluginPath);
            LoadConfig();
        }

        public void LoadConfig() {
            DebugLogger.Log(3, "[DEBUG] Loading Config file ... ");

            string path = configModel.configPath;
            string configFilePath = Path.Combine(path, "config.ini");

            if (!File.Exists(configFilePath))
            {
                DebugLogger.Log(2, "[WARN] Config.ini doesn't exist, Creating ... ");
                SaveConfig();
                return;
            }

            MapIniToConfig("Components", configFilePath);
        }
        public void SaveConfig() {
            string path = configModel.configPath;

            if (!Directory.Exists(path))
            {
                DebugLogger.Log(2, "[WARN] Config Directory doesn't exist, Creating ...");
                Directory.CreateDirectory(path);
            }
            string configFilePath = Path.Combine(path, "config.ini");

            MapConfigToIniFile("Components", configFilePath);
        }

        public void SetSerialConfig(List<SerialConfig> configs)
        {
            configModel.serialConfigs = configs;
        }

        private void MapConfigToIniFile(string section, string path) {
            PropertyInfo[] properties = configModel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (!property.CanWrite) continue;
                object val = property.GetValue(configModel);
                WritePrivateProfileString(section, property.Name, val?.ToString() ?? "", path);
            }

        }

        private void MapIniToConfig(string section, string path) {
            PropertyInfo[] properties = configModel.GetType().GetProperties(BindingFlags.Public);
            StringBuilder sb = new StringBuilder(255);

            foreach (var property in properties) {
                if (!property.CanWrite) continue;

                GetPrivateProfileString(section, property.Name, "", sb, 255, path);
                string value = sb.ToString();
                if (string.IsNullOrEmpty(value)) continue;

                try
                {
                    object convertedVal;
                    if (property.PropertyType.IsEnum)
                        convertedVal = Enum.Parse(property.PropertyType, value);
                    else
                        convertedVal = Convert.ChangeType(value, property.PropertyType);

                    property.SetValue(configModel, convertedVal);
                    
                }
                catch
                {
                    DebugLogger.Log(2, $"[WARN] Error while loading {property.Name} property. Load Default setting");
                }
            }
            DebugLogger.Log(3, "[DEBUG] Config Successfully Loaded !!");
        }

        [DllImport("Kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("Kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
    }
}
