using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnemiaPrediction
{
    public class Settings : Notifier
    {
        private int _serialPortID;
        private int _spiPortID;

        private int _probThres = 50;

        private int _sendMod = 1;

        public int serialPortID
        {
            get { return _serialPortID; }
            set { _serialPortID = value; OnPropertyChanged(); }
        }
        public int spiPortID
        {
            get { return _spiPortID; }
            set { _spiPortID = value; OnPropertyChanged(); }
        }
        public int probThres
        {
            get { return _probThres; }
            set { _probThres = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// 이미지 전송 방식 선택
        /// 0 : pad  /  1 : resize (default)
        /// </summary>
        public int sendMod
        {
            get { return _sendMod; }
            set { _sendMod = value; OnPropertyChanged(); }
        }
    }
}
