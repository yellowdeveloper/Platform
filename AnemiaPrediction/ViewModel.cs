using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static System.Resources.ResXFileRef;

namespace AnemiaPrediction
{
    internal class ViewModel: Notifier
    {
        private readonly IISerialDeviceHandler serialDeviceHandler;
        private readonly IISPIDeviceHandler spiDeviceHandler;
        private WebCamService webCamService;
        public Settings settings { get; }

        private readonly object frameLock = new object();
        private readonly object sendLock = new object();
        private readonly object bboxLock = new object();

        private int[] _serialDevices { get; set; }
        private int[] _spiDevices { get; set; }

        public ICommand NPUConnectCommand { get; }
        public ICommand CameraConnectCommand { get; }
        public ICommand MeasureStartCommand { get; }

        private BitmapSource _bitmapShow;
        private BitmapSource _bitmapProcessed;

        private Mat frameToSend;
        private Mat tmpMat;
        private Mat frameToDraw;
        private Mat convertedMat;

        private List<OpenCvSharp.Rect> bbox = new List<OpenCvSharp.Rect>();

        private float _power;
        private float _ampere;
        private float _pred;

        private bool _measureFlag = false;
        private bool isUpdated = false;
        private bool isRefBoxValid = false;
        private bool isImageValid = false;
        private bool isDetectionComplete = false;

        private int tryCount = 0;
        public ESendStatus sendStatus = ESendStatus.NotReady;

        private string _topMessageLeft;
        private string _topMessageRight = "진단 결과가 없습니다.";

        public int[] serialDevices
        {
            get { return _serialDevices; }
            set { _serialDevices = value; OnPropertyChanged(); }
        }
        public int[] spiDevices
        {
            get { return _spiDevices; }
            set { _spiDevices = value; OnPropertyChanged(); }
        }
        public float pow
        {
            get { return _power; }
            set { _power = value; OnPropertyChanged(); }
        }
        public float amp
        {
            get { return _ampere; }
            set { _ampere = value; OnPropertyChanged(); }
        }
        public float pred
        {
            get => _pred;
            set { _pred = value; OnPropertyChanged(); }
        }
        public bool measureFlag
        {
            get => _measureFlag;
            set { _measureFlag = value; OnPropertyChanged(); }
        }
        public BitmapSource bitmapShow
        {
            get => _bitmapShow;
            set { _bitmapShow = value; OnPropertyChanged(); }
        }
        public BitmapSource bitmapProcessed
        {
            get => _bitmapProcessed;
            set { _bitmapProcessed = value; OnPropertyChanged(); }
        }
        public string topMessageLeft
        {
            get => _topMessageLeft;
            set { _topMessageLeft = value; OnPropertyChanged(); }
        }

        public string topMessageRight
        {
            get => _topMessageRight;
            set { _topMessageRight = value; OnPropertyChanged(); }
        }

        public ViewModel(IISerialDeviceHandler _serialDeviceHandler, IISPIDeviceHandler _spiDeviceHandler, Settings _settings)
        {
            serialDeviceHandler = _serialDeviceHandler;
            spiDeviceHandler = _spiDeviceHandler;
            settings = _settings;

            webCamService = new WebCamService();

            serialDeviceHandler.PointsReceived += OnPointsReceived;

            NPUConnectCommand = new RelayCommand(param =>
            {
                NPUConnect();
            });

            CameraConnectCommand = new RelayCommand(param =>
            {
                CamaraConnect();
            });

            MeasureStartCommand = new RelayCommand(param =>
            {
                measureFlag = true;
            });

            GetDeviceNum();

            settings.serialPortID = serialDevices.Length > 0 ? serialDevices[0] : -1;
            settings.spiPortID = spiDevices.Length > 0 ? spiDevices[0] : 0;
        }

