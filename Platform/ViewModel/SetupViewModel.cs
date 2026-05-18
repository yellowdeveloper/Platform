using OpenCvSharp;
using Platform.Model;
using Platform.Services;
using Platform.Utils;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Device.Spi;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Platform.ViewModel
{
    internal class SetupViewModel :Notifier
    {
        private readonly CommunicationFacade communicationFacade;
        private readonly SerialService serialService;
        private readonly SPIService spiService;

        private List<SerialConfig> serialConfigs = new List<SerialConfig>();
        private Dictionary<int, string> spiDevices;
        private List<string> FTDIDevices = new List<string>();

        private int _serialIndex = 0;
        private int _spiIndex = 0;
        // true = Connected, false = Disconnected
        private bool _serialStat;
        private bool _spiStat;

        public ICommand SerialConnectionButtonCommand { get; }
        private ICommand SPIConnectCommand { get; }

        public bool serialStat
        {
            get => _serialStat;
            set
            {
                _serialStat = value;
                OnPropertyChanged();
            }
        }

        public bool spiStat
        {
            get => _spiStat;
            set
            {
                _spiStat = value;
                OnPropertyChanged();
            }
        }

        public int serialIndex
        {
            get => _serialIndex;
            set
            {
                _serialIndex = value;
                OnPropertyChanged();
            }
        }

        public int spiIndex
        {
            get => _spiIndex;
            set
            {
                _spiIndex = value;
                OnPropertyChanged();
            }
        }

        public SerialConfig serialConfig
        {
            get { return serialConfigs[serialIndex]; }
            set { serialConfigs[serialIndex] = value; OnPropertyChanged(); }
        }
        public string spiDevice
        {
            get { return spiDevices[spiIndex]; }
            set { spiDevices[spiIndex] = value; OnPropertyChanged(); }
        }

        public SetupViewModel(CommunicationFacade _communicationFacade, SerialService _serialService, SPIService _spiService)
        {
            communicationFacade = _communicationFacade;
            serialService = _serialService;
            spiService = _spiService;

            Init();

            SerialConnectionButtonCommand = new RelayCommand(param => {
                
                if (!serialStat)
                    serialStat = serialService.Connect(serialIndex, serialConfigs[serialIndex]);
                else
                {
                    serialService.Disconnect(serialIndex);
                    serialStat = false;
                }
            });
        }

        // Get Available Serial Ports and Set to List
        // Called when SetupWindow Pop-Up
        public void Init()
        {
            // Clear serial config list
            serialConfigs.Clear();
            serialConfigs = communicationFacade.InitializeSerialSettings();

            // Clear spi device index list
            spiDevices = communicationFacade.InitializeSPIDevices();

            // Codes For Debug :: You can comment out 3 lines after this comment
            int size = serialConfigs.Count();
            DebugLogger.Log(3, $"[DEBUG] Num of ports :: {size}");
            for (int i = 0; i < size; i++) DebugLogger.Log(3, $"[DEBUG] {i}th serial :: {serialConfigs[i].comPort}");
        }

        public void IncreaseSerialIndex()
        {
            if (serialIndex < serialConfigs.Count - 1) ++serialIndex;
            else serialIndex = 0;

            serialStat = serialService.isSerialDeviceExists(serialIndex);

            OnPropertyChanged(nameof(serialConfig));
            DebugLogger.Log(3, $"[DEBUG] Now Pointing Serial {serialConfigs[serialIndex].comPort}, {serialIndex}");
        }

        public void DecreaseSerialIndex()
        {
            if (serialIndex > 0) serialIndex--;
            else serialIndex = serialConfigs.Count - 1;

            serialStat = serialService.isSerialDeviceExists(serialIndex);

            OnPropertyChanged(nameof(serialConfig));
            DebugLogger.Log(3, $"[DEBUG] Now Pointing Serial {serialConfigs[serialIndex].comPort}, {serialIndex}");
        }

        public void IncreaseSPIIndex()
        {
            if (spiIndex < spiDevices.Count - 1) ++spiIndex;
            else spiIndex = 0;

            spiStat = spiService.isSPIDeviceExists(spiIndex);

            OnPropertyChanged(nameof(spiDevice));
            DebugLogger.Log(3, $"[DEBUG] Now Pointing SPI Device {spiIndex}");
        }

        public void DecreaseSPIIndex()
        {
            if (spiIndex > 0) spiIndex--;
            else spiIndex = spiDevices.Count - 1;

            spiStat = spiService.isSPIDeviceExists(spiIndex);

            OnPropertyChanged(nameof(spiDevice));
            DebugLogger.Log(3, $"[DEBUG] Now Pointing SPI Device {spiIndex}");
        }
    }
}
