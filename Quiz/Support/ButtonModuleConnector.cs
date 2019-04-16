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

        public string PortName {
            get { return _portName; }
            set {
                _portName = value;
                modulePort.PortName = value;
            }
        }

        private string _portName;
        private bool isConnectorActive = true;
        private SerialPort modulePort;

        private List<string> portNames;

        private ModuleStatus status = ModuleStatus.Disconnected;

        public event Action<ModuleStatus> OnModuleConnectionChange;
        public event Action<List<string>> OnNewPortNames;

        private Thread watchDogThread;

        public ButtonModuleConnector() {
            modulePort = new SerialPort();
            modulePort.BaudRate = PORT_BAUD_RATE;
            modulePort.DtrEnable = true;
            modulePort.ReadTimeout = 100;
        }

        public void Init()
        {
            portNames = new List<string>();

            foreach (string name in SerialPort.GetPortNames()) {
                portNames.Add(name);
            }

            OnNewPortNames?.Invoke(portNames);

            watchDogThread = new Thread(WatchDog);
            watchDogThread.Start();
        }

        public int GetButtonClick()
        {
            isConnectorActive = true;
            try {
                modulePort.DiscardInBuffer();
            } catch { }

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

        public void AbortAll() {
            watchDogThread?.Abort();
            isConnectorActive = false;
        }

        public void WatchDog() {
            while (true) {
                CheckPort();
                Thread.Sleep(3000);
            }
        }

        private void CheckPort()
        {
            try {
                if (!modulePort.IsOpen) {
                    modulePort.Open();
                }

                if (status == ModuleStatus.Disconnected) {
                    status = ModuleStatus.Connected;
                    Extensions.ExcecuteWithAppIdleDispatcher(() => OnModuleConnectionChange?.Invoke(ModuleStatus.Connected));
                }
            } catch {
                if (status == ModuleStatus.Connected) {
                    status = ModuleStatus.Disconnected;
                    Extensions.ExcecuteWithAppIdleDispatcher(() => OnModuleConnectionChange?.Invoke(ModuleStatus.Disconnected));
                }
            }

            Extensions.ExcecuteWithAppIdleDispatcher(() => OnNewPortNames?.Invoke(SerialPort.GetPortNames().ToList()));
        }
    }

    public enum ModuleStatus {
        Connected,
        Disconnected
    }
}
