using Platform.Model;
using Platform.Utils;
using PluginBase;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Services
{
    internal class SerialService : ISerialService
    {
        private readonly Dictionary<int, SerialDevice> serialDevices = new();

        // Match Device ID With SerialConfig Index
        public bool Connect(int id, SerialConfig config)
        {
            if (serialDevices.ContainsKey(id))
            {
                DebugLogger.Log(2, $"[WARN] Port [NO.{id}] Already Connected.");
                return false;
            }

            var device = new SerialDevice(config);
            bool ret = device.Connect();

            if(ret) serialDevices[id] = device;

            return ret;
        }

        public void Disconnect(int id)
        {
            if (!serialDevices.ContainsKey(id))
                return;

            serialDevices[id].Disconnect();
            serialDevices.Remove(id);
        }

        public bool isSerialDeviceExists(int id)
        {
            return serialDevices.ContainsKey(id);
        }
        
        public string[] getAvailablePorts() {
            string[] port = System.IO.Ports.SerialPort.GetPortNames();
            return port;
        }

        public ISerialDevice GetDevice(int id)
        {
            if (serialDevices.ContainsKey(id))
                return serialDevices[id];

            return null;
        }
    }
}
