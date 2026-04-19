using FTD2XX_NET;
using Platform.Model;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Services
{
    internal class FTDIDevice
    {
        public string id { get; set; }
        public FTDI ftdi { get; set; }

        public FTDIDevice(string _id)
        {
            id = _id;
            ftdi = new FTDI(); ;
        }
        public int Connect()
        {
            if (ftdi.IsOpen) return 1;

            uint devCount = 0;
            ftdi.GetNumberOfDevices(ref devCount);

            if (devCount == 0)
            {
                // TODO :: ERROR REVEAL NO DEVICES FOUND
                return 0;
            }

            // Get FTDI Device List
            FTDI.FT_DEVICE_INFO_NODE[] deviceList = new FTDI.FT_DEVICE_INFO_NODE[devCount];
            ftdi.GetDeviceList(deviceList);

            int targetIndex = -1;

            // Find FT232H or RS232-HS
            for (int i = 0; i < devCount; i++)
            {
                // TODO :: LOG -> Device list

                if (deviceList[i].Description.Contains("RS232-HS") || deviceList[i].Type == FTDI.FT_DEVICE.FT_DEVICE_232H)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex == -1)
            {
                // TODO :: ERROR REVEAL NO SPECIFIC DEVICES FOUND (FT232H)
                return 0;
            }

            // Open Divice with Target Index
            FTDI.FT_STATUS status = ftdi.OpenByIndex((uint)targetIndex);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                // TODO :: ERROR REVEAL WHILE OPENING DEVICE FROM SPECIFIC INDEX
                return 0;
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
                    // TODO :: ERROR REVEAL BIT MODE SETTING FAILED
                    ftdi.Close();
                    return 0;
                }

                Thread.Sleep(50);

                // MPSSE Setting
                if (!MPSSEConfig())
                {
                    // TODO :: ERROR REVEAL MPSSE SETTING FAILED
                    ftdi.Close();
                    return 0;
                }

                // TODO :: LOG -> CONNECTED LOG
                return 1;
            }
            catch (Exception ex)
            {
                // TODO :: ERROR REVEAL Unexpected Error while Opening SPI Port
                ftdi.Close();
                return 0;
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
    }
}
