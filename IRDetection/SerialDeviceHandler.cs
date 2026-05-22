using PluginBase;
using PluginBase.CommonUtils;
using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IRDetection
{
    public interface IISerialDeviceHandler : ISerialDeviceHandler
    {
        public event Action<float, float, List<OpenCvSharp.Rect>, List<int>, List<int>> PointsReceived;
        public int[] GetSerialDeviceList();
        public void Dispose();
    }
    // 시리얼 인터페이스는 외부에 노출하지 않음.
    // 내부적으로 모든 기능을 실행.
    // 모듈마다 헤더 / 푸터 / 데이터 길이 등을 다르게 설정할 수 있는 가능성을 열어두기 위함.
    // 기본적인 로직 자체는 그래도 써도 무방.

    /// <summary>
    /// 시리얼 수신 버퍼 처리 담당 인터페이스
    /// </summary>
    public class SerialDeviceHandler : IISerialDeviceHandler
    {
        private ISerialDevice serialDevice;
        private ISerialService serialService;

        private readonly Settings settings;

        private List<byte> receivedBuffer = new List<byte>();
        private List<byte> validData = new List<byte>();

        private int footerTryCnt = 0;

        public event Action<float, float, List<OpenCvSharp.Rect>, List<int>, List<int>> PointsReceived;

        /// <summary>
        /// Serial 클래스 생성자.
        /// </summary>
        /// <param name="_service">
        /// 메인 플랫폼에서 시리얼 디바이스를 관리하는 서비스 객체의 인스턴스. 시리얼 디바이스에 접근하기 위해 필요.
        /// </param>
        /// <param name="_settings">
        /// 통신 및 디스플레이에 필요한 모든 설정값을 담고 있는 객체의 인스턴스.
        /// </param>
        public SerialDeviceHandler(ISerialService _serialService, Settings _settings)
        {
            settings = _settings;
            serialService = _serialService;
        }

        public byte[] header { get; } = new byte[] { 0x10, 0x01, 0x10, 0x01 };
        public byte[] footer { get; } = new byte[] { 0x0D, 0x0A, 0x0D, 0x0A };
        /// <summary>
        /// 데이터 유효성을 확인하기 위함. 
        /// </summary>
        public int checkValidDataLength = 10;

        public void SerialConnect(int id)
        {
            serialDevice = serialService.GetDevice(id);
            if (serialDevice == null)
            {
                DebugLogger.Log(1, $"[ERROR] Device doesn't exists.");
                return;
            }
            DebugLogger.Log(3, $"[DEBUG] Serial Device No.{id} Plugin Connection Operated ...");

            serialDevice.SetDeviceHandler(this);
            serialDevice.AttachSerialEventHandler();

        }

        public void SerialDisconnect()
        {
            if (serialDevice == null) return;
            DebugLogger.Log(3, $"[DEBUG] Disconnect with serial device in IR Detection");


            serialDevice.SetDeviceHandler(null);
            serialDevice.DetachSerialEventHandler();
        }

        public void StackReceivedBuffer(byte[] dataByte, int actuallyRead)
        {
            receivedBuffer.AddRange(dataByte.Take(actuallyRead));
        }

        public void FindValidData()
        {
            DebugLogger.Log(3, $"[DEBUG] *Function In* FindValidData entered");
            ReadOnlySpan<byte> bufferSpan = CollectionsMarshal.AsSpan(receivedBuffer);

            int headerIndex = bufferSpan.IndexOf(header);
            int dataLength = 0;

            if (headerIndex != -1)
            {
                bufferSpan = bufferSpan.Slice(headerIndex + 4);
                int footerIndex = bufferSpan.IndexOf(footer);
                if (footerIndex != -1)
                {
                    dataLength = footerIndex;

                    if (validData.Count >= checkValidDataLength && validData.Count % checkValidDataLength == 0)
                    {
                        DebugLogger.Log(3, $"[DEBUG] Valid Packet Found, Process to Next Step");
                        validData = receivedBuffer.GetRange(headerIndex + 4, dataLength);
                    }
                    else
                    {
                        DebugLogger.Log(2, $"[WARN] Warning!! DataLength Not Valid. Discard this packet.");
                    }

                    receivedBuffer.RemoveRange(0, headerIndex + footerIndex + 8);
                }
                else
                {
                    if (footerTryCnt >= 5)
                    {
                        DebugLogger.Log(2, $"[WARN] Warning!! Wrong Footer :: Clear Buffer");
                        receivedBuffer.Clear();
                        footerTryCnt = 0;
                    }
                    footerTryCnt++;
                    DebugLogger.Log(3, $"[DEBUG] No Footer Found in Buffer Find Count:: {footerTryCnt}");
                    Thread.Sleep(1);
                }
            }
            else
            {
                DebugLogger.Log(2, $"[WARN] Warning!! No Header Found in Buffer... Find Again");
            }
        }

        public void ParseReceivedData()
        {
            DebugLogger.Log(3, $"[DEBUG] *Function In* ParseReceivedData entered");
            List<OpenCvSharp.Rect> receivedRects = new List<OpenCvSharp.Rect>();
            List<int> receivedCls = new List<int>();
            List<int> receivedProbs = new List<int>();

            float power = 0.0f;
            float ampere = 0.0f;

            int detectedCount = validData[0];

            validData.RemoveAt(0);

            int modelType = validData[validData.Count - 9];

            if (modelType != 1)
            {
                DebugLogger.Log(2, $"[WARN] ModelTypeError!! receivedType :: {modelType}, currentType :: 1");
                // SendModuleChangeNotice(EModuleType.IR);
                Thread.Sleep(10);
                // connectionState = EConnectionState.Connected;
                return;
            }

            ParseConsumption(validData.Count, out power, out ampere);
            ParseDetectionResult(detectedCount, receivedRects, receivedCls, receivedProbs);

            PointsReceived?.Invoke(ampere, power, receivedRects, receivedCls, receivedProbs);
        }

        private void ParseDetectionResult(int detectedCount, List<OpenCvSharp.Rect> receivedRects, List<int> receivedCls, List<int> receivedProbs)
        {
            DebugLogger.Log(3, $"[DEBUG] *Function In* ParseDetectionResult entered");

            for (int i = 0; i < detectedCount; i++)
            {
                byte[] rectData = validData.Take(10).ToArray();
                validData.RemoveRange(0, 10);

                int cls = rectData[0];
                int prob = rectData[1];
                int x = BitConverter.ToInt16(rectData, 2); // lt x
                int y = BitConverter.ToInt16(rectData, 4); // lt y
                int w = BitConverter.ToInt16(rectData, 6);
                int h = BitConverter.ToInt16(rectData, 8);

                DebugLogger.Log(3, $"[DEBUG] Before Resize :: x={x}, y={y}, w={w}, h={h}");

                int x_new = x;
                int y_new = y;
                int w_new = w;
                int h_new = h;

                double ratio_x;
                double ratio_y;

                if (settings.deviceID == 1)
                {
                    ratio_x = 640.0f / settings.resolution_x;
                    ratio_y = 480.0f / settings.resolution_y;
                }
                else
                {
                    ratio_x = 512.0f / settings.resolution_x;
                    ratio_y = 512.0f / settings.resolution_y;
                }

                x_new = (int)(x * ratio_x);
                y_new = (int)(y * ratio_y);
                w_new = (int)(w * ratio_x);
                h_new = (int)(h * ratio_y);

                DebugLogger.Log(3, $"[DEBUG] Num {i + 1} | class {cls} | probability {prob} :: x={x}, y={y}, w={w}, h={h}");

                if (prob >= settings.probThres && (cls == 0 || cls == 1))
                {
                    receivedCls.Add(cls);
                    receivedProbs.Add(prob);
                    receivedRects.Add(new OpenCvSharp.Rect(x_new, y_new, w_new, h_new));
                }
            }
        }

        private void ParseConsumption(int validDataLen, out float power, out float ampere)
        {
            DebugLogger.Log(3, $"[DEBUG] *Function In* ParseConsumption entered");
            byte[] powerByte = new byte[4];
            byte[] ampereByte = new byte[4];

            powerByte[0] = validData[validDataLen - 8];
            powerByte[1] = validData[validDataLen - 7];
            powerByte[2] = validData[validDataLen - 6];
            powerByte[3] = validData[validDataLen - 5];

            ampereByte[0] = validData[validDataLen - 4];
            ampereByte[1] = validData[validDataLen - 3];
            ampereByte[2] = validData[validDataLen - 2];
            ampereByte[3] = validData[validDataLen - 1];

            validData.RemoveRange(validData.Count - 9, 9);

            float powerVal = BitConverter.ToUInt32(powerByte);
            power = powerVal / 1000.0f;

            float ampereVal = BitConverter.ToUInt32(ampereByte);
            ampere = ampereVal / 1000.0f;
        }

        public void Dispose()
        {
            SerialDisconnect();

            receivedBuffer.Clear();
            validData.Clear();
        }

        public int[] GetSerialDeviceList()
        {
            return serialService.GetDeviceList();
        }

        ~SerialDeviceHandler()
        {
            DebugLogger.Log(3, $"[DEBUG] Disposing IRDetection Serial Interface");
        }
    }
}
