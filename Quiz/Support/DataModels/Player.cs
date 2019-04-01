using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Quiz.Support.DataModels
{
    public class Player : INotifyPropertyChanged
    {
        public int PlayerIndex { get; private set; }
        public string Name { get; set; }
        public int Status { get; private set; }
        public int Points {
            get { return _points; }
            set {
                _points = value;
                OnPropertyChanged("Points");
            }
        }
        public double PlayerBarHeight {
            get { return _playerBarHeight; }
            set {
                _playerBarHeight = value;
                OnPropertyChanged("PlayerBarHeight");
            }
        }
        public SolidColorBrush PlayerBrush { get; }
        public SolidColorBrush StatusBrush { get; private set; }

        private int _points;
        private double _playerBarHeight;

        private Color _disconnectColor = Color.FromRgb(206, 2, 2);
        private Color _connectingColor = Color.FromRgb(232, 216, 0);
        private Color _connectedColor = Color.FromRgb(2, 206, 129);
        private Color _transparentColor = Color.FromArgb(0, 0, 0, 0);

        private Timer _flickTimer;

        /// <summary>
        /// Creatre a new player
        /// </summary>
        /// <param name="name">Name of player</param>
        /// <param name="c">Color of player bars</param>
        /// <param name="pIndex">Index of player</param>
        /// <param name="maxPoint">Max points value</param>
        /// <param name="pointBarsContainerWidth">Width of container with pointBars</param>
        public Player(string name, Color c, int pIndex) {
            PlayerBarHeight = 0;
            Points = 0;
            
            PlayerIndex = pIndex;
            Name = name;

            PlayerBrush = new SolidColorBrush(c);
            StatusBrush = new SolidColorBrush(_transparentColor);

            _flickTimer = new Timer(Flick, null, Timeout.Infinite, 0);
            ChangeStatus(0);
        }

        public void ChangeStatus(int newStatus) {
            Status = newStatus;
            switch (newStatus) {
                case 0:
                    StatusBrush.Color = _disconnectColor;
                    _flickTimer.Change(Timeout.Infinite, 0);
                    break;
                case 1:
                    StatusBrush.Color = _transparentColor;
                    _flickTimer.Change(0, 500);
                    break;
                case 2:
                    StatusBrush.Color = _connectedColor;
                    _flickTimer.Change(Timeout.Infinite, 0);
                    break;
            }
        }

        private void Flick(object o) {
            try {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    if (StatusBrush.Color == _transparentColor) {
                        StatusBrush.Color = _connectingColor;
                    } else {
                        StatusBrush.Color = _transparentColor;
                    }
                }), DispatcherPriority.Normal);
            } catch { }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
