using Platform.Model;
using System.IO.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Services
{
    internal class SerialDevice
    {
        public int id { get; set; }
        public SerialPort sp { get; set; }

        public SerialDevice(int _id, SerialConfig serialConfig)
        {
            id = _id;
            sp = new SerialPort
            {
                PortName = serialConfig.comPort,
                BaudRate = serialConfig.baudRate,
                Parity = (Parity)serialConfig.parity,
                DataBits = serialConfig.dataBits,
                DtrEnable = serialConfig.dtrEnable
            };
        }

        public void Connect()
        {
            try
            {
                // TODO :: Align EventHandler

                sp.Open();
            }
            catch (Exception ex)
            {
                // TODO :: REVEAL ERROR While Opening Serial Port
            }
        }

        public void Disconnect()
        {
            if ((sp != null && sp.IsOpen))
            {
                try
                {
                    // TODO :: Free EventHandler

                    System.Threading.Thread.Sleep(20);

                    sp.Close();
                }
                catch (Exception ex)
                {
                    // TODO :: REVEAL ERROR While Closing Serial Port
                }
            }
        }
    }
}
