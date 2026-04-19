using Platform.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Services
{
    internal class FTDIService
    {
        private readonly Dictionary<string, FTDIDevice> ftdiDevices = new();

        public void Connect(string id)
        {
            if (ftdiDevices.ContainsKey(id)) return;

            var device = new FTDIDevice(id);
            device.Connect();

            ftdiDevices[id] = device;
        }

        public void Disconnect(string id)
        {
            if (!ftdiDevices.ContainsKey(id)) return;

            ftdiDevices[id].Disconnect();
            ftdiDevices.Remove(id);
        }
    }
}
