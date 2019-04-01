using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SimpleWifi;

namespace Quiz.Support
{
    class WiFiWorker
    {
        private const string SSID = "QuizModuleAP";
        private const int RECONNECT_PERIOD = 3000;
        private const int CHECK_CHANGES_PERIOD = 7000;

        private Wifi wifi;
        private AccessPoint moduleAP;

        private Timer reconnectTimer;
        private Timer checkChangeTimer;

        private string WiFiPass = "123456789";
        private int wifiSignalStatus;

        private bool isReconnectTimerStarted = false;
        private bool isNetworkExist = false;

        public event Action<int> OnStatusChanged;

        public void Init() {
            wifi = new Wifi();

            InitTimers();
        }

        public void StartWorker() {
            Thread workerThread = new Thread(Worker);
            workerThread.Start();
        }

        public void Worker() {
            StartReconnectTimer();
            StartCheckChangesTimer();
        }

        private bool Search() {
            try {
                foreach (AccessPoint point in wifi.GetAccessPoints()) {
                    if (point.Name == SSID) {
                        moduleAP = point;
                        isNetworkExist = true;
                        return true;
                    } 
                }
                isNetworkExist = false;
                return false;    
            }
            catch {
                isNetworkExist = false;
                return false;
            }  
        }

        private void TryConnect(object state) {
            if (!Search()) { return; }

            AuthRequest request = new AuthRequest(moduleAP);
            request.Password = WiFiPass;

            if (moduleAP.Connect(request)) {
                StopReconnectTimer();
            }
        }

        private void CheckConnection(object state) {
            if (!isNetworkExist) { return; }

            int currentStatus = 0;

            try {
                if (wifi.ConnectionStatus == WifiStatus.Connected) {
                    if (wifi.GetAccessPoints().Where(ap => ap.IsConnected).ToList()[0].Name == SSID) {
                        currentStatus = (Convert.ToInt32(moduleAP.SignalStrength) - 1) / 25 + 1;
                    }
                }
            } catch { }

            if (currentStatus != wifiSignalStatus) {
                wifiSignalStatus = currentStatus;
                OnStatusChanged.Invoke(currentStatus);
            }

            if (currentStatus == 0) {
                if (!isReconnectTimerStarted) {
                    StartReconnectTimer();
                }
            }
        }

        #region TimerMethods

        private void InitTimers() {
            reconnectTimer = new Timer(TryConnect, null, Timeout.Infinite, RECONNECT_PERIOD);
            checkChangeTimer = new Timer(CheckConnection, null, Timeout.Infinite, CHECK_CHANGES_PERIOD);
        }

        private void StartReconnectTimer() {
            reconnectTimer.Change(0, RECONNECT_PERIOD);
            isReconnectTimerStarted = true;
        }

        private void StopReconnectTimer() {
            reconnectTimer.Change(Timeout.Infinite, 0);
            isReconnectTimerStarted = false;
        }

        private void StartCheckChangesTimer() {
            checkChangeTimer.Change(0, CHECK_CHANGES_PERIOD);
        }

        private void StopCheckChangeTimer() {
            checkChangeTimer.Change(Timeout.Infinite, 0);
        }

        #endregion

    }
}
