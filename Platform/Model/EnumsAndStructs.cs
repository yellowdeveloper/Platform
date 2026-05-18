using PluginBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Model
{
    struct SerialConfig
    {
        public string comPort { get; set; }
        public int baudRate { get; set; }
        public int dataBits { get; set; }
        public int parity { get; set; }
        public int stopBits { get; set; }
        public bool dtrEnable { get; set; }
    };
}
