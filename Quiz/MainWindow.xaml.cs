using Quiz.Support;
using Quiz.Support.DataModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
        public ObservableCollection<string> SerialPorts { get; set; }
        public int RoundNumber {
            get { return _roundNumber; }
            set {
                _roundNumber = value;
                OnPropertyChanged("RoundNumber");
            }
        }
        public string RightAnswer {
            get { return _rightAnswer; }
            set {
                _rightAnswer = value;
                OnPropertyChanged("RightAnswer");
            }
        }
        public double PlayerBarHeight {
            get { return _playerBarHeight; }
            set {
                _playerBarHeight = value;
                OnPropertyChanged("PlayerBarHeight");
            }
        }
        public int QuestionNumber {
            get { return _questionNumber; }
            set {
                _questionNumber = value;
                OnPropertyChanged("QuestionNumber");
            }
        }
        public string MainWindowQuestion {
            get { return _mainWindowQuestion; }
            set {
                _mainWindowQuestion = value;
                OnPropertyChanged("MainWindowQuestion");
            }
        }

        public string QuestionText {
            get { return _questionString; }
            set {
                _questionString = value;
                OnPropertyChanged("QuestionText");
            }
        }
        public string AnswerTimerText {
            get { return _questionAnswerTime; }
            set {
                if (!isTimerStarted) {
                    _questionAnswerTime = string.Format(":{0}", value);
                    AnswerTimeFontSize = 33;
                } else {
                    AnswerTimeFontSize = 26;
                    _questionAnswerTime = value;
                }

                OnPropertyChanged("AnswerTimeFontSize");
                OnPropertyChanged("AnswerTimerText");
            }
        }
        public int QuestionNumberFontSize {
            get { return _questionNumberFontSize; }
            set {
                _questionNumberFontSize = value;
                OnPropertyChanged("QuestionNumberFontSize");
            }
        }
        public int QuestionTextFontSize {
            get { return _questionTextFontSize; }
            set {
                _questionTextFontSize = value;
                OnPropertyChanged("QuestionTextFontSize");
            }
        }
        public int AnswerTimeFontSize {
            get { return _answerTimeFontSize; }
            set {
                _answerTimeFontSize = value;
                OnPropertyChanged("AnswerTimeFontSize");
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

        public Player ActivePlayer { 
            get { return _activePlayer; }
            set {
                _activePlayer = value;

                if(value == null) {
                    OnPropertyChanged("ActivePlayer");
                    return;
                }

                if (value.Points == 0) {
                    ActivePlayerPointsBarFontSize = 50;
                } else {
                    ActivePlayerPointsBarFontSize = 60;
                }

                OnPropertyChanged("ActivePlayer");
            }
        }
        public int ActivePlayerPointsBarFontSize {
            get { return _activePlayerPointsBarFontSize; }
            set {
                _activePlayerPointsBarFontSize = value;
                OnPropertyChanged("ActivePlayerPointsBarFontSize");
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
        private int registratingPlayerCounter = 0;
        private int activePlayerIndex = -1;
        private int maxPoint = 30;

        private bool isRegistrationActive = false;
        private bool isModuleConnect = false;
        private bool isBlackerShowen = true;
        private bool isQuizStarted = false;
        private bool isTimerStarted = false;
        private bool isPlayerAnswering = false;
        private bool isVideoPlay = false;
        private bool isVideoQuestion = false;
        private bool isRightAnswerVideoPlay = false;
        private bool isVideoRightAnswer = false;
        private bool isAddPointsWindowOpened = false;
        private bool isAddingPointNecessary = false;
    
        private double pointBarWidthStep = 0;
        private double pointBarsContainerWidth = 0;
        private double playersNameContainerHeight = 0;

        private double _playerBarHeight;
        private int _roundNumber;
        private int _questionNumber;
        private int _questionNumberFontSize;
        private int _questionTextFontSize;
        private int _answerTimeFontSize;
        private int _activePlayerPointsBarFontSize;
        private string _questionString;
        private string _mainWindowQuestion;
        private string _questionAnswerTime;
        private string _rightAnswer;
        private Player _activePlayer;
        private Thickness _playerNameMargin;
        private TimeSpan _answersTime;
        private Timer _answerSecondsTimer;

        private UIElement closedElement;

        private DataBaseWorker dbWorker;
        private ButtonModuleConnector buttonConnector;
        private RegistrationManager registrationManager;
        private QuizManager quizManager;

        public MainWindow()
        {
            ActivePlayer = new Player();

            RoundNumber = 1;
            QuestionNumberFontSize = 33;
            QuestionTextFontSize = 30;

            Players = new ObservableCollection<Player>();
            SerialPorts = new ObservableCollection<string>();
            this.DataContext = this;

            InitializeComponent();

            SerialPorts.Add("COM4");
            SerialBox.SelectedIndex = 0;

            playersNameContainerHeight = (SystemParameters.PrimaryScreenHeight - 25) / 10 * 9;
            pointBarsContainerWidth = SystemParameters.PrimaryScreenWidth / 13.31 * 11 - 15; //-15 - distance between border and bar

            pointBarWidthStep = pointBarsContainerWidth / maxPoint;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            buttonConnector = new ButtonModuleConnector();
            buttonConnector.OnModuleConnectionChange += ButtonConnector_OnModuleConnectionChange;
            buttonConnector.OnNewPortNames += ButtonConnector_OnNewPortNames;
            buttonConnector.PortName = "COM4";
            buttonConnector.Init();

            registrationManager = new RegistrationManager();
            registrationManager.Init(buttonConnector);
            registrationManager.OnPlayerDisable += RegistrationManager_OnPlayerDisable;
            registrationManager.OnPlayerRegistrated += RegistrationManager_OnPlayerRegistrated;

            dbWorker = new DataBaseWorker();
            dbWorker.Init();
            QuizInfo playersInfo = dbWorker.GetFullInfo();

            RoundNumber = playersInfo.RoundIndex;

            quizManager = new QuizManager();
            quizManager.OnNewQuestion += QuizManager_OnNewQuestion;
            quizManager.OnPlayerButtonClicked += QuizManager_OnPlayerButtonClicked;
            quizManager.OnRightAnswer += QuizManager_OnRightAnswer;
            quizManager.OnWrongAnswer += QuizManager_OnWrongAnswer;
            quizManager.OnRoundEnd += QuizManager_OnRoundEnd;
            quizManager.Init(dbWorker.GetQuestions(), buttonConnector);

            if (playersInfo.PlayersNames.Count == 0) {
                AddPlayer();
                AddPlayer();
            } else {
                for (int i = 0; i < playersInfo.PlayersNames.Count; ++i) {
                    AddPlayer(playersInfo.PlayersNames[i]);
                    AddPoints(i, playersInfo.PlayersPoints[i]);
                }
            }

            MediaBlock.LoadedBehavior = MediaState.Manual;
            MediaBlock.UnloadedBehavior = MediaState.Manual;
            MediaBlock.Volume = 0.5f;

            RightAnswerMediaBlock.LoadedBehavior = MediaState.Manual;
            RightAnswerMediaBlock.UnloadedBehavior = MediaState.Manual;
            RightAnswerMediaBlock.Volume = 0.5f;

            Sound.LoadedBehavior = MediaState.Manual;
            Sound.UnloadedBehavior = MediaState.Manual;
            Sound.Volume = 0.5f;

            Sound.Stop();
        }

        private void QuizManager_OnRoundEnd()
        {
            double p1 = Players[0].Points;
            double p2 = Players[1].Points;
            double p3 = Players[2].Points;

            if (!(p1 != p2 && p2 != p3 && p1 != p3)) {
                MediaDockPanel.Visibility = Visibility.Collapsed;
                quizManager.AddExtraQuestion();
                quizManager.Next();
                return;
            }   

            if (dbWorker.CheckContinueRound()) {

                dbWorker.UpdateCurrentRound(RoundNumber + 1);
                dbWorker.UpdateCurrentQuestion(0);

                Thread thread = new Thread(() => {
                    Process.Start(Path.Combine(Environment.CurrentDirectory, "Quiz.exe"));
                });
                thread.Start();

                quizManager.StopQuiz();
                registrationManager.StopManager();
                buttonConnector.AbortAll();

                this.Close();
            } else {
                MessageBox.Show("Игра окончена.");

                quizManager.StopQuiz();
                registrationManager.StopManager();
                buttonConnector.AbortAll();
                this.Close();
            }
        }

        #region PlayersSupportMethods

        /// <summary>
        /// Add the new player
        /// </summary>
        private void AddPlayer(string playerName = "Игрок {0}") {

            if (playersCount == MAX_PLAYERS_COUNT - 1) {
                AddPlayerBtn.Visibility = Visibility.Collapsed;
            }

            if (playersCount == 0) {
                Players.Add(new Player(string.Format(playerName, playersCount + 1), Color.FromRgb(79, 185, 159), playersCount, pointBarsContainerWidth / maxPoint));
            } else if (playersCount == 1) {
                Players.Add(new Player(string.Format(playerName, playersCount + 1), Color.FromRgb(242, 177, 52), playersCount, pointBarsContainerWidth / maxPoint));
            } else if (playersCount == 2) {
                Players.Add(new Player(string.Format(playerName, playersCount + 1), Color.FromRgb(237, 85, 89), playersCount, pointBarsContainerWidth / maxPoint));
            }

            if (playersCount < defaultColors.Count) {
                
            } else {
                Players.Add(new Player(string.Format(playerName, playersCount + 1), Color.FromRgb(48, 59, 63), playersCount, pointBarsContainerWidth / maxPoint));
            }

            if (!isRegistrationActive && isModuleConnect) {
                isRegistrationActive = true;
                Players.Last().ChangeStatus(PlayerStatus.Registrating);
                registrationManager.RegisterNext();
            }

            playersCount++;

            PlayerBarHeight = playersNameContainerHeight / playersCount;
            PlayerNameMargin = new Thickness(0);
            if (PlayerBarHeight > 140) {
                PlayerBarHeight = 140;

                double margins = (playersNameContainerHeight - playersCount * PlayerBarHeight) / ((playersCount - 1) * 2 + 2);
                PlayerNameMargin = new Thickness(0, margins, 0, margins);

            }
        }

        /// <summary>
        /// Add points to player
        /// </summary>
        /// <param name="playerIndex">Index of player</param>
        /// <param name="points">Adding points</param>
        private void AddPoints(int playerIndex, double points) {
            double newPointBarWidth = (Players[playerIndex].Points += points) * pointBarWidthStep;

            DoubleAnimation animation = new DoubleAnimation();

            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                Grid g = GetElementFromItemsControl<Grid>(PointBarsContainer, playerIndex, "PointBarGrid");

                animation.From = g.Width;
                animation.To = newPointBarWidth;
                animation.AccelerationRatio = 0.6;
                animation.Duration = TimeSpan.FromMilliseconds(200);

                g.BeginAnimation(WidthProperty, animation);
            }), DispatcherPriority.ApplicationIdle);

            animation.From = RightPlayerPointsBar.Width;
            animation.To = newPointBarWidth;
            animation.AccelerationRatio = 0.6;
            animation.Duration = TimeSpan.FromMilliseconds(200);

            RightPlayerPointsBar.BeginAnimation(WidthProperty, animation);
        }

        #endregion

        #region SupportEventsListeners

        private void ButtonConnector_OnModuleConnectionChange(ModuleStatus status)
        {
            ResourceDictionary dict = new ResourceDictionary();
            dict.Source = new Uri("Resources/Images/Module.xaml", UriKind.Relative);

            switch (status) {
                case ModuleStatus.Disconnected:
                    {
                        isModuleConnect = false;
                        isRegistrationActive = false;
                        registrationManager.StopManager();

                        dict["BranchesColor"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                        break;
                    }
                case ModuleStatus.Connected:
                    {
                        isModuleConnect = true;
                        registratingPlayerCounter = Players.Where(p => p.Status == PlayerStatus.Disable).First().PlayerIndex;
                        if (Players.Count > registratingPlayerCounter && !isRegistrationActive) {
                            Players[registratingPlayerCounter].ChangeStatus(PlayerStatus.Registrating);
                            isRegistrationActive = true;
                            registrationManager.RegisterNext();
                        }

                        dict["BranchesColor"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        break;
                    }
            }

            Application.Current.Resources.MergedDictionaries[3] = dict;
        }

        private void ButtonConnector_OnNewPortNames(List<string> names)
        {
            if (names.Count >= SerialPorts.Count) {
                for (int i = 0; i < names.Count; ++i) {
                    if (i >= SerialPorts.Count) {
                        SerialPorts.Add(names[i]);
                    } else if (SerialPorts[i] != names[i]) {
                        SerialPorts[i] = names[i];
                    }
                }
            } else {
                for (int i = names.Count; i < SerialPorts.Count; ++i) {
                    SerialPorts.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Invoke when player button successfully registrate
        /// </summary>
        /// <param name="buttonIndex">Index of player button</param>
        private void RegistrationManager_OnPlayerRegistrated(int buttonIndex)
        {
            if (Players.Where(p => p.ButtonIndex == buttonIndex).ToArray().Count() > 0) {
                registrationManager.RegisterNext();
                return;
            }

            Players[registratingPlayerCounter].ChangeStatus(PlayerStatus.Registered);
            Players[registratingPlayerCounter].ButtonIndex = buttonIndex;

            registratingPlayerCounter++;

            if (registratingPlayerCounter < Players.Count) {
                Players[registratingPlayerCounter].ChangeStatus(PlayerStatus.Registrating);
                registrationManager.RegisterNext();
            } else {
                isRegistrationActive = false;
            }
        }

        /// <summary>
        /// Invoke when player registration failed
        /// </summary>
        private void RegistrationManager_OnPlayerDisable()
        {
            Players[registratingPlayerCounter].ChangeStatus(PlayerStatus.Disable);

            registratingPlayerCounter++;

            if (registratingPlayerCounter < Players.Count) {
                Players[registratingPlayerCounter].ChangeStatus(PlayerStatus.Registrating);
                registrationManager.RegisterNext();
            } else {
                isRegistrationActive = false;
            }
        }

        /// <summary>
        /// Invoke when player give a wrong answer
        /// </summary>
        private void QuizManager_OnWrongAnswer()
        {
            if (Players.Where(p => !p.isAnswered).ToArray().Count() == 0) {
                ActivePlayer = null;
                isAddingPointNecessary = true;
                AddPointsMenu.Visibility = Visibility.Visible;
                ShowImage();

                if (RightAnswerGrid.Visibility == Visibility.Visible) {
                    closedElement = RightAnswerGrid;
                    RightAnswerGrid.Visibility = Visibility.Hidden;
                } else if (MediaDockPanel.Visibility == Visibility.Visible) {
                    closedElement = MediaGrid;
                    MediaDockPanel.Visibility = Visibility.Hidden;
                } else if (MainSettingsGrid.Visibility == Visibility.Visible) {
                    closedElement = MainSettingsGrid;
                    MainSettingsGrid.Visibility = Visibility.Hidden;
                }

                isAddPointsWindowOpened = true;

                quizManager.StopQuiz();

                Players[activePlayerIndex].ChangeStatus(PlayerStatus.Registered);
                isPlayerAnswering = false;
                return;
            }


            Players[activePlayerIndex].ChangeStatus(PlayerStatus.Registered);
            isPlayerAnswering = false;

            ShowImage();
        }

        /// <summary>
        /// Invoke when player give a right answer
        /// </summary>
        /// <param name="points"></param>
        private void QuizManager_OnRightAnswer(double points, bool isInvokeFromThis = false)
        {
            if (!isInvokeFromThis) {
                ActivePlayer = Players[activePlayerIndex];
            }
            
            ShowBlacker();
            RightAnswerGrid.Visibility = Visibility.Visible;

            AddPoints(activePlayerIndex, points);
            Players[activePlayerIndex].ChangeStatus(PlayerStatus.Registered);

            foreach (Player p in Players) {
                p.AnswerTime = "";
                p.isAnswered = false;
            }
            isPlayerAnswering = false;

            dbWorker.UpdatePoints(activePlayerIndex, Players[activePlayerIndex].Points);
            dbWorker.UpdateCurrentQuestion(QuestionNumber);
        }

        /// <summary>
        /// Invoke when player's button clicked
        /// </summary>
        /// <param name="buttonIndex">Index of clicked button</param>
        private void QuizManager_OnPlayerButtonClicked(int buttonIndex, double points)
        {
            MediaBlock.Pause();
            isVideoPlay = false;

            Sound.Position = TimeSpan.FromMilliseconds(0);
            Sound.Play();

            if (Players.Where(p => p.ButtonIndex == buttonIndex).ToArray()[0].isAnswered) {
                if (Players.Where(p => !p.isAnswered).ToArray().Count() == 0) {
                    return;
                } else {
                    quizManager.WrongPlayer(StartTimer);
                }

                return;
            }

            HideImage();
            _answerSecondsTimer.Change(Timeout.Infinite, 0);
            isTimerStarted = false;
            isPlayerAnswering = true;

            activePlayerIndex = Players.Where(p => p.ButtonIndex == buttonIndex).ToArray()[0].PlayerIndex;
            Players[activePlayerIndex].ChangeStatus(PlayerStatus.Answering, points, (long)_answersTime.TotalMilliseconds);
            Players[activePlayerIndex].isAnswered = true;
        }

        /// <summary>
        /// Invoke when manager starts new question
        /// </summary>
        /// <param name="id">Number of question</param>
        /// <param name="text">Text of question</param>
        /// <param name="secondsToAnswer">Time in seconds to answer</param>
        /// <param name="kind">Type of question</param>
        /// <param name="answers">List of answers</param>
        /// <param name="mediaPath">Path to video or picture for question</param>
        private void QuizManager_OnNewQuestion(int id, string text, string rightAnswer, int secondsToAnswer, QuestionKind kind, List<Answer> answers, Uri mediaPath, MediaAnswer mediaAnswer)
        {
            switch (kind) {
                case QuestionKind.WithVideo:
                    {
                        ShowBlacker();
                        ImageAndVideoGrid.Children[0].Visibility = Visibility.Collapsed;
                        MediaGrid.Visibility = Visibility.Visible;
                        MediaBorder.Padding = new Thickness(200, 30, 200, 0);
                        MediaBlock.Source = mediaPath;
                        MediaBlock.Visibility = Visibility.Visible;
                        MediaBlock.Play();
                        isVideoQuestion = true;
                        isVideoPlay = true;
                        break;
                    }
                case QuestionKind.WithImage:
                    {
                        ShowBlacker();
                        MediaBlock.Stop();

                        ImageAndVideoGrid.Children.RemoveAt(0);

                        Image image = new Image();
                        BitmapImage src = new BitmapImage();
                        src.BeginInit();
                        src.UriSource = mediaPath;
                        src.EndInit();
                        image.Source = src;
                        image.Stretch = Stretch.Uniform;

                        ImageAndVideoGrid.Children.Insert(0, image);

                       
                        MediaGrid.Visibility = Visibility.Visible;
                        MediaBlock.Visibility = Visibility.Collapsed;
                        isVideoQuestion = false;


                        break;
                    }
                case QuestionKind.Simple:
                    {
                        HideBlacker();
                        MediaBlock.Stop();
                        MediaDockPanel.Visibility = Visibility.Collapsed;
                        MediaBlock.Visibility = Visibility.Collapsed;
                        isVideoQuestion = false;
                        break;
                    }
            }

            switch (mediaAnswer.Kind) {
                case AnswerKind.WithImage:
                    {
                        RightAnswerMediaGrid.Visibility = Visibility.Visible;
                        RightAnswerMediaBlock.Stop();
                        RightAnswerMediaBlock.Visibility = Visibility.Collapsed;

                        RightMediaInnerGrid.Children.RemoveAt(0);

                        Image image = new Image();
                        BitmapImage src = new BitmapImage();
                        src.BeginInit();
                        src.UriSource = mediaAnswer.AnswerImagePath;
                        src.EndInit();
                        image.Source = src;
                        image.Stretch = Stretch.Uniform;

                        RightMediaInnerGrid.Children.Insert(0, image);

                        if (src.Height > SystemParameters.PrimaryScreenHeight / 2.4) {
                            (RightMediaInnerGrid.Children[0] as Image).Height = SystemParameters.PrimaryScreenHeight / 2.4;
                        }


                        RightAnswerDockPanel.Margin = new Thickness(0, 20, 0, 0);

                        break;
                    }
                case AnswerKind.WithVideo:
                    {
                        RightAnswerMediaGrid.Visibility = Visibility.Visible;
                        RightAnswerMediaBlock.Visibility = Visibility.Visible;
                        RightMediaInnerGrid.Children[0].Visibility = Visibility.Collapsed;
                        RightAnswerMediaBlock.Source = mediaAnswer.AnswerVideoPath;
                        RightAnswerMediaBlock.Play();

                        RightAnswerDockPanel.Margin = new Thickness(0, 20, 0, 0);

                        break;
                    }
                case AnswerKind.Simple:
                    {
                        RightAnswerMediaGrid.Visibility = Visibility.Collapsed;
                        RightAnswerDockPanel.Margin = new Thickness(0, 100, 0, 0);
                        break;
                    }
            }

            QuestionNumberFontSize = 33 - ((int)Math.Log10(QuestionNumber) + 1) * 2;
            if (text.Count() < 93) {
                QuestionTextBlock.VerticalAlignment = VerticalAlignment.Center;
            }
            else if (text.Count() < 185) {
                QuestionTextBlock.VerticalAlignment = VerticalAlignment.Top;
            }
            else {
                QuestionTextBlock.VerticalAlignment = VerticalAlignment.Top;
                text = text.Substring(0, 182) + "...";
            }

            QuestionText = text;

            Regex reg = new Regex(@"\s{2,}");
            text = reg.Replace(text, " ");

            MainWindowQuestion = text;
            AnswerTimerText = secondsToAnswer.ToString();
            _answersTime = TimeSpan.FromSeconds(secondsToAnswer);

            QuestionNumber = id;

            RightAnswer = rightAnswer;
            RightAnswerGrid.Visibility = Visibility.Hidden;
            RightAnswerMediaBlock.Stop();

            MediaGrid.Visibility = Visibility.Visible;
            MediaDockPanel.Visibility = Visibility.Visible;
            ShowImage();
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

        #region Blacker Methods
        private void ShowBlacker() {
            if (isBlackerShowen) { return; }

            ColorAnimation blackerAnimation = new ColorAnimation();
            blackerAnimation.From = Color.FromArgb(0, 0, 0, 0);
            blackerAnimation.To = Color.FromArgb(174, 0, 0, 0);
            blackerAnimation.Duration = TimeSpan.FromMilliseconds(300);
            blackerAnimation.AccelerationRatio = 0.6;

            (Blacker.Background as SolidColorBrush).BeginAnimation(SolidColorBrush.ColorProperty, blackerAnimation);

            isBlackerShowen = true;
        }

        private void HideBlacker() {
            if (!isBlackerShowen) { return; }

            ColorAnimation blackerAnimation = new ColorAnimation();
            blackerAnimation.From = (Blacker.Background as SolidColorBrush).Color;
            blackerAnimation.To = Color.FromArgb(0, 0, 0, 0);
            blackerAnimation.Duration = TimeSpan.FromMilliseconds(300);
            blackerAnimation.AccelerationRatio = 0.6;

            (Blacker.Background as SolidColorBrush).BeginAnimation(SolidColorBrush.ColorProperty, blackerAnimation);

            isBlackerShowen = false;
        }
        #endregion

        #region AnswerTimerArea
        private void StartTimer()
        {
            if (isTimerStarted || isPlayerAnswering) { return; }
            isTimerStarted = true;
            _answerSecondsTimer = new Timer(SecondsTimerCallback, null, 0, 71);
        }

        private void SecondsTimerCallback(object o)
        {
            try {
                Extensions.ExecuteWithNormalDispatcher(() => {
                    _answersTime = TimeSpan.FromMilliseconds(_answersTime.TotalMilliseconds - 71);
                    AnswerTimerText = _answersTime.ToString(@"mm\:ss\:ff");

                    if (_answersTime.TotalMilliseconds <= 71)
                    {
                        _answerSecondsTimer.Change(Timeout.Infinite, 0);
                        AnswerTimerText = "00:00:00";
                        Timer flickTimer = new Timer(TimeFlick, null, 0, 300);
                        Thread thread = new Thread(() => {
                            Thread.Sleep(3000);
                            Extensions.ExecuteWithNormalDispatcher(() => {
                                flickTimer.Change(Timeout.Infinite, 0);
                                TimeBlock.Visibility = Visibility.Visible;
                            });
                            isTimerStarted = false;
                        });

                    }
                });
            } catch { }
        }
        
        private void TimeFlick(object o)
        {
            Extensions.ExecuteWithNormalDispatcher(() => {
                if (TimeBlock.Visibility == Visibility.Visible) {
                    TimeBlock.Visibility = Visibility.Hidden;
                } else {
                    TimeBlock.Visibility = Visibility.Visible;
                }
            });
        }
        #endregion

        private void ShowImage()
        {
            MediaGrid.Visibility = Visibility.Visible;
            ShowBlacker();
        }

        private void HideImage()
        {
            MediaGrid.Visibility = Visibility.Hidden;
            HideBlacker();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        #region ControlsListeners

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            quizManager.StopQuiz();
            registrationManager.StopManager();
            buttonConnector.AbortAll();

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
            DoubleAnimation settingsHeightAnimation = new DoubleAnimation();
            settingsHeightAnimation.From = SettingsBorder.ActualHeight;
            settingsHeightAnimation.To = 0;
            settingsHeightAnimation.Duration = TimeSpan.FromMilliseconds(100);
            settingsHeightAnimation.AccelerationRatio = 0.3;

            HideBlacker();
            SettingsBorder.BeginAnimation(WidthProperty, settingsHeightAnimation);

            registrationManager.StopManager();

            Thread thread = new Thread(() => {
                foreach (Player p in Players) {
                    if (p.ButtonIndex == -1) {
                        dbWorker.DeletePlayer(p.PlayerIndex);              
                    } else {
                        dbWorker.AddOrUpdatePlayerInfo(p.PlayerIndex, p.Name, p.Points);
                    }
                }

                foreach (Player p in Players) {
                    if (p.ButtonIndex == -1) {
                        Extensions.ExcecuteWithAppIdleDispatcher(() => {
                            Players.Remove(p);
                        });
                    }
                }
            });
            thread.Start();

            if (!isQuizStarted) {
                quizManager.StartQuiz();
                isQuizStarted = true;
            }

            this.WindowStyle = WindowStyle.None;
            this.WindowState = WindowState.Maximized;
        } 

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsBorder.Height != 0) { return; }

            AddPlayerBtn.Visibility = Visibility.Collapsed;

            DoubleAnimation settingsHeightAnimation = new DoubleAnimation();
            settingsHeightAnimation.From = 0;
            settingsHeightAnimation.To = (SystemParameters.PrimaryScreenHeight - 30) / 2 ;
            settingsHeightAnimation.Duration = TimeSpan.FromMilliseconds(100);
            settingsHeightAnimation.AccelerationRatio = 0.3;

            ShowBlacker();
            SettingsBorder.BeginAnimation(HeightProperty, settingsHeightAnimation);

            StartButton.Text = "Продолжить";
            FooterGrid.ColumnDefinitions[0].Width = new GridLength(2.1, GridUnitType.Star);
        }

        private void SerialBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                buttonConnector.PortName = (sender as ComboBox).SelectedItem.ToString();
            }
            catch { }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key) {
                case Key.P:
                    {
                        if (isVideoQuestion) {
                            if (isVideoPlay) {
                                MediaBlock.Pause();
                            } else {
                                MediaBlock.Play();
                            }

                            isVideoPlay = !isVideoPlay;
                        }

                        if (isVideoRightAnswer) {
                            if (isRightAnswerVideoPlay) {
                                RightAnswerMediaBlock.Pause();
                            } else {
                                RightAnswerMediaBlock.Play();
                            }

                            isRightAnswerVideoPlay = !isRightAnswerVideoPlay;
                        }
                        break;
                    }
                case Key.R:
                    {
                        if (isVideoQuestion) {
                            MediaBlock.Stop();
                            MediaBlock.Play();
                            isVideoPlay = true;
                        }

                        if (isVideoRightAnswer) {
                            MediaBlock.Stop();
                            MediaBlock.Play();
                            isRightAnswerVideoPlay = true;
                        } 
                        break;
                    }
            }

            if (!isQuizStarted) { return; }

            if (e.Key == Key.Enter) {
                if (isAddPointsWindowOpened) {
                    if (ActivePlayer != null && ActivePlayer.PlayerIndex >= 0) {
                        if (quizManager.AddedPoints()) {
                            HideBlacker();
                        } else {
                            ShowBlacker();
                        }
                        

                        isAddPointsWindowOpened = false;
                        
                        AddPointsMenu.Visibility = Visibility.Collapsed;

                        if (isAddingPointNecessary) {
                            AddPoints(ActivePlayer.PlayerIndex, Convert.ToDouble(AddingPointsTextBlock.Text));
                            QuizManager_OnRightAnswer(0, true);
                            MediaGrid.Visibility = Visibility.Hidden;
                            RightAnswerGrid.Visibility = Visibility.Visible;
                            isAddingPointNecessary = false;
                            return;
                        }

                        HideBlacker();
                        AddPoints(ActivePlayer.PlayerIndex, Convert.ToDouble(AddingPointsTextBlock.Text));

                        isAddingPointNecessary = false;
                        closedElement.Visibility = Visibility.Visible;
                    }
                }
            }

            if (e.Key == Key.Escape) {
                if (isAddPointsWindowOpened) {
                    if (!isAddingPointNecessary) {
                        AddPointsMenu.Visibility = Visibility.Hidden;
                        closedElement.Visibility = Visibility.Visible;
                        HideBlacker();
                    }
                }
            }

            if (isAddPointsWindowOpened) {
                if (Char.IsDigit((char)KeyInterop.VirtualKeyFromKey(e.Key)) && e.Key != Key.Back || e.Key == Key.Space) {
                    if ((int)(((char)KeyInterop.VirtualKeyFromKey(e.Key)) - '0') > Players.Count || (int)(((char)KeyInterop.VirtualKeyFromKey(e.Key)) - '0') == 0) {
                        return;
                    }

                    ActivePlayer = Players[(int)(((char)KeyInterop.VirtualKeyFromKey(e.Key)) - '0') - 1];
                } else { return; }
            }

            switch (e.Key) {
                case Key.Y:
                    {
                        quizManager.RightAnswerClick();
                        break;
                    }
                case Key.N:
                    {
                        quizManager.WrongAnswerClick();
                        break;
                    }
                case Key.S:
                    {
                        quizManager.StartButtonListener(StartTimer);
                        break;
                    }
                case Key.C:
                    {
                        quizManager.Next();
                        break;
                    }
                case Key.Q:
                    {
                        ShowBlacker();
                        isAddingPointNecessary = false;
                        AddPointsMenu.Visibility = Visibility.Visible;
                        ActivePlayer = null;

                        if (RightAnswerGrid.Visibility == Visibility.Visible) {
                            closedElement = RightAnswerGrid;
                            RightAnswerGrid.Visibility = Visibility.Hidden;
                        } else if (MediaGrid.Visibility == Visibility.Visible) {
                            closedElement = MediaGrid;
                            MediaGrid.Visibility = Visibility.Hidden;
                        } else if (MainSettingsGrid.Visibility == Visibility.Visible) {
                            closedElement = MainSettingsGrid;
                            MainSettingsGrid.Visibility = Visibility.Hidden;
                        }

                        isAddPointsWindowOpened = true;

                        quizManager.StopQuiz();

                        break;
                    }
            }
        }

        private void SettingsBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (PlayersPanel.Visibility == Visibility.Hidden) {
                PlayersPanel.Visibility = Visibility.Visible;
                SettingsPanel.Visibility = Visibility.Hidden;
            } else {
                PlayersPanel.Visibility = Visibility.Hidden;
                SettingsPanel.Visibility = Visibility.Visible;
            }
        }

        private void ResPointsBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (Player p in Players) {
                AddPoints(p.PlayerIndex, -1 * p.Points);
                dbWorker.UpdatePoints(p.PlayerIndex, 0);
            }
        }

        private void DelPlayersBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (Player p in Players.Reverse()) {
                dbWorker.DeletePlayer(p.PlayerIndex);
                Players.Remove(p);
            }

            playersCount = 0;
        }

        private void DelQuestionBtn_Click(object sender, RoutedEventArgs e)
        {
            dbWorker.UpdateCurrentQuestion(0);

            quizManager.StopQuiz();
            quizManager.Init(dbWorker.GetQuestions(), buttonConnector);
        }

        private void DelAllBtn_Click(object sender, RoutedEventArgs e)
        {
            dbWorker.UpdateCurrentQuestion(0);
            dbWorker.UpdateCurrentRound(1);

            quizManager.StopQuiz();
            quizManager.Init(dbWorker.GetQuestions(), buttonConnector);

            foreach (Player p in Players.Reverse()) {
                dbWorker.DeletePlayer(p.PlayerIndex);
                Players.Remove(p);
            }

            playersCount = 0;
            RoundNumber = 1;
        }

        #endregion

        
    }
}
