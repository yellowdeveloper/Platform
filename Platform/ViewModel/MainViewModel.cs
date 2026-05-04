using Platform.Model;
using Platform.Services;
using Platform.Utils;
using PluginBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private int lastPluginIndex;
        public ObservableCollection<PluginComponent> plugins { get; } = new ObservableCollection<PluginComponent>();

        private readonly DispatcherTimer _dateTimer = new DispatcherTimer();

        private ConfigService configService;
        private PluginService pluginService;

        private string _displayTime;
        private int _pluginNum;

        ICommand CreateNewComponentCommand;

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

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            pluginService.setPluginNum(pluginService.loadPluginPathsAndReturnCount());
            pluginNum = pluginService.getPluginNum();
            DebugLogger.Log(3, $"[DEBUG] number of plugins : {pluginNum}");

            if (pluginNum > 0)
            {
                for (int pluginIndex = 0; pluginIndex < pluginNum; pluginIndex++)
                {
                    string pluginName = pluginService.GetPluginNameFromIndex(pluginIndex);
                    CreateNewComponent(pluginIndex, pluginName);
                    DebugLogger.Log(3, $"[DEBUG] Plugin [No.{pluginIndex} - {pluginName}]");
                }
            }
        }

        public void IncreaseComponentNum()
        {
            pluginService.setPluginNum(pluginNum += 1);
            pluginService.getPluginNum();
        }

        public void CreateNewComponent(int _pluginIndex, string _pluginName = "New")
        {
            plugins.Add(new PluginComponent
            {
                pluginName = _pluginName,
                pluginIndex = _pluginIndex
            });
        }

        public void PluginDropped(PluginComponent _plugin)
        {
            try
            {
                IPlugin nowPlugin;
                IUI nowUI;
                nowPlugin = pluginService.loadPlugin(_plugin.pluginIndex);
                nowUI = nowPlugin.GetUIPlugins();
                pluginUI = nowUI.GetPluginUI();
                DebugLogger.Log(3, $"[DEBUG] Plugin [ID.{_plugin.pluginIndex} - {_plugin.pluginName}] Loadded Succesfully!");
            }
            catch (Exception ex)
            {
                DebugLogger.Log(1, $"[ERROR] Error Occurred while loading plugins !!"
                    + $"Exception Message\n::{ex}\n");
            }
        }

        private void _dateTimer_Tick(object sender, EventArgs e)
        {
            displayTime = DateTime.Now.ToString("yyyy. MM. dd tt hh:mm:ss");
        }
    }
}
