using OpenCvSharp;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetection
{
    class WebCamService
    {
        private VideoCapture _capture;
        private CancellationTokenSource _cts;
        private int _access_try = 0;

        public event Action<Mat> FrameUpdate;
        public void WebCamInitialize()
        {
            _capture?.Dispose();
            _capture = new VideoCapture(0);

            if (_capture.IsOpened())
            {
                DebugLogger.Log(3, $"[DEBUG] Web Cam enabled");
                _cts = new CancellationTokenSource();
                Task.Run(() => CaptureFrame(_cts.Token));
            }
            else
            {
                DebugLogger.Log(1, $"[ERROR] Web Cam Initialize Failed");
            }
        }

        private async Task CaptureFrame(CancellationToken token)
        {
            while (_capture != null && _capture.IsOpened() && !token.IsCancellationRequested)
            {
                using (var frame = new Mat())
                {
                    _capture.Read(frame);
                    if (frame.Empty())
                    {
                        // if no frame for 1sec, reconnect cam
                        if (_access_try < 5)
                        {
                            _access_try++;
                            DebugLogger.Log(3, $"[DEBUG] Capture failed. no frame to save :: waiting for frame :: " + _access_try.ToString());
                            await Task.Delay(500);
                            continue;
                        }

                        _cts.Cancel();

                        while (_cts.IsCancellationRequested)
                        {
                            DebugLogger.Log(2, $"[WARN] Try to Initialize WebCam again ... ");
                            WebCamInitialize();
                            await Task.Delay(500);
                        }
                    }
                    else
                    {
                        _access_try = 0;
                        FrameUpdate?.Invoke(frame.Clone());
                        await Task.Delay(33);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_cts != null)
            {
                try
                {
                    _cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (AggregateException)
                {
                }
                finally
                {
                    try { _cts.Dispose(); } catch { }
                    _cts = null;
                }
            }

            if (_capture != null)
            {
                try
                {
                    if (_capture.IsOpened())
                    {
                        _capture.Release();
                    }
                    _capture.Dispose();
                }
                catch (Exception ex)
                {
                    DebugLogger.Log(1, $"[ERROR] Capture Dispose Error: {ex.Message}");
                }
                finally
                {
                    _capture = null;
                }
            }

            DebugLogger.Log(3, $"[DEBUG] All Resources Disposed From WebCamService");
        }
    }
}
