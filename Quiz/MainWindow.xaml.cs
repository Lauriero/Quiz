using Quiz.Support;
using Quiz.Support.DataModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Quiz
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int MAX_PLAYERS_COUNT = 10;

        #region Public Global Variables

        public ObservableCollection<Player> Players { get; set; }
        public int RoundNumber { get; set; }
        public double PlayerBarHeight
        {
            get { return _playerBarHeight; }
            set {
                _playerBarHeight = value;
                OnPropertyChanged("PlayerBarHeight");
            }
        }
        public Thickness PlayerNameMargin
        {
            get { return _playerNameMargin; }
            set {
                _playerNameMargin = value;
                OnPropertyChanged("PlayerNameMargin");
            }
        }

        #endregion

        #region Colors

        private List<Color> defaultColors = new List<Color>() {
            Color.FromRgb(17, 47, 65), Color.FromRgb(6, 133, 135), Color.FromRgb(79, 185, 159),
            Color.FromRgb(242, 177, 52), Color.FromRgb(237, 85, 59), Color.FromRgb(64, 21, 42), Color.FromRgb(2, 206, 129)
        };

        private SolidColorBrush transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        private SolidColorBrush white = new SolidColorBrush(Color.FromArgb(1, 255, 255, 255));

        #endregion

        private int playersCount = 0;
        private int maxPoint = 20;

        private double pointBarWidthStep = 0;
        private double pointBarsContainerWidth = 0;
        private double playersNameContainerHeight = 0;
        private double _playerBarHeight;
        private Thickness _playerNameMargin;

        private ButtonModuleConnector buttonConnector;
        private RegistrationManager registrationManager;

        public MainWindow()
        {
            RoundNumber = 1;

            Players = new ObservableCollection<Player>();
            this.DataContext = this;

            InitializeComponent();

            playersNameContainerHeight = (SystemParameters.PrimaryScreenHeight - 25) / 10 * 9;
            pointBarsContainerWidth = SystemParameters.PrimaryScreenWidth / 13.31 * 11 - 15; //-15 - distance between border and bar

            pointBarWidthStep = pointBarsContainerWidth / maxPoint;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            buttonConnector = new ButtonModuleConnector();
            buttonConnector.Init();

            registrationManager = new RegistrationManager();
            registrationManager.Init(buttonConnector);
            registrationManager.OnRegistrationChanged += RegistrationManager_OnRegistrationChanged;

            WiFiWorker worker = new WiFiWorker();
            worker.Init();
            worker.OnStatusChanged += ChangeWiFiSignal;
            worker.StartWorker();

            AddPlayer();
            AddPlayer();

            MediaBlock.LoadedBehavior = MediaState.Manual;
            MediaBlock.UnloadedBehavior = MediaState.Manual;

            MediaBlock.Source = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, "Question1.mp4"));
            MediaBlock.Volume = 0.5f;
            //MediaBlock.Play();

            //_manager = new QuestionManager(_questions, _groupsTables, NewResponding, IncreasePoint, questionsBlock, questionNumber);
            //_manager.Start();
        }

        private void RegistrationManager_OnRegistrationChanged(int playerIndex, RegistrationManager.RegistrationStatus status, int buttonIndex)
        {
            switch (status) {
                case RegistrationManager.RegistrationStatus.Disable:
                    {
                        Players[playerIndex].ChangeStatus(0);
                        break;
                    }
                case RegistrationManager.RegistrationStatus.Registered:
                    {
                        Players[playerIndex].ChangeStatus(2);
                        Players[playerIndex].ButtonIndex = buttonIndex;
                        break;
                    }
                case RegistrationManager.RegistrationStatus.Registrating:
                    {
                        Players[playerIndex].ChangeStatus(1);
                        break;
                    }
            }
        }

        #region PlayersSupportMethods

        private void AddPlayer() {

            if (playersCount == MAX_PLAYERS_COUNT - 1) {
                AddPlayerBtn.Visibility = Visibility.Collapsed;
            }

            if (playersCount < defaultColors.Count) {
                Players.Add(new Player(string.Format("Игрок {0}", playersCount + 1), defaultColors[playersCount], playersCount));
            } else {
                Players.Add(new Player(string.Format("Игрок {0}", playersCount + 1), Color.FromRgb(48, 59, 63), playersCount));
            }

            registrationManager.Register(playersCount);

            playersCount++;

            PlayerBarHeight = playersNameContainerHeight / playersCount;
            PlayerNameMargin = new Thickness(0);
            if (PlayerBarHeight > 100) {
                PlayerBarHeight = 100;
                PlayerNameMargin = new Thickness(0, 0, 0, 6);
            }
        }

        private void AddPoints(int playerIndex, int points) {
            double newPointBarWidth = (Players[playerIndex].Points += points) * pointBarWidthStep;

            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                Grid g = GetElementFromItemsControl<Grid>(PointBarsContainer, playerIndex, "PointBarGrid");

                DoubleAnimation animation = new DoubleAnimation();
                animation.From = g.Width;
                animation.To = newPointBarWidth;
                animation.AccelerationRatio = 0.6;
                animation.Duration = TimeSpan.FromMilliseconds(200);

                g.BeginAnimation(Grid.WidthProperty, animation);
            }), DispatcherPriority.ApplicationIdle);
        }

        #endregion

        #region SupportEventsListeners

        private void ChangeWiFiSignal(int signal)
        {
            ResourceDictionary dict = new ResourceDictionary();
            dict.Source = new Uri("Resources/Images/WiFi.xaml", UriKind.Relative);

            switch (signal) {
                case 0:
                    {
                        registrationManager.Stop();
                        break; 
                    }
                case 1:
                    {
                        registrationManager.Start();
                        dict["FirstWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                        dict["SecondWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                        dict["ThirdWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                        break;
                    }
                case 2:
                    {
                        registrationManager.Start();
                        dict["FirstWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                        dict["SecondWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                        dict["ThirdWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        break;
                    }
                case 3:
                    {
                        registrationManager.Start();
                        dict["FirstWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                        dict["SecondWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        dict["ThirdWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        break;
                    }
                case 4:
                    {
                        registrationManager.Start();
                        dict["FirstWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        dict["SecondWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        dict["ThirdWiFiStickBrush"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        break;
                    }
            }

            Application.Current.Resources.MergedDictionaries[3] = dict;
        }

        #endregion

        #region SupportMethods

        /// <summary>
        /// Return item from ItemsControl
        /// </summary>
        /// <typeparam name="T">Type of element</typeparam>
        /// <param name="itemsCollection">Container of element</param>
        /// <param name="index">Index of element in container</param>
        /// <param name="elementName">Name of element in template</param>
        /// <returns></returns>
        private T GetElementFromItemsControl<T>(ItemsControl itemsCollection, int index, string elementName) where T : class
        {
            ContentPresenter cp = itemsCollection.ItemContainerGenerator.ContainerFromIndex(index) as ContentPresenter;
            DataTemplate template = cp.ContentTemplate;

            return template.FindName(elementName, cp) as T;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        #region ControlsListeners

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void HideWindowButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void SquareElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            (sender as Border).Width = e.NewSize.Height;
        }

        private void AddPlayerBtn_Click(object sender, RoutedEventArgs e)
        {
            AddPlayer();
        }

        private void StartButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ColorAnimation blackerAnimation = new ColorAnimation();
            blackerAnimation.From = (blacker.Background as SolidColorBrush).Color;
            blackerAnimation.To = Color.FromArgb(0, 0, 0, 0);
            blackerAnimation.Duration = TimeSpan.FromMilliseconds(300);
            blackerAnimation.AccelerationRatio = 0.6;

            DoubleAnimation settingsHeightAnimation = new DoubleAnimation();
            settingsHeightAnimation.From = SettingsBorder.ActualHeight;
            settingsHeightAnimation.To = 0;
            settingsHeightAnimation.Duration = TimeSpan.FromMilliseconds(100);
            settingsHeightAnimation.AccelerationRatio = 0.3;

            (blacker.Background as SolidColorBrush).BeginAnimation(SolidColorBrush.ColorProperty, blackerAnimation);
            SettingsBorder.BeginAnimation(HeightProperty, settingsHeightAnimation);

            AddPoints(1, 2);
            AddPoints(1, 2);

            foreach (Player p in Players) {
                MessageBox.Show(p.ButtonIndex.ToString());
            }
        } 

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsBorder.Height != 0) { return; }

            ColorAnimation blackerAnimation = new ColorAnimation();
            blackerAnimation.From = Color.FromArgb(0, 0, 0, 0);
            blackerAnimation.To = Color.FromArgb(174, 0, 0, 0);
            blackerAnimation.Duration = TimeSpan.FromMilliseconds(300);
            blackerAnimation.AccelerationRatio = 0.6;

            DoubleAnimation settingsHeightAnimation = new DoubleAnimation();
            settingsHeightAnimation.From = 0;
            settingsHeightAnimation.To = (SystemParameters.PrimaryScreenHeight - 30) / 2 ;
            settingsHeightAnimation.Duration = TimeSpan.FromMilliseconds(100);
            settingsHeightAnimation.AccelerationRatio = 0.3;

            (blacker.Background as SolidColorBrush).BeginAnimation(SolidColorBrush.ColorProperty, blackerAnimation);
            SettingsBorder.BeginAnimation(HeightProperty, settingsHeightAnimation);

            StartButton.Text = "Продолжить";
            FooterGrid.ColumnDefinitions[0].Width = new GridLength(2.1, GridUnitType.Star);
        }

        #endregion
    }
}
