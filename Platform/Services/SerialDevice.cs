using Platform.Model;
using System.IO.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluginBase;
using Platform.Utils;
using PluginBase.CommonUtils;

namespace Platform.Services
{
    internal class SerialDevice : ISerialDevice
    {
        private SerialPort sp { get; set; }

        private ISerialDeviceHandler serialDeviceHandler;

        public SerialDevice(SerialConfig serialConfig)
        {
            sp = new SerialPort
            {
                PortName = serialConfig.comPort,
                BaudRate = serialConfig.baudRate,
                Parity = (Parity)serialConfig.parity,
                DataBits = serialConfig.dataBits,
                DtrEnable = serialConfig.dtrEnable
            };
        }

        public bool Connect()
        {       
            try
            {
                sp.Open();
                DebugLogger.Log(3, $"[DEBUG] Port {sp.PortName} Opend.");
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                DebugLogger.Log(2, $"[WARN] The port {sp.PortName} is already in use.");
                return false;
            }
            catch (Exception ex)
            {
                DebugLogger.Log(1, $"[ERROR] Error While Opening Port {sp.PortName}"
                    + $"Exception Message\n::{ex}\n");
                return false;
            }
        }

        public void Disconnect()
        {
            if ((sp != null && sp.IsOpen))
            {
                try
                {
                    Thread.Sleep(20);

                    sp.Close();
                }
                catch (Exception ex)
                {
                    DebugLogger.Log(1, $"[ERROR] Error While Closing Port {sp.PortName}"
                        + $"Exception Message\n::{ex}\n");
                }
            }
        }

        public void SetDeviceHandler(ISerialDeviceHandler _serialDeviceHandler)
        {
            DebugLogger.Log(3, "[DEBUG] Serial Device Handler successfully attatched!!");
            serialDeviceHandler = _serialDeviceHandler;
        }
        public void AttachSerialEventHandler()
        {
            sp.DataReceived += OnSerialReceived;
        }
        public void DetachSerialEventHandler()
        {
            sp.DataReceived -= OnSerialReceived;
        }

        private void OnSerialReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!sp.IsOpen)
            {
                DebugLogger.Log(1, $"[ERROR] Port is not opened, cannot receive data from {sp.PortName}");
                return;
            }

            try
            {
                int bytesToRead = sp.BytesToRead;
                byte[] buffer = new byte[bytesToRead];
                int actuallyRead = sp.Read(buffer, 0, bytesToRead);

                DebugLogger.Log(3, $"[DEBUG] Serial Buffer :: ");
                Console.WriteLine("");
                for (int i = 0; i < buffer.Length; i++)
                {
                    Console.Write($"{buffer[i]:X2}");
                }
                Console.WriteLine("");

                if (actuallyRead > 0)
                {
                    serialDeviceHandler?.StackReceivedBuffer(buffer, actuallyRead);
                    serialDeviceHandler?.FindValidData();
                    serialDeviceHandler?.ParseReceivedData();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log(1, $"[ERROR] Error Occurred while receiving bytes from serial. {ex}");
            }
        }

        public void Send(Packet packet, int _chunkSize)
        {
            sp.Write(packet.Data, 0, _chunkSize);
        }
    }
}