        private void NPUConnect()
        {
            serialDeviceHandler.SerialConnect(settings.serialPortID);
            spiDeviceHandler.SPIConnect(settings.spiPortID);

            serialDeviceHandler.SendModuleAlarm();

            sendStatus = ESendStatus.Idle;
        }

        private void NPUDisconnect()
        {
            serialDeviceHandler.SerialDisconnect();
            spiDeviceHandler.SPIDisconnect();

            sendStatus = ESendStatus.NotReady;
        }

        private void CamaraConnect()
        {
            webCamService.WebCamInitialize();
            webCamService.FrameUpdate += OnFrameUpdate;
        }

        private void CamaraDisconnect()
        {
            webCamService.Dispose();
            webCamService.FrameUpdate -= OnFrameUpdate;
        }

        private void OnPointsReceived(float power, float ampere, float prob, byte errorCode, List<OpenCvSharp.Rect> b_box)
        {
            DebugLogger.Log(3, $"[DEBUG] Points Received !! Processing Data ...");

            if (errorCode == 0x01)
            {
                topMessageRight = "손가락이 충분히 탐지되지 않았습니다.";
                isImageValid = false;
                return;
            }

            if (errorCode == 0x02)
            {
                topMessageRight = "손이 너무 가깝거나 위치가 잘못됐습니다.";
                isImageValid = false;
                return;
            }

            isImageValid = true;
            isDetectionComplete = true;

            int cnt = 0;

            lock (bboxLock)
            {
                bbox = b_box;
            }

            if (frameToDraw == null || frameToDraw.IsDisposed)
                // TODO: FIX HARD CODDED PARAMETER LATER
                frameToDraw = new Mat(640, 640, MatType.CV_8UC3);

            lock (bboxLock)
            {
                Cv2.Resize(tmpMat, frameToDraw, new OpenCvSharp.Size(640, 640), 0, 0, InterpolationFlags.Linear);

                foreach (var box in bbox)
                {
                    Cv2.Rectangle(frameToDraw, box, Scalar.Red, 1);
                    cnt++;
                }
            }

            DebugLogger.Log(3, $"[DEBUG] Bbox drawing Completed ... Check Image\n");

            Application.Current.Dispatcher.Invoke(() => {
                BitmapSource bitmap_tmp = frameToDraw.ToBitmapSource();
                bitmap_tmp.Freeze();

                bitmapProcessed = bitmap_tmp;

                this.pow = power;
                this.amp = ampere;
            });
            this.pred = prob * 100.0f;

            if (pred <= 30)
            {
                topMessageRight = $"높은 확률로 정상 입니다.";
            }
            else if (pred >= 70)
            {
                topMessageRight = $"높은 확률로 빈혈 입니다.";
            }
            else
            {
                topMessageRight = $"정확한 진료가 어렵습니다. 병원에 방문해주세요.";
            }
            topMessageLeft = $"진단이 완료되었습니다.";

            measureFlag = false;
            isImageValid = false;
            isDetectionComplete = false;

            DebugLogger.Log(3, $"[DEBUG] All packets successfully processed! Set Status to Idle");
            sendStatus = ESendStatus.Idle;
        }

        private void OnFrameUpdate(Mat frame)
        {
            try
            {
                if (Application.Current == null) return;

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    OpenCvSharp.Rect roi = new OpenCvSharp.Rect(110, 80, 420, 320);

                    if (bitmapShow == null || bitmapShow.PixelWidth != frame.Width || bitmapShow.PixelHeight != frame.Height)
                    {
                        bitmapShow = new WriteableBitmap(roi.Width, roi.Height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);
                    }
                    using (Mat centerCropped = frame[roi])
                    {
                        UpdateFrame(centerCropped);
                    }
                }));

                if (UtilsForMatImage.CheckIfRegionWhite(frame, new OpenCvSharp.Rect(180, 285, 34, 34)))
                {
                    isRefBoxValid = true;
                    topMessageLeft = "이미지 전송 후 결과를 기다리는 중입니다";
                }
                else
                {
                    isRefBoxValid = false;
                    topMessageLeft = "배경이 흰색이 아니거나, 너무 어둡습니다.";
                    return;

                }

