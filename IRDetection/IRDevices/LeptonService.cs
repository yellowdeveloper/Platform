using Iot.Device.Ft232H;
using Iot.Device.FtCommon;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Spi;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRDetection.IRDevices
{
    internal class LeptonService
    {
        private Ft232HDevice ft232h;
        private SpiConnectionSettings settings;

        // PACEKT SIZE 164
        byte[] lepton_packet = new byte[164];
        // SEGMENT SIZE = PAKCET SIZE * PACKETS PER SEGMENT = 160 * 60 = 9840
        byte[] lepton_segment_left = new byte[9676];
        // COMPLETE IMAGE == 4 SEGMENTS
        // byte[,] lepton_image = new byte[4, 9600];
        byte[] lepton_image = new byte[78720];
        byte[] tmp = new byte[39360];

        private CancellationTokenSource _cts;
        private int locID;

        int imageIndexCount = 0;
        int readIndexCount = -1;

        bool isWritingRawBuffer = false;

        public event Action<byte[]> FrameUpdate;

        public void LeptonInitialize(int _locID)
        {
            locID = _locID;

            Connect();
            _cts = new CancellationTokenSource();
            Task.Run(() => CommunicateLepton3(_cts.Token));
        }

        public int Connect()
        {
            //SPI Connect
            var devices = FtCommon.GetDevices();
            var targetDevice = devices.FirstOrDefault(d => d.LocId == locID);

            if (targetDevice == null)
            {
                DebugLogger.Log(1, $"[ERROR] LocID {locID}에 해당하는 장치를 찾을 수 없습니다.");
                return 0;
            }
            DebugLogger.Log(3, $"[DEBUG] 성공적으로 LocID {locID}에 해당하는 장치에 연결되었습니다.");

            ft232h = new Ft232HDevice(targetDevice);

            settings = new SpiConnectionSettings(0, 3)
            {
                ClockFrequency = 18_000_000,
                Mode = SpiMode.Mode3,
                DataBitLength = 8,
                ChipSelectLineActiveState = PinValue.Low
            };

            if (ft232h == null)
            {
                return 0;
            }
            return 1;
        }

        public async Task CommunicateLepton3(CancellationToken cts)
        {
            SpiDevice spi = null;

            int discardCount = 0;
            int invalidSegmentCount = 0;

            while (!cts.IsCancellationRequested)
            {
                if (spi == null && ft232h != null)
                {
                    spi = ft232h.CreateSpiDevice(settings);
                }

                if (ft232h == null)
                {
                    DebugLogger.Log(3, $"[DEBUG] ft232 null :: reconnect");
                    Connect();
                    if (ft232h != null)
                    {
                        spi?.Dispose();
                        spi = ft232h.CreateSpiDevice(settings);
                        discardCount = 0;
                        invalidSegmentCount = 0;
                        imageIndexCount = 0;
                    }
                    else
                    {
                        FrameUpdate?.Invoke(null);
                        Thread.Sleep(500);
                    }
                    continue;
                }

                ReadPacket(spi);
                if ((lepton_packet[0] & 0x0F) == 0x0F)
                {
                    discardCount++;
                    if (discardCount > 4000)
                    {
                        DebugLogger.Log(3, $"[DEBUG] Reset Camera...");
                        Disconnect();
                        Thread.Sleep(190);
                    }
                    continue;
                }

                if (lepton_packet[1] != 0)
                {
                    DebugLogger.Log(1, $"[ERROR] Packet Index Error: {lepton_packet[1]}");
                    continue;
                }

                discardCount = 0;
                ReadSegment(spi);

                int segmentIndex = (lepton_segment_left[3116] >> 4 & 0x0F) - 1;

                if (segmentIndex < 0 || segmentIndex > 3)
                {
                    //Console.ForegroundColor = ConsoleColor.Red;
                    //Console.WriteLine($"Segment Index Error: {segmentIndex}");
                    //Console.ForegroundColor = ConsoleColor.White;

                    invalidSegmentCount++;
                    if (invalidSegmentCount > 10)
                    {
                        DebugLogger.Log(3, $"[DEBUG] Reset Camera...");
                        Disconnect();
                        Thread.Sleep(190);
                    }
                    continue;
                }
                invalidSegmentCount = 0;

                //Console.WriteLine($"Segment Index: {segmentIndex}");

                isWritingRawBuffer = true;
                Buffer.BlockCopy(lepton_packet, 0, lepton_image, imageIndexCount * 39360 + segmentIndex * 9840, 164);
                Buffer.BlockCopy(lepton_segment_left, 0, lepton_image, imageIndexCount * 39360 + segmentIndex * 9840 + 164, 9676);
                isWritingRawBuffer = false;

                if (segmentIndex == 3)
                {
                    Buffer.BlockCopy(lepton_image, imageIndexCount * 39360, tmp, 0, 39360);

                    imageIndexCount = (imageIndexCount + 1) % 2;
                    readIndexCount = (readIndexCount + 1) % 2;

                    FrameUpdate?.Invoke(tmp);
                }
            }
        }
        public void ReadSegment(SpiDevice spi)
        {
            try
            {
                spi.Read(lepton_segment_left);
            }
            catch (Exception ex)
            {
                DebugLogger.Log(1, $"[ERROR] Exception Occurred while reading packet :: {ex}");
                return;
            }
        }

        public void ReadPacket(SpiDevice spi)
        {
            try
            {
                spi.Read(lepton_packet);
            }
            catch (Exception ex)
            {
                DebugLogger.Log(1, $"[ERROR] Exception Occurred while reading packet :: {ex}");
                return;
            }
        }

        public void Disconnect()
        {
            if (ft232h != null)
            {
                try
                {
                    ft232h.Dispose();
                    ft232h = null;
                }
                catch (Exception ex)
                {
                    DebugLogger.Log(1, $"[ERROR] Exception Occurred while disconnecting :: {ex}");
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

            Disconnect();
        }

        ~LeptonService()
        {
            DebugLogger.Log(3, $"[DEBUG] Disposing LeptonService Instance");
        }
    }
}
