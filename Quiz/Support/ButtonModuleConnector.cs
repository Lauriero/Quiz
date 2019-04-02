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
    class ButtonModuleConnector
    {
        private const string MODULE_SETCOMMAND_URL = "http://192.168.4.1/getButton";
        private const string MODULE_GETDATA_URL = "http://192.168.4.1/getData";

        private WebClient client;

        public void Init() {
            client = new WebClient();
        }

        public int GetButtonClick() {
            using (client) {
                client.DownloadString(new Uri(MODULE_SETCOMMAND_URL));

                int buttonIndex = -1;
                string response = "";
                while (response == "") {
                    response = client.DownloadString(new Uri(MODULE_GETDATA_URL));
                }

                buttonIndex = int.Parse(response);

                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    MessageBox.Show(buttonIndex.ToString());
                }), DispatcherPriority.ApplicationIdle);

                return buttonIndex;
            }
        }

        public void SendRequest() {

            
        }
    }
}
