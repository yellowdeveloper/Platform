using OpenCvSharp;
using Platform.Model;
using Platform.Services;
using System;
using System.Collections.Generic;
using Platform.Utils;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.ViewModel
{
    internal class SetupViewModel :Notifier
    {
        private readonly ConfigService configService;

        private List<SerialConfig> serialConfigs = new List<SerialConfig>();
        private List<string> FTDIDevices = new List<string>();

        public SerialConfig serialConfig
        {
            get { return serialConfigs[0]; }
            set { serialConfigs[0] = value; OnPropertyChanged(); }
        }
        public SetupViewModel(ConfigService _configService)
        {
            configService = _configService;
            SerialConfigInit();
        }

        // Get Available Serial Ports and Set to List
        // Called when SetupWindow Pop-Up
        void SerialConfigInit()
        {
            // Clear serial config list
            serialConfigs.Clear();

            configService.getAvailablePorts();

            int size = configService.getSerialConfigSize();
            DebugLogger.Log(3, $"[DEBUG] Num of ports :: {size}");
            for (int i = 0; i < size; i++)
            {
                serialConfigs.Add(configService.getSerialConfigFromIndex(i));
                DebugLogger.Log(3, $"[DEBUG] {i}th serial :: {serialConfigs[i].comPort}");
            }
        }

        void FTDIInit()
        {
            // Clear serial config list
            serialConfigs.Clear();

            configService.getAvailablePorts();

            int size = configService.getSerialConfigSize();
            DebugLogger.Log(3, $"[DEBUG] Num of ports :: {size}");
            for (int i = 0; i < size; i++)
            {
                serialConfigs.Add(configService.getSerialConfigFromIndex(i));
                DebugLogger.Log(3, $"[DEBUG] {i}th serial :: {serialConfigs[i].comPort}");
            }
        }
    }
}
