using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRDetection
{
    public class Settings : Notifier
    {
        private int _deviceID = 1;

        private int _devicePortID = 0;
        private int _resolution_x = 640;
        private int _resolution_y = 480;
        private int _numOfData = 1025;

        private float _minTemp = 20.0f;
        private float _maxTemp = 40.0f;

        private int _serialPortID = 0;
        private int _probThres = 50;

        private int _sendMod = 0;

        /// <summary>
        /// 현재 사용 가능한 디바이스 : 총 2개
        /// 0 = [디웰전자] DTPA-UART-3232
        /// 1 = [FLIR] LEPTON 3.5 - (Default)
        /// </summary>
        public int deviceID
        {
            get { return _deviceID; }
            set { _deviceID = value; OnPropertyChanged(); }
        }

        public int devicePortID
        {
            get { return _devicePortID; }
            set { _devicePortID = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 디스플레이 해상도 (Width)
        /// default : 640
        /// </summary>
        public int resolution_x
        {
            get { return _resolution_x; }
            set { _resolution_x = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 디스플레이 해상도 (Height)
        /// default : 480
        /// </summary>
        public int resolution_y
        {
            get { return _resolution_y; }
            set { _resolution_y = value; OnPropertyChanged(); }
        }

        // 이 옵션은 DTPA-UART-3232를 사용할 때만 유효.
        public int numOfData
        {
            get { return _numOfData; }
            set { _numOfData = value; OnPropertyChanged(); }
        }

        public float minTemp
        {
            get { return _minTemp; }
            set { _minTemp = value; OnPropertyChanged(); }
        }

        public float maxTemp
        {
            get { return _maxTemp; }
            set { _maxTemp = value; OnPropertyChanged(); }
        }

        public int serialPortID
        {
            get { return _serialPortID; }
            set { _serialPortID = value; OnPropertyChanged(); }
        }
        public int probThres
        {
            get { return _probThres;  }
            set { _probThres = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 이미지 전송 방식 선택
        /// 0 : pad (default)  /  1 : resize
        /// </summary>
        public int sendMod
        {
            get { return _sendMod; }
            set { _sendMod = value; OnPropertyChanged(); }
        }

        ~Settings()
        {
            DebugLogger.Log(3, $"[DEBUG] Disposing IRDetection Setting Instance");
        }
    }
}
