using IRDetection.IRDevices;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using PluginBase;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace IRDetection
{
    internal class ViewModel : Notifier
    {
        private readonly IISerialDeviceHandler serialDeviceHandler;
        private readonly IISPIDeviceHandler spiDeviceHandler;
        private LeptonService leptonService;
        public Settings settings { get; }

        private readonly Stopwatch stopwatch = new Stopwatch();

        private readonly object frameLock = new object();
        private readonly object sendLock = new object();
        private readonly object bboxLock = new object();
        
        private int[] _serialDevices { get; set; }
        private int[] _spiDevices { get; set; }

        public ICommand NPUConnectCommand { get; }
        public ICommand IRConnectCommand  { get; }

        public WriteableBitmap colorBitmap { get; private set; }
        public BitmapSource _bitmapShow;

        public Mat colorMat;
        public Mat colorMatShow;

        private Mat frameToDraw;
        private Mat convertedMat;

        private List<OpenCvSharp.Rect> bbox = new List<OpenCvSharp.Rect>();

        public byte[] lepton_render = new byte[57600];

        // WARN:: Temporary action for lepton sensor. PixelMax will be "MaxTemp - bias"
        private float _bias;
        private float _pixelMax;
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
        public float pixelMax
        {
            get { return _pixelMax; }
            set { _pixelMax = value; OnPropertyChanged(); }
        }
        public float bias
        {
            get { return _bias; }
            set { _bias = value; OnPropertyChanged(); }
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

        public ViewModel(IISerialDeviceHandler _serialDeviceHandler, IISPIDeviceHandler _spiDeviceHandler, Settings _settings)
        {
            serialDeviceHandler = _serialDeviceHandler;
            spiDeviceHandler = _spiDeviceHandler;
            settings = _settings;

            leptonService = new LeptonService();

            serialDeviceHandler.PointsReceived += OnPointsReceived;

            NPUConnectCommand = new RelayCommand(param =>
            {
                NPUConnect();
            });

            IRConnectCommand = new RelayCommand(param =>
            {
                IRConnect();
            });

            GetDeviceNum();

            settings.serialPortID = serialDevices.Length > 0 ? serialDevices[0] : -1;
            settings.spiPortID = spiDevices.Length > 0 ? spiDevices[0] : 0;
            settings.devicePortID = spiDevices.Length > 1 ? spiDevices[1] : 0;
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

        private void IRConnect()
        {
            leptonService.LeptonInitialize(settings.devicePortID);
            leptonService.FrameUpdate += OnFrameUpdate;
        }

        private void IRDisconnect()
        {
            leptonService.Disconnect();
            leptonService.FrameUpdate -= OnFrameUpdate;
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
                Cv2.Resize(colorMatShow, frameToDraw, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Linear);

                foreach (var box in bbox)
                {
                    FindMaxTempInFace(cls[cnt], box);

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

                bitmapShow = bitmap_tmp;
                fps = 1 / elapsed;

                this.pow = power;
                this.amp = ampere;
                this.detectedCnt = b_box.Count;
            });

            DebugLogger.Log(3, $"[DEBUG] All packets successfully processed! Set Status to Idle");
            sendStatus = ESendStatus.Idle;
        }

        private void OnFrameUpdate(byte[] evt)
        {
            try
            {
                MapInferno(evt);

                if (Application.Current == null) return;

                Application.Current.Dispatcher.Invoke(new Action(() => {
                    if (colorBitmap == null || colorBitmap.PixelWidth != 160 || colorBitmap.PixelHeight != 120)
                    {
                        colorBitmap = new WriteableBitmap(160, 120, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);
                    }
                    UpdateColorBitmap();
                }));

                //Task.Run(async () => await ProcessImageAndCallSendFunc());
                _ = ProcessImageAndCallSendFunc();
            }
            catch (Exception ex)
            {
                DebugLogger.Log(1, $"[ERROR] Error While Updating Frame :: {ex}");
            }
        }

        private async Task ProcessImageAndCallSendFunc()
        {
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

                if (colorMat == null)
                {
                    DebugLogger.Log(1, $"[ERROR] Frame Empty.");
                    return;
                }

                if (convertedMat == null || convertedMat.IsDisposed)
                    // TODO: FIX HARD CODDED PARAMETER LATER
                    convertedMat = new Mat(160, 160, MatType.CV_8UC3);

                if (settings.sendMod == 0)
                {
                    // ADD PADDING TO MAKE 160X160 IMAGE
                    DebugLogger.Log(3, $"[DEBUG] Send Mode {settings.sendMod}, add padding to image");

                    Cv2.CopyMakeBorder(colorMat, convertedMat, 20, 20, 0, 0, BorderTypes.Constant, new Scalar(0, 0, 0));
                    Cv2.CvtColor(convertedMat, convertedMat, ColorConversionCodes.BGR2RGB);
                }
                else
                {
                    // RESIZE TO MAKE 160X160 IMAGE
                    DebugLogger.Log(3, $"[DEBUG] Send Mode {settings.sendMod}, resize image");

                    Cv2.Resize(colorMat, convertedMat, new OpenCvSharp.Size(160, 160), 0, 0, InterpolationFlags.Linear);
                    Cv2.CvtColor(convertedMat, convertedMat, ColorConversionCodes.BGR2RGB);
                }

                lock (sendLock)
                {
                    if (colorMatShow != null && !colorMatShow.IsDisposed)
                    {
                        colorMat.CopyTo(colorMatShow);
                    }
                    else
                    {
                        colorMatShow = colorMat.Clone();
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

        unsafe private void UpdateColorBitmap()
        {
            if (settings.deviceID == 1)
            {
                colorBitmap.WritePixels(new Int32Rect(0, 0, 160, 120), lepton_render, 160 * 3, 0);
                OnPropertyChanged(nameof(colorBitmap));

                isUpdated = true;

                if (sendStatus != ESendStatus.Idle) return;

                lock (frameLock)
                {
                    if (colorMat == null || colorMat.Rows != 160 || colorMat.Channels() != 3)
                    {
                        colorMat?.Dispose();
                        colorMat = new Mat(120, 160, MatType.CV_8UC3);
                    }

                    System.Runtime.InteropServices.Marshal.Copy(lepton_render, 0, colorMat.Data, lepton_render.Length);
                }
            }
        }

        private void MapInferno(byte[] frame)
        {
            float min = settings.minTemp * 100.0f + 27000.0f;
            float max = settings.maxTemp * 100.0f + 27000.0f;
            float scale = 255.0f / (max - min);

            int imageBufferIndex = 0;

            if (frame == null)
            {
                string dummyImagePath = @"C:\Users\user\Downloads\doksan_dev\thermal_check\lepton_test_0.png";
                using (Mat img = Cv2.ImRead(dummyImagePath, ImreadModes.Color))
                {
                    System.Runtime.InteropServices.Marshal.Copy(img.Data, lepton_render, 0, img.Width * img.Height * 3);
                }
                return;
            }

            for (int i = 0; i < 19680; i++)
            {
                // Jump every headet in packet (4 bytes)
                if ((i % 82) < 2) continue;

                UInt16 rawValue = (UInt16)((frame[i * 2] << 8) + frame[i * 2 + 1]);

                if (rawValue < min) rawValue = (UInt16)min;
                if (rawValue > max) rawValue = (UInt16)max;

                int value = (int)((rawValue - min) * scale);

                int ofs_r = 3 * value + 0; if (ColorMapSource.colormap.Length <= ofs_r) ofs_r = ColorMapSource.colormap.Length - 1;
                int ofs_g = 3 * value + 1; if (ColorMapSource.colormap.Length <= ofs_g) ofs_g = ColorMapSource.colormap.Length - 1;
                int ofs_b = 3 * value + 2; if (ColorMapSource.colormap.Length <= ofs_b) ofs_b = ColorMapSource.colormap.Length - 1;

                lepton_render[imageBufferIndex + 0] = (byte)ColorMapSource.colormap[ofs_b];
                lepton_render[imageBufferIndex + 1] = (byte)ColorMapSource.colormap[ofs_g];
                lepton_render[imageBufferIndex + 2] = (byte)ColorMapSource.colormap[ofs_r];

                imageBufferIndex += 3;
            }
        }

        private void FindMaxTempInFace(int cls, OpenCvSharp.Rect box)
        {
            double downsizeRatioX = 160.0f / settings.resolution_x;
            double downsizeRatioY = 120.0f / settings.resolution_y;

            int orgTLX = box.X;
            int orgTLY = box.Y;
            int orgBRX = box.X + box.Width - 1;
            int orgBRY = box.Y + box.Height - 1;

            int newTLX = (int)Math.Round(orgTLX * downsizeRatioX);
            int newTLY = (int)Math.Round(orgTLY * downsizeRatioY);
            int newBRX = (int)Math.Round(orgBRX * downsizeRatioX);
            int newBRY = (int)Math.Round(orgBRY * downsizeRatioY);

            newTLX = Math.Clamp(newTLX, 0, 159);
            newTLY = Math.Clamp(newTLY, 0, 119);
            newBRX = Math.Clamp(newBRX, 0, 159);
            newBRY = Math.Clamp(newBRY, 0, 119);

            float maxTemp = 0.0f;

            for (int y = newTLY; y <= newBRY; y++)
            {
                for (int x = newTLX; x <= newBRX; x++)
                {
                    int pixelIdx = (y * 160 + x) * 3;

                    byte b = lepton_render[pixelIdx];
                    byte g = lepton_render[pixelIdx + 1];
                    byte r = lepton_render[pixelIdx + 2];

                    float temp = DecalcTemp(r, g, b);

                    if (temp > maxTemp)
                    {
                        maxTemp = temp;
                    }
                }
            }
            pixelMax = maxTemp - bias;
        }

        private float DecalcTemp(int r, int g, int b)
        {
            int index = 0;
            int minDistance = 0;

            for (int i = 0; i < 256; i++)
            {
                int MapR = (int)ColorMapSource.colormap[3 * i + 0];
                int MapG = (int)ColorMapSource.colormap[3 * i + 1];
                int MapB = (int)ColorMapSource.colormap[3 * i + 2];

                int distanceR = r - MapR;
                int distanceG = g - MapG;
                int distanceB = b - MapB;

                int totalDistance = distanceR * distanceR + distanceG * distanceG + distanceB * distanceB;

                if (totalDistance == 0)
                {
                    index = i;
                    break;
                }

                if (totalDistance < minDistance)
                {
                    minDistance = totalDistance;
                    index = i;
                }
            }

            float min = settings.minTemp * 100.0f + 27000.0f;
            float max = settings.maxTemp * 100.0f + 27000.0f;

            float rawValue = index * ((max - min) / 255.0f) + min;

            return (rawValue - 27000.0f) / 100.0f;
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
