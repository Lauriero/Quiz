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
        public int ButtonIndex { get; set; }
        public int PlayerIndex { get; private set; }
        public bool isAnswered { get; set; }
        public string Name {
            get { return _name; }
            set {
                _name = value;
                OnPropertyChanged("Name");
            }
        }
        public string AnswerTime {
            get { return _answerTime; }
            set {
                _answerTime = value;
                OnPropertyChanged("AnswerTime");
            }
        }
        public int Points {
            get { return _points; }
            set {
                _points = value;
                OnPropertyChanged("Points");
            }
        }
        public string ExtraPoints {
            get { return _extraPoints; }
            set {
                _extraPoints = value;
                OnPropertyChanged("ExtraPoints");
            }
        }
        public CornerRadius PointsBarCornerRadius {
            get { return _pointsBarCornerRadius; }
            set {
                _pointsBarCornerRadius = value;
                OnPropertyChanged("PointsBarCornerRadius");
            }
        }
        public double ExtraPointsBlockWidth {
            get { return _extraPointsBlockWidth; }
            private set {
                _extraPointsBlockWidth = value;
                OnPropertyChanged("ExtraPointsBlockWidth");
            }
        }
        public Visibility ExtraPointsBlockVisibility {
            get { return _extraPointsBlockVisibility; }
            set {
                _extraPointsBlockVisibility = value;
                OnPropertyChanged("ExtraPointsBlockVisibility");
            }
        }
        public PlayerStatus Status { get; private set; }
        public SolidColorBrush PlayerBrush { get; }
        public SolidColorBrush StatusBrush {
            get { return _sBrush; }
            set {
                _sBrush = value;
                OnPropertyChanged("StatusBrush");
            }
        }

        private int _points;
        private double _extraPointsBlockWidth;
        private string _extraPoints;
        private string _name;
        private string _answerTime;
        private CornerRadius _pointsBarCornerRadius;
        private SolidColorBrush _sBrush;
        private Visibility _extraPointsBlockVisibility;

        private double _extraPointsBarStep;

        private Color _disconnectColor = Color.FromRgb(206, 2, 2);
        private Color _connectingColor = Color.FromRgb(232, 216, 0);
        private Color _connectedColor = Color.FromRgb(2, 206, 129);
        private Color _transparentColor = Color.FromArgb(0, 0, 0, 0);

        private Timer _flickTimer;
        private Timer _flickExtraBartimer;

        /// <summary>
        /// Creatre a new player
        /// </summary>
        /// <param name="name">Name of player</param>
        /// <param name="c">Color of player bars</param>
        /// <param name="pIndex">Index of player</param>
        /// <param name="pointsBlockStep">Width step on 1 point for block</param>
        public Player(string name, Color c, int pIndex, double pointsBlockStep) {
            ExtraPointsBlockVisibility = Visibility.Collapsed;
            PointsBarCornerRadius = new CornerRadius(0, 7, 7, 0);
            ExtraPoints = "";

            ButtonIndex = -1;
            Points = 0;
            
            PlayerIndex = pIndex;
            Name = name;

            _extraPointsBarStep = pointsBlockStep;

            PlayerBrush = new SolidColorBrush(c);
            StatusBrush = new SolidColorBrush(_transparentColor);

            _flickTimer = new Timer(Flick, null, Timeout.Infinite, 0);
            _flickExtraBartimer = new Timer(BarFlick, null, Timeout.Infinite, 0);
            ChangeStatus(0);
        }

        /// <summary>
        /// Change player status
        /// </summary>
        /// <param name="newStatus">Status</param>
        /// <param name="extraPoints">Points when extra points bar shows</param>
        public void ChangeStatus(PlayerStatus newStatus, int extraPoints = 0, long answerMilliseconds = 0) {  
            if (newStatus != PlayerStatus.Answering && Status == PlayerStatus.Answering) {
                ExtraPointsBlockWidth = 0;
                ExtraPointsBlockVisibility = Visibility.Collapsed;
                PointsBarCornerRadius = new CornerRadius(0, 7, 7, 0);
                ExtraPoints = "";
                _flickExtraBartimer.Change(Timeout.Infinite, 0);
            }
            Status = newStatus;

            switch (newStatus) {
                case PlayerStatus.Disable:
                    {
                        StatusBrush.Color = _disconnectColor;
                        _flickTimer.Change(Timeout.Infinite, 0);
                        break;
                    }
                case PlayerStatus.Registrating:
                    {
                        StatusBrush.Color = _transparentColor;
                        _flickTimer.Change(0, 500);
                        break;
                    }
                case PlayerStatus.Registered:
                    {
                        StatusBrush.Color = _connectedColor;
                        _flickTimer.Change(Timeout.Infinite, 0);
                        break;
                    }
                case PlayerStatus.Answering:
                    {
                        ExtraPointsBlockWidth = _extraPointsBarStep * extraPoints;
                        ExtraPointsBlockVisibility = Visibility.Visible;
                        PointsBarCornerRadius = new CornerRadius(0, 0, 0, 0);
                        AnswerTime = TimeSpan.FromMilliseconds(answerMilliseconds).ToString(@"mm\:ss\:ff");
                        ExtraPoints = string.Format("+{0}", extraPoints);
                        _flickExtraBartimer.Change(0, 400);
                        break;
                    }
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

        private void BarFlick(object o) {
            try {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    if (ExtraPointsBlockVisibility == Visibility.Visible) {
                        ExtraPointsBlockVisibility = Visibility.Collapsed;
                    } else {
                        ExtraPointsBlockVisibility = Visibility.Visible;
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

    public enum PlayerStatus {
        Disable,
        Registrating,
        Registered,
        Answering
    }
}
