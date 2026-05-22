using FTD2XX_NET;
using Iot.Device.FtCommon;
using Platform.Model;
using PluginBase;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Services
{
    internal class SPIService : ISPIService
    {
        private readonly Dictionary<int, FTDIDevice> ftdiDevices = new();

        private ISerialDeviceHandler spiDeviceHandler;

        public bool Connect(int locID)
        {
            if (ftdiDevices.ContainsKey(locID))
            {
                DebugLogger.Log(2, $"[WARN] SPI {locID} Already Connected.");
                return false;
            }

            var device = new FTDIDevice(locID);
            bool ret = device.Connect(locID);

            if (ret) ftdiDevices[locID] = device;

            return ret;
        }

        public void Disconnect(int locID)
        {
            if (!ftdiDevices.ContainsKey(locID)) return;

            ftdiDevices[locID].Disconnect();
            ftdiDevices.Remove(locID);
        }

        public int[]? GetFTDIDeviceList()
        {
            FTDI ftdi = new FTDI();

            uint devCount = 0;
            ftdi.GetNumberOfDevices(ref devCount);

            if (devCount == 0)
            {
                DebugLogger.Log(2, $"[WARN] Device doesn't exists.");
                return null;
            }

            // Get FTDI Device List
            FTDI.FT_DEVICE_INFO_NODE[] deviceList = new FTDI.FT_DEVICE_INFO_NODE[devCount];
            ftdi.GetDeviceList(deviceList);

            int spiDevCount = 0;
            int[] locationIDs = new int[devCount];

            // Find FT232H or RS232-HS
            for (int i = 0; i < devCount; i++)
            {
                if (deviceList[i].Description.Contains("RS232-HS") || deviceList[i].Type == FTDI.FT_DEVICE.FT_DEVICE_232H)
                {
                    DebugLogger.Log(3, $"[DEBUG] Found SPI Device: {deviceList[i].Description} (LocID: {deviceList[i].LocId})");
                    locationIDs[spiDevCount] = (int)deviceList[i].LocId;
                    spiDevCount++;
                }
            }

            return locationIDs.Take(spiDevCount).ToArray();
        }
        public bool IsSPIDeviceExists(int id)
        {
            return ftdiDevices.ContainsKey(id);
        }

        public ISPIDevice GetDevice(int id)
        {
            if (ftdiDevices.ContainsKey(id))
                return ftdiDevices[id];

            return null;
        }

        public int[] GetDeviceList()
        {
            return ftdiDevices.Keys.Select(id => id).ToArray();
        }
    }
}
