using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using System.IO.Ports;

namespace Quiz.Support
{
    class ButtonModuleConnector
    {
        private const int PORT_BAUD_RATE = 9600;

        public string PortName { get; set; }

        private SerialPort modulePort;

        private bool isConnectorActive = true;

        public void Init(string portName)
        {
            PortName = portName;

            modulePort = new SerialPort(portName, PORT_BAUD_RATE);
            modulePort.DtrEnable = true;
            modulePort.ReadTimeout = 100;
            
            try {
                modulePort.Open();
            } catch { }
        }

        public int GetButtonClick()
        {
            isConnectorActive = true;

            int incomingByte;
            while (isConnectorActive) {
                try {
                    incomingByte = modulePort.ReadByte();
                    return incomingByte;
                } catch (Exception e) {
                    if (e is TimeoutException) {
                        Thread.Sleep(100);
                    } else {
                        return -1;
                    }
                }
            }

            return -2;
        }

        public void AbortListener() {
            isConnectorActive = false;
        }
    }
}
