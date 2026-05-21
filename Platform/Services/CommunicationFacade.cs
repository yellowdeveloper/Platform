using OpenCvSharp.Aruco;
using Platform.Model;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Services
{
    internal class CommunicationFacade
    {
        private readonly SerialService serialService;
        private readonly SPIService    spiService;
        private readonly ConfigService configService;

        public CommunicationFacade(SerialService s, SPIService p, ConfigService c)
        {
            serialService = s;
            spiService    = p;
            configService = c;
        }

        public List<SerialConfig> InitializeSerialSettings()
        {
            string[] ports = serialService.getAvailablePorts();

            List<SerialConfig> configs = ports.Select(port =>
                new SerialConfig
                {
                    comPort = port,
                    baudRate = 115200,
                    dataBits = 8,
                    parity = 0,
                    stopBits = 1,
                    dtrEnable = true
                }
            ).ToList();

            configService.SetSerialConfig(configs);

            return configs;
        }

        public int[] InitializeSPIDevices()
        {
            int[] devices = spiService.GetFTDIDeviceList();

            if (devices == null)
                return Array.Empty<int>();

            return devices;
        }
    }
}
