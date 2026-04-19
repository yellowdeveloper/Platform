using Platform.Model;
using Platform.Services;
using Platform.Utils;
using PluginBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;


namespace Platform.ViewModel
{
    class MainViewModel: Notifier
    {
        public SetupViewModel setupViewModel { get; }
        public PluginViewModel pluginViewModel { get; }

        public object _pluginUI;
        public ISerial _pluginSerial;

        private readonly DispatcherTimer _dateTimer = new DispatcherTimer();

        private ConfigService configService;
        private PluginService pluginService;

        private string _displayTime;
        private int _pluginNum;

        public object pluginUI
        {
            get { return _pluginUI; }
            set { _pluginUI = value; OnPropertyChanged(); }
        }

        public string displayTime
        {
            get { return _displayTime; }
            set { _displayTime = value; OnPropertyChanged(); }
        }

        public int pluginNum
        {
            get { return _pluginNum; }
            set { _pluginNum = value; OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            var _configModel = new ConfigModel();

            var _configService = new ConfigService(_configModel);
            var _pluginService = new PluginService(_configModel);
            var _serialService = new SerialService();
            var _ftdiService = new FTDIService();


            configService = _configService;
            pluginService = _pluginService;

            setupViewModel = new SetupViewModel(configService);
            pluginViewModel = new PluginViewModel(configService);

            _dateTimer.Interval = TimeSpan.FromSeconds(1.0);
            _dateTimer.Tick += new EventHandler(_dateTimer_Tick);
            _dateTimer.Start();

            pluginService.setPluginNum(pluginService.loadPluginPathsAndReturnCount()); 
            pluginNum = pluginService.getPluginNum();
            
            if (pluginNum)
            {

            }
        }

        public void IncreaseComponentNum()
        {
            pluginService.setPluginNum(pluginNum += 1);
            pluginService.getPluginNum();
        }

        private void _dateTimer_Tick(object sender, EventArgs e)
        {
            displayTime = DateTime.Now.ToString("yyyy. MM. dd tt hh:mm:ss");
        }
    }
}
