using Platform.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Services
{
    internal class SerialService
    {
        private readonly Dictionary<int, SerialDevice> serialDevices = new();

        // Match Device ID With SerialConfig Index
        public void Connect(int id, SerialConfig config)
        {
            if (serialDevices.ContainsKey(id)) return;

            var device = new SerialDevice(id, config);
            device.Connect();

            serialDevices[id] = device;
        }

        public void Disconnect(int id)
        {
            if (!serialDevices.ContainsKey(id)) return;

            serialDevices[id].Disconnect();
            serialDevices.Remove(id);
        }
    }
}
