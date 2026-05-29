using OpenCvSharp;
using PluginBase;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnemiaPrediction
{
    public interface IISPIDeviceHandler : ISPIDeviceHandler
    {
        int[] GetSPIDeviceList();
        Task SendImage(Mat imageData);
    }
    internal class SPIDeviceHandler: IISPIDeviceHandler
    {
        private ISPIDevice spiDevice;
        private ISPIService spiService;

        private readonly Settings settings;

        public SPIDeviceHandler(ISPIService _spiService, Settings _settings)
        {
            settings = _settings;
            spiService = _spiService;
        }

        public void SPIConnect(int id)
        {
            spiDevice = spiService.GetDevice(id);

            if (spiDevice == null)
            {
                DebugLogger.Log(1, $"[ERROR] SPI Device doesn't exists.");
                return;
            }
            DebugLogger.Log(3, $"[DEBUG] SPI Device No.{id} Plugin Connection Operated ...");

            spiDevice.SetDeviceHandler(this);
            spiDevice.AttachSPIEventHandler();
        }

        public void SPIDisconnect()
        {
            if (spiDevice == null) return;
            DebugLogger.Log(3, $"[DEBUG] Disconnect with SPI device in IR Detection");

            spiDevice.SetDeviceHandler(null);
            spiDevice.DetachSPIEventHandler();
        }

        public Task SendImage(Mat imageData)
        {
            return Task.Run(() =>
            {
                if (spiDevice == null)
                {
                    DebugLogger.Log(1, $"[ERROR] SPI Device is not connected.");
                }

                Packet spiPacket = new Packet();

                spiPacket.DataLength = imageData.Height * imageData.Width * 3;
                spiPacket.Data = new byte[spiPacket.DataLength];

                System.Runtime.InteropServices.Marshal.Copy(imageData.Data, spiPacket.Data, 0, spiPacket.DataLength);

                spiDevice.Send(spiPacket, 256);
            });
        }

        public int[] GetSPIDeviceList()
        {
            return spiService.GetDeviceList();
        }
    }
}
