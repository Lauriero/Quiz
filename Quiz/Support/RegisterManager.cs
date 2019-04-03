using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private ObservableCollection<int> registrationOrder;
        private List<int> temp;
        private Thread workerThread;

        bool isManagerActive = false;

        public event Action<int, RegistrationStatus, int> OnRegistrationChanged;

        public void Init(ButtonModuleConnector connector)
        {
            this.connector = connector;

            registrationOrder = new ObservableCollection<int>();
            registrationOrder.CollectionChanged += RegistrationOrder_CollectionChanged;

            workerThread = new Thread(RegistrationWorker);
            workerThread.Start();
        }

        private void RegistrationOrder_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add) {
                if (!isManagerActive) {
                    temp.Add((int)e.NewItems[0]);
                }

                Extensions.ExecuteInApplicationThread(() => OnRegistrationChanged?.Invoke((int)e.NewItems[0], RegistrationStatus.Registrating, -1));

                int buttonIndex = connector.GetButtonClick();
                if (buttonIndex == -1) {
                    Extensions.ExecuteInApplicationThread(() => OnRegistrationChanged?.Invoke((int)e.NewItems[0], RegistrationStatus.Disable, -1));
                } else {
                    Extensions.ExecuteInApplicationThread(() => OnRegistrationChanged?.Invoke((int)e.NewItems[0], RegistrationStatus.Registered, buttonIndex));
                }

                Extensions.ExecuteInApplicationThread(() => MessageBox.Show(buttonIndex.ToString()));
                registrationOrder.Remove((int)e.NewItems[0]);
            }
        }

        public void Register(int index)
        {
            registrationOrder.Add(index);
        }

        public void StopManager() {
            isManagerActive = false;
            connector.AbortConnection();
        }

        public void StartManager() {
            isManagerActive = true;
        }

        private void RegistrationWorker() {
            while (true) {
                if (!isManagerActive || temp.Count == 0) {
                    Thread.Sleep(50);
                    continue;
                }

                int buttonIndex = connector.GetButtonClick();
                if (buttonIndex == -1) {
                    Extensions.ExecuteInApplicationThread(() => OnRegistrationChanged?.Invoke(temp[0], RegistrationStatus.Disable, -1));
                } else {
                    Extensions.ExecuteInApplicationThread(() => OnRegistrationChanged?.Invoke(temp[0], RegistrationStatus.Registered, buttonIndex));
                }

                temp.RemoveAt(0);
            }
        }

    }

    public enum RegistrationStatus
    {
        Registered,
        Registrating,
        Disable
    }
}