                //Task.Run(async () => await ProcessImageAndCallSendFunc());
                _ = ProcessImageAndCallSendFunc();
            }
            catch (Exception ex)
            {
                DebugLogger.Log(1, $"[ERROR] Error While Updating Frame :: {ex}");
            }
            finally
            {
                frame.Dispose();
            }
        }

        public unsafe void UpdateFrame(Mat frame)
        {
            BitmapSource bitmapTmp = frame.ToBitmapSource();
            bitmapShow = bitmapTmp;

            isUpdated = true;

            if (sendStatus != ESendStatus.Idle || !measureFlag || isDetectionComplete) return;

            lock (frameLock)
            {
                OpenCvSharp.Rect roi = new OpenCvSharp.Rect(50, 0, 320, 320);
                using (Mat centerCropped = frame[roi])
                {
                    if (frameToSend != null && !frameToSend.IsDisposed)
                    {
                        centerCropped.CopyTo(frameToSend);
                    }
                    else
                    {
                        frameToSend = centerCropped.Clone();
                    }
                }
            }
        }

        private async Task ProcessImageAndCallSendFunc()
        {
            DebugLogger.Log(2, $"[DEBUG] trc:: {tryCount} | status:: {sendStatus} {isUpdated} {isRefBoxValid}");
            if (sendStatus == ESendStatus.WaitingForResponse && tryCount > 15)
            {
                DebugLogger.Log(2, $"[WARN] Late Response, re-call Send Function tryCount::{tryCount} | status:: {sendStatus}");
                sendStatus = ESendStatus.Idle;
            }
            if (sendStatus == ESendStatus.Idle && isUpdated && measureFlag && isRefBoxValid)
            {
                DebugLogger.Log(3, $"[DEBUG] State {sendStatus}, preprocess image data matrix");
                sendStatus = ESendStatus.Sending;

                tryCount = 0;

                if (frameToSend == null)
                {
                    DebugLogger.Log(1, $"[ERROR] Frame Empty.");
                    return;
                }

                if (convertedMat == null || convertedMat.IsDisposed)
                    // TODO: FIX HARD CODDED PARAMETER LATER
                    convertedMat = new Mat(320, 320, MatType.CV_8UC3);

                Cv2.CvtColor(frameToSend, convertedMat, ColorConversionCodes.BGR2RGB);

                lock (sendLock)
                {
                    if (tmpMat != null && !tmpMat.IsDisposed)
                    {
                        frameToSend.CopyTo(tmpMat);
                    }
                    else
                    {
                        tmpMat = frameToSend.Clone();
                    }
                }

                try
                {
                    DebugLogger.Log(3, $"[DEBUG] Processing Image endded, Call Send Function");
                    await spiDeviceHandler.SendImage(convertedMat);

                    sendStatus = ESendStatus.WaitingForResponse;
                }
                catch (Exception ex)
                {
                    DebugLogger.Log(1, $"[ERROR] Error While Sending Image Data, set Status to Idle. Exception Message :: {ex}");
                    sendStatus = ESendStatus.Idle;
                }
            }
            else if (sendStatus == ESendStatus.WaitingForResponse)
            {
                tryCount++;
            }
        }

        public void Dispose()
        {
            serialDeviceHandler.Dispose();
            serialDeviceHandler.PointsReceived -= OnPointsReceived;
        }

        public void GetDeviceNum()
        {
            serialDevices = serialDeviceHandler.GetSerialDeviceList();
            spiDevices = spiDeviceHandler.GetSPIDeviceList();
        }

        ~ViewModel()
        {
            DebugLogger.Log(3, $"[DEBUG] Disposing IRDetection ViewModel Instance");
        }
    }
}
