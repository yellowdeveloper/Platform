using FTD2XX_NET;
using Iot.Device.FtCommon;
using Platform.Model;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Services
{
    internal class SPIService
    {
        private readonly Dictionary<int, FTDIDevice> ftdiDevices = new();

        public void Connect(int id)
        {
            if (ftdiDevices.ContainsKey(id)) return;

            var device = new FTDIDevice(id);
            device.Connect();

            ftdiDevices[id] = device;

            //if (ftdiDevices.ContainsKey(id))
            //{
            //    DebugLogger.Log(2, $"[WARN] Port [NO.{id}] Already Connected.");
            //    return false;
            //}

            //var device = new FTDIDevice(id);
            //bool ret = device.Connect();

            //if (ret) serialDevices[id] = device;

            //return ret;
        }

        public void Disconnect(int id)
        {
            if (!ftdiDevices.ContainsKey(id)) return;

            ftdiDevices[id].Disconnect();
            ftdiDevices.Remove(id);
        }

        public int getAvailablePorts()
        {
            int deviceCount = 0;

            ListFTDIDevices();
            deviceCount = ftdiDevices.Count;

            return deviceCount;
        }

        public void ListFTDIDevices()
        {
            int id   = 0;

            while (true)
            {
                FTDIDevice newDevice = new FTDIDevice(id);
                uint deviceCount = newDevice.searchDevice();

                if ((deviceCount <= 0) || (ftdiDevices.Count) >= deviceCount)
                {
                    newDevice.Dispose();

                    int disconnectedDevices = (int)(ftdiDevices.Count - deviceCount);

                    if (disconnectedDevices <= 0) break;

                    DebugLogger.Log(3, $"[DEBUG] Disconnected FTDI device found!, Re-Search all Devices");

                    foreach (var device in ftdiDevices.Values)
                    {
                        device.Dispose();
                    }
                    ftdiDevices.Clear();

                    ListFTDIDevices();
                    return;
                }

                DebugLogger.Log(3, $"[DEBUG] {id + 1} / {deviceCount} FTDI device found!");
                ftdiDevices[id] = newDevice;
                id++;
            }
        }
        public bool isSPIDeviceExists(int id)
        {
            return ftdiDevices.ContainsKey(id);
        }
    }
}
