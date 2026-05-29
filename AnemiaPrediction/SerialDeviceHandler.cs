using OpenCvSharp;
using PluginBase;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AnemiaPrediction
{
    public interface IISerialDeviceHandler : ISerialDeviceHandler
    {
        public event Action<float, float, float, byte, List<OpenCvSharp.Rect>> PointsReceived;
        public int[] GetSerialDeviceList();
        public void SendModuleAlarm();
        public void Dispose();
    }

    internal class SerialDeviceHandler: IISerialDeviceHandler
    {
        private ISerialDevice serialDevice;
        private ISerialService serialService;

        private readonly Settings settings;

        private List<byte> receivedBuffer = new List<byte>();
        private List<byte> validData = new List<byte>();

        private int footerTryCnt = 0;

        public event Action<float, float, float, byte, List<OpenCvSharp.Rect>> PointsReceived;

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
        public int checkValidDataLength = 12;

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
            DebugLogger.Log(3, $"[DEBUG] Disconnect with serial device in Object Detection");


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

                    if (dataLength >= checkValidDataLength && dataLength % checkValidDataLength == 0)
                    {
                        DebugLogger.Log(3, $"[DEBUG] Valid Packet Found, Process to Next Step");
                        validData = receivedBuffer.GetRange(headerIndex + 4, dataLength);
                    }
                    else
                    {
                        DebugLogger.Log(2, $"[WARN] Warning!! DataLength Not Valid. Discard this packet.");
                    }

                    receivedBuffer.RemoveRange(0, headerIndex + footerIndex + 8);

                    footerTryCnt = 0;
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
            float prob = 0.0f;
            byte errorCode = 0;

            if (validData.Count < 12 && validData[0] == 0xFF)
            {
                errorCode = validData[1];
                PointsReceived.Invoke(0.0f, 0.0f, 0.0f, errorCode, null);
                return;
            }

            int detectedCount = validData[0];

            validData.RemoveAt(0);

            int modelType = validData[validData.Count - 9];

            if (modelType != 2)
            {
                DebugLogger.Log(2, $"[WARN] ModelTypeError!! receivedType :: {modelType}, currentType :: 2");
                Thread.Sleep(10);
                return;
            }

            ParseConsumption(validData.Count, out power, out ampere);
            ParseDetectionResult(detectedCount, receivedRects, out prob);

            PointsReceived?.Invoke(ampere, power, prob, errorCode, receivedRects);
        }

        private void ParseDetectionResult(int detectedCount, List<OpenCvSharp.Rect> receivedRects, out float prob)
        {
            DebugLogger.Log(3, $"[DEBUG] *Function In* ParseDetectionResult entered");

            byte[] probByte = new byte[4];
            probByte[0] = validData[validData.Count - 4];
            probByte[1] = validData[validData.Count - 3];
            probByte[2] = validData[validData.Count - 2];
            probByte[3] = validData[validData.Count - 1];

            float probVal = BitConverter.ToUInt32(probByte);
            prob = probVal / 1000.0f;

            validData.RemoveRange(validData.Count - 4, 4);

            for (int i = 0; i < detectedCount; i++)
            {
                byte[] rectData = validData.Take(9).ToArray();
                validData.RemoveRange(0, 9);

                int nailProb = rectData[1];
                int x = BitConverter.ToInt16(rectData, 2); // lt x
                int y = BitConverter.ToInt16(rectData, 4); // lt y
                int w = BitConverter.ToInt16(rectData, 6);
                int h = BitConverter.ToInt16(rectData, 8);

                DebugLogger.Log(3, $"[DEBUG] Before Process :: x={x}, y={y}, w={w}, h={h}");

                int x_new = x;
                int y_new = y;
                int w_new = w;
                int h_new = h;

                double ratio_x;
                double ratio_y;

                if (settings.sendMod == 0)
                {
                    y_new -= 40;
                }

                ratio_x = 640.0f / 320;
                ratio_y = 640.0f / 320;

                x_new = (int)(x_new * ratio_x);
                y_new = (int)(y_new * ratio_y);
                w_new = (int)(w_new * ratio_x);
                h_new = (int)(h_new * ratio_y);

                DebugLogger.Log(3, $"[DEBUG] Num {i + 1} | probability {nailProb} :: x={x}, y={y}, w={w}, h={h}");

                if (nailProb >= settings.probThres)
                {
                    receivedRects.Add(new Rect(x_new, y_new, w_new, h_new));
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

        public void SendModuleAlarm()
        {
            Packet moduleAlarm = new Packet();

            moduleAlarm.Data = new byte[] { 0x02, 0x0D, 0x0A };
            moduleAlarm.DataLength = 3;

            serialDevice.Send(moduleAlarm, moduleAlarm.DataLength);
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
            DebugLogger.Log(3, $"[DEBUG] Disposing Anemia Prediction Serial Interface");
        }
    }
}
