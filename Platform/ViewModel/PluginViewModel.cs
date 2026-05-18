using Platform.Model;
using Platform.Services;
using PluginBase.CommonUtils;
using Platform.Utils;
using PluginBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.ViewModel
{
    internal class PluginViewModel : Notifier
    {
        private readonly ConfigService configService;

        public PluginViewModel(ConfigService _configService)
        {
            configService = _configService;
            PluginInit();
        }

        private void PluginInit()
        {
            
        }
    }
}
