using PluginBase;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IRDetection
{
    internal class ViewModel : Notifier
    {
        private readonly IICommunication serial;
        public Settings settings { get; }

        

        public ICommand ConnectCommand { get; }

        public ViewModel(IICommunication _serial, Settings _settings)
        {
            serial = _serial;
            settings = _settings;

            serial.PointsReceived += OnPointsReceived;

            ConnectCommand = new RelayCommand(param =>
            {
                serial.PluginConnect(settings.serialPortID);
            });
        }

        private void OnPointsReceived(float power, float ampere, List<OpenCvSharp.Rect> b_box, List<int> cls, List<int> prob)
        {

        }

        public void Dispose()
        {
            serial.Dispose();
            serial.PointsReceived -= OnPointsReceived;
        }

        ~ViewModel()
        {
            DebugLogger.Log(3, $"[DEBUG] Disposing IRDetection ViewModel Instance");
        }
    }
}
