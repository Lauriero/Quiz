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
        private const string MODULE_RESTART_URL = "http://192.168.4.1/restart";

        private bool isWorkerActive = false;

        private WebClient client;

        public void Init() {
            client = new WebClient();
        }

        public int GetButtonClick() {
            isWorkerActive = true;

            using (client) {
                client.DownloadString(new Uri(MODULE_SETCOMMAND_URL));

                int buttonIndex = -1;
                string response = "";
                while (response == "") {
                    if (!isWorkerActive) {
                        response = "-1";
                        client.DownloadString(new Uri(MODULE_RESTART_URL));
                        break;
                    } 
                    response = client.DownloadString(new Uri(MODULE_GETDATA_URL));
                }

                buttonIndex = int.Parse(response);
                return buttonIndex;
            }
        }

        public void AbortConnection() {
            isWorkerActive = false;
        }
    }
}
