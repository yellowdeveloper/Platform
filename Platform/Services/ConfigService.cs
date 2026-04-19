using Platform.Model;
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
            Debug.WriteLine("\n* Initializing Config... *\n");

            // Create Plugin Directory to Load Model resources
            if (!Directory.Exists(configModel.pluginPath)) Directory.CreateDirectory(configModel.pluginPath);
            LoadConfig();
        }

        public void LoadConfig() {
            string path = configModel.configPath;
            string configFilePath = Path.Combine(path, "config.ini");

            if (!File.Exists(configFilePath))
            {
                SaveConfig();
                return;
            }

            MapIniToConfig("Components", configFilePath);
        }
        public void SaveConfig() {
            string path = configModel.configPath;

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            string configFilePath = Path.Combine(path, "config.ini");

            MapConfigToIniFile("Components", configFilePath);
        }

        public int getSerialConfigSize()
        {
            return configModel.serialConfigs.Count;
        }

        public SerialConfig getSerialConfigFromIndex(int idx)
        {
            if((configModel.serialConfigs.Count - 1) < idx)
            {
                // TODO REVEAL IDX ERROR
            }
            return configModel.serialConfigs[idx];
        }

        public void getAvailablePorts()
        {
            string[] port = System.IO.Ports.SerialPort.GetPortNames();

            foreach (string portName in port)
            {
                configModel.serialConfigs.Add(
                    new SerialConfig
                    {
                        comPort = portName,
                        baudRate = 115200,
                        dataBits = 8,
                        parity = 0,
                        stopBits = 0
                    });
            }
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
                catch { /* Default Setting */ }
            }
        }

        [DllImport("Kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("Kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
    }
}
