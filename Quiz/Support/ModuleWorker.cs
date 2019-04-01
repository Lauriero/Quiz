using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Windows;
using System.Windows.Threading;

namespace Quiz.Support
{
    class ModuleWorker
    {
        private const string MODULE_URL = "http://192.168.4.1/";

        private WebClient client;

        public event Action<int> OnButtonClicked;

        public void Init() {
            client = new WebClient();
        }

        public void GetButtonClick() {
            Thread thread = new Thread(SendRequest);
            thread.Start();
        }

        private void SendRequest() {
            using (client) {
                string response = client.DownloadString(new Uri(MODULE_URL));

                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    MessageBox.Show(response);
                }), DispatcherPriority.ApplicationIdle);
            }
        }
    }
}
