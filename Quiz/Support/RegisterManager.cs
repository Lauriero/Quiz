using Quiz.Support.DataModels;
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

        public event Action OnPlayerDisable;
        public event Action<int> OnPlayerRegistrated;

        private Thread workerThread;

        public void Init(ButtonModuleConnector connector)
        {
            this.connector = connector;
        }

        public void RegisterNext()
        {
            workerThread = new Thread(RegistrationWorker);
            workerThread.Start();
        }

        public void StopManager()
        {
            connector.AbortListener();
        }

        private void RegistrationWorker()
        {
            int buttonIndex = connector.GetButtonClick();
            if (buttonIndex == -1) {
                Extensions.ExcecuteWithAppIdleDispatcher(() => OnPlayerDisable?.Invoke());
            } else if (buttonIndex == -2) {
                return;
            } else {
                Extensions.ExcecuteWithAppIdleDispatcher(() => OnPlayerRegistrated?.Invoke(buttonIndex));
            }
        }
    }
}
