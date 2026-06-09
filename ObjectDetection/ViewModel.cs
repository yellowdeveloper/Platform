using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using PluginBase;
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

namespace ObjectDetection
{
    internal class ViewModel: Notifier
    {
        private readonly IISerialDeviceHandler serialDeviceHandler;
        private readonly IISPIDeviceHandler spiDeviceHandler;
        private WebCamService webCamService;
        public Settings settings { get; }

        private readonly Stopwatch stopwatch = new Stopwatch();

        private readonly object frameLock = new object();
        private readonly object sendLock = new object();
        private readonly object bboxLock = new object();

        private int[] _serialDevices { get; set; }
        private int[] _spiDevices { get; set; }

        public ICommand NPUConnectCommand { get; }
        public ICommand CameraConnectCommand { get; }

        private BitmapSource _bitmapShow;
        private BitmapSource _bitmapProcessed;

        private Mat frameToSend;
        private Mat tmpMat;
        private Mat frameToDraw;
        private Mat convertedMat;

        private List<OpenCvSharp.Rect> bbox = new List<OpenCvSharp.Rect>();

        private float _power;
        private float _ampere;
        private double _fps;
        private int _detectedCnt;

        private int tryCount = 0;
        private bool isUpdated = false;
        public ESendStatus sendStatus = ESendStatus.NotReady;

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
        public double fps
        {
            get { return _fps; }
            set { _fps = value; OnPropertyChanged(); }
        }
        public int detectedCnt
        {
            get { return _detectedCnt; }
            set { _detectedCnt = value; OnPropertyChanged(); }
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

        private void OnPointsReceived(float power, float ampere, List<OpenCvSharp.Rect> b_box, List<int> cls, List<int> prob)
        {
            DebugLogger.Log(3, $"[DEBUG] Points Received !! Processing Data ...");

            int cnt = 0;

            lock (bboxLock)
            {
                bbox = b_box;
            }

            List<OpenCvSharp.Rect> textBoxs = new List<OpenCvSharp.Rect>();

            if (frameToDraw == null || frameToDraw.IsDisposed)
                // TODO: FIX HARD CODDED PARAMETER LATER
                frameToDraw = new Mat(480, 640, MatType.CV_8UC3);


            lock (bboxLock)
            {
                Cv2.Resize(tmpMat, frameToDraw, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Linear);

                foreach (var box in bbox)
                {
                    Scalar rectColor = new Scalar(68, 156, 74, 255);  // Dark Green
                    Scalar textColor = new Scalar(255, 255, 255, 255);  // White
                    Cv2.Rectangle(frameToDraw, box, rectColor, 2);

                    textBoxs.Add(UtilsForMatImage.DrawTextWithBox(frameToDraw, rectColor, textColor, cls[cnt], prob[cnt], box, textBoxs));
                    cnt++;
                }
            }

            DebugLogger.Log(3, $"[DEBUG] Bbox drawing Completed ... Check Image\n");

            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed.TotalSeconds;

            Application.Current.Dispatcher.Invoke(() => {
                BitmapSource bitmap_tmp = frameToDraw.ToBitmapSource();
                bitmap_tmp.Freeze();

                bitmapProcessed = bitmap_tmp;
                fps = 1 / elapsed;

                this.pow = power;
                this.amp = ampere;
                this.detectedCnt = b_box.Count;
            });

            DebugLogger.Log(3, $"[DEBUG] All packets successfully processed! Set Status to Idle");
            sendStatus = ESendStatus.Idle;
        }

        private void OnFrameUpdate(Mat frame)
        {
            try
            {
                if (Application.Current == null) return;

                Application.Current.Dispatcher.Invoke(new Action(() => {
                    if (bitmapShow == null || bitmapShow.PixelWidth != frame.Width || bitmapShow.PixelHeight != frame.Height)
                    {
                        bitmapShow = new WriteableBitmap(frame.Width, frame.Height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);
                    }
                    UpdateFrame(frame);
                }));

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

            if (sendStatus != ESendStatus.Idle) return;

            lock (frameLock)
            {
                if (frameToSend != null && !frameToSend.IsDisposed)
                {
                    frame.CopyTo(frameToSend);
                }
                else
                {
                    frameToSend = frame.Clone();
                }
            }
        }

        private async Task ProcessImageAndCallSendFunc()
        {
            DebugLogger.Log(2, $"[DEBUG] trc:: {tryCount} | status:: {sendStatus}");
            if (sendStatus == ESendStatus.WaitingForResponse && tryCount > 15)
            {
                DebugLogger.Log(2, $"[WARN] Late Response, re-call Send Function tryCount::{tryCount} | status:: {sendStatus}");
                sendStatus = ESendStatus.Idle;
            }
            if (sendStatus == ESendStatus.Idle && isUpdated)
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

                if (settings.sendMod == 0)
                {
                    // TODO :: Add downsizing before pad
                    // ADD PADDING TO MAKE 320X320 IMAGE
                    DebugLogger.Log(3, $"[DEBUG] Send Mode {settings.sendMod}, add padding to image");

                    Cv2.CopyMakeBorder(frameToSend, convertedMat, 80, 80, 0, 0, BorderTypes.Constant, new Scalar(0, 0, 0));
                    Cv2.CvtColor(convertedMat, convertedMat, ColorConversionCodes.BGR2RGB);
                }
                else
                {
                    // RESIZE TO MAKE 320X320 IMAGE
                    DebugLogger.Log(3, $"[DEBUG] Send Mode {settings.sendMod}, resize image");

                    Cv2.Resize(frameToSend, convertedMat, new OpenCvSharp.Size(320, 320), 0, 0, InterpolationFlags.Linear);
                    Cv2.CvtColor(convertedMat, convertedMat, ColorConversionCodes.BGR2RGB);
                }

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
                    stopwatch.Restart();
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

            spiDeviceHandler?.SPIDisconnect();

            CamaraDisconnect();

            frameToSend?.Dispose();
            tmpMat?.Dispose();
            frameToDraw?.Dispose();
            convertedMat?.Dispose();
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
