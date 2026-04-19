using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Model
{
    class ConfigModel
    {
        public int pluginNum { get; set; }
        public List<SerialConfig> serialConfigs { get; set; } = new List<SerialConfig>();
        public Dictionary<int, string> pluginPaths { get; set; } = new Dictionary<int, string>();
        public string configPath { get; } = @"Config\";
        public string pluginPath { get; } = @"Plugins\";

    }
}
