using FTD2XX_NET;
using Platform.Model;
using PluginBase;
using PluginBase.CommonUtils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Services
{
    internal class FTDIDevice : ISPIDevice
    {
        public int id { get; }
        private FTDI ftdi { get; set; }

        private int locationID;
        // 라이브러리 지정 사용 구현 보류
        /// <summary>
        /// 타입이 0이면, FTD2XX 라이브러리 사용 (SPI 고속 통신)
        /// 타입이 1이면, Iot.Device.FtCommon 라이브러리 사용 (안정성은 낮으나, SPI 모드를 전부 이용 가능.)
        /// </summary>
        // private int libType;

        private ISPIDeviceHandler spiDeviceHandler;
        event EventHandler spiSendRequested;

        public FTDIDevice(int _id)
        {
            id = _id;
            ftdi = new FTDI();
        }

        public bool Connect(int _locationID)
        {
            locationID = _locationID;

            // Open Divice with Target Location
            FTDI.FT_STATUS status = ftdi.OpenByLocation((uint)locationID);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                DebugLogger.Log(1, $"[ERROR] Error occured while opening FTDI Device SPI {locationID}: {status}");
                return false;
            }

            try
            {
                // Device Init
                ftdi.ResetDevice();
                ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);

                ftdi.SetCharacters(0, false, 0, false);
                ftdi.SetTimeouts(1000, 1000);
                ftdi.SetLatency(1);
                ftdi.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x00, 0x00);

                // Set to MPSSE Mode
                status = ftdi.SetBitMode(0x00, 0x02);
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    DebugLogger.Log(1, $"[ERROR] Error occured while setting Bit Mode on FTDI Device SPI {locationID}: {status}");

                    ftdi.Close();
                    return false;
                }

                Thread.Sleep(50);

                // MPSSE Setting
                if (!MPSSEConfig())
                {
                    DebugLogger.Log(1, $"[ERROR] Error occured while setting MPSSE Config on FTDI Device SPI {locationID}: {status}");

                    ftdi.Close();
                    return false;
                }

                DebugLogger.Log(3, $"[DEBUG] Successfully Connected to FTDI Device SPI {locationID}: {status}");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.Log(1, $"[ERROR] Error occured while opening FTDI Device SPI {locationID}: {status}" +
                    $" Exception Message : {ex}");
                ftdi.Close();
                return false;
            }
        }

        private bool MPSSEConfig()
        {
            uint bytesWritten = 0;
            uint bytesRead = 0;
            byte[] buffer = new byte[1];

            ftdi.Write(new byte[] { 0xAA }, 1, ref bytesWritten);
            Thread.Sleep(10);
            ftdi.GetRxBytesAvailable(ref bytesRead);
            if (bytesRead > 0)
            {
                byte[] readData = new byte[bytesRead];
                ftdi.Read(readData, bytesRead, ref bytesRead);
            }

            ftdi.Write(new byte[] { 0xAB }, 1, ref bytesWritten);

            List<byte> cmd = new List<byte>();
            //uint bytesWritten = 0;

            // FT232H setting
            cmd.Add(0x8A); // Disable Divide by 5 (60MHz Master Clock) 
            cmd.Add(0x97); // Adaptive Clocking:: Enable = 0x96 || Disable = 0x97
            cmd.Add(0x8D); // Disable 3-Phase Data Clocking

            // Clock Setting
            // 30MHz = 60MHz / ((1 + Divisor) * 2) => Divisor = 0
            cmd.Add(0x86); // Set Clock Divisor 
            cmd.Add(0x00); // ValL
            cmd.Add(0x00); // ValH

            // Pin Direction Setting (Low Byte: ADBUS)
            cmd.Add(0x80); // 0x80
            cmd.Add(0x08); // Value: CS=1, SK=0
            cmd.Add(0xFB);  // Dir: SK/DO/CS=Out, DI=In

            // Send Setup Packet
            FTDI.FT_STATUS status = ftdi.Write(cmd.ToArray(), cmd.Count, ref bytesWritten);
            if (status != FTDI.FT_STATUS.FT_OK) return false;

            Thread.Sleep(20);
            return true;
        }

        // Chip Select (CS) Control : Low
        private void SetCS_Low()
        {
            uint bytesWritten = 0;
            byte[] cmd = { 0x80, 0x00, 0xFB };
            ftdi.Write(cmd, cmd.Length, ref bytesWritten);
        }

        // Chip Select (CS) Control : High
        private void SetCS_High()
        {
            uint bytesWritten = 0;
            byte[] cmd = { 0x80, 0x08, 0xFB };
            ftdi.Write(cmd, cmd.Length, ref bytesWritten);
        }
        public void SetDeviceHandler(ISPIDeviceHandler _spiDeviceHandler)
        {
            DebugLogger.Log(3, "[DEBUG] SPI Device Handler successfully attatched!!");
            spiDeviceHandler = _spiDeviceHandler;
        }
        public void AttachSPIEventHandler()
        {

        }
        public void DetachSPIEventHandler()
        {
            
        }

        public void Send(Packet packet, int _chunkSize)
        {
            int chunkSize = _chunkSize;
            int chunkSendCount = packet.DataLength / chunkSize;
            uint bytesWritten = 0;

            byte[] txBuffer = new byte[chunkSize + 3];

            txBuffer[0] = 0x11;                      // send cmd

            int len = chunkSize - 1;

            // Packet length
            txBuffer[1] = (byte)(len & 0xFF);        // Low Byte
            txBuffer[2] = (byte)((len >> 8) & 0xFF); // High Byte

            try
            {
                // [CS Low] Comm Start (ADBUS3 = 0)
                // 0x80(GPIO Setting) + 0x00(CS Low, 나머지 Low) + 0xFB(Direction)
                SetCS_Low();

                for (int i = 0; i < chunkSendCount; i++)
                {
                    int offset = i * chunkSize;
                    
                    // Copy and send with SPI
                    Buffer.BlockCopy(packet.Data, offset, txBuffer, 3, chunkSize);
                    FTDI.FT_STATUS status = ftdi.Write(txBuffer, txBuffer.Length, ref bytesWritten);

                    if (status != FTDI.FT_STATUS.FT_OK)
                    {
                        DebugLogger.Log(1, $"[ERROR] SPI Write Failed at offset {offset}: {status}");
                        break;
                    }
                }

                // [CS High] Comm Start (ADBUS3 = 1)
                // 0x80(GPIO Setting) + 0x08(CS High) + 0xFB(Direction)
                SetCS_High();

                DebugLogger.Log(3, $"[DEBUG] SPI Image Transfer Complete. Total: {packet.DataLength} bytes");
            }
            catch (Exception ex)
            {
                DebugLogger.Log(1, $"[ERROR] in SendImageFragment_SPI: {ex.Message}");
                SetCS_High();
                Disconnect();
            }
        }

        public int Disconnect()
        {
            if (ftdi != null && ftdi.IsOpen)
            {
                try
                {
                    ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
                    ftdi.SetBitMode(0x00, 0x00);

                    ftdi.Close();

                    System.Threading.Thread.Sleep(100);

                    // TODO :: LOG -> Disconnected Log
                    return 1;
                }
                catch (Exception ex)
                {
                    // TODO :: ERROR REVEAL While Closing SPI Connection
                    return -1;
                }
            }
            return -1;
        }

        public void Dispose()
        {
            DebugLogger.Log(3, $"[DEBUG] Disposing FTDI Device {id} ...");
            ftdi.Close();
            ftdi = null;
        }
    }
}