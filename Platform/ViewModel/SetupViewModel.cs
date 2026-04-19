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
            Debug.WriteLine($"\n Num of ports :: {size}\n");
            for (int i = 0; i < size; i++)
            {
                serialConfigs.Add(configService.getSerialConfigFromIndex(i));
                Debug.WriteLine($"{i}th serial :: {serialConfigs[i].comPort}");
            }
        }
    }
}
