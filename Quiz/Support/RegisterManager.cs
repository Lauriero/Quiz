using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Quiz.Support
{
    class RegistrationManager
    {
        private ButtonModuleConnector connector;
        private List<int> registrationOrder;
        private Thread workerThread;

        public event Action<int, RegistrationStatus, int> OnRegistrationChanged;

        public void Init(ButtonModuleConnector connector)
        {
            this.connector = connector;
            registrationOrder = new List<int>();

            workerThread = new Thread(RegistrationWorker);
        }

        public void Register(int index)
        {
            registrationOrder.Add(index);
        }

        private void RegistrationWorker()
        {
            while (true) {
                if (registrationOrder.Count == 0) {
                    Thread.Sleep(1000);
                    continue;
                }

                for (int i = 0; i < registrationOrder.Count; ++i) {
                    SendData(registrationOrder[i], RegistrationStatus.Registrating, -1);

                    int buttonIndex = connector.GetButtonClick();
                    if (buttonIndex == -1) {
                        SendData(registrationOrder[i], RegistrationStatus.Disable, -1);
                    } else {
                        SendData(registrationOrder[i], RegistrationStatus.Registered, buttonIndex);
                    }

                    registrationOrder.RemoveAt(i);
                }
            }
        }

        private void SendData(int pI, RegistrationStatus s, int bI) {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                OnRegistrationChanged.Invoke(pI, s, bI);
            }), DispatcherPriority.ApplicationIdle);
        }

        public void Start() {
            workerThread.Start();
        }

        public void Stop() {
            workerThread.Abort();
        }


        public enum RegistrationStatus {
            Registered,
            Registrating,
            Disable
        }
    }
}
