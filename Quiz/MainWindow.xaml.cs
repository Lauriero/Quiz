using Quiz.Support;
using Quiz.Support.DataModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        public string QuestionText {
            get { return _questionString; }
            set {
                _questionString = value;
                OnPropertyChanged("QuestionText");
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
        private int registratingPlayerCounter = 0;
        private int activePlayer = -1;
        private int maxPoint = 20;
        private int WiFiStatus = 0;

        private bool isRegistrationActive = false;

        private double pointBarWidthStep = 0;
        private double pointBarsContainerWidth = 0;
        private double playersNameContainerHeight = 0;
        private double _playerBarHeight;
        private int _questionNumber;
        private int _questionNumberFontSize;
        private int _questionTextFontSize;
        private string _questionString;
        private Thickness _playerNameMargin;

        private ButtonModuleConnector buttonConnector;
        private RegistrationManager registrationManager;
        private QuizManager quizManager;

        public MainWindow()
        {
            RoundNumber = 1;
            QuestionNumberFontSize = 33;
            QuestionTextFontSize = 30;

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
            buttonConnector.Init("COM4");

            registrationManager = new RegistrationManager();
            registrationManager.Init(buttonConnector);
            registrationManager.OnPlayerDisable += RegistrationManager_OnPlayerDisable;
            registrationManager.OnPlayerRegistrated += RegistrationManager_OnPlayerRegistrated;

            List<Question> questions = new List<Question>();
            Question q1 = new Question();
            Question q2 = new Question();
            Question q3 = new Question();
            q1.questionText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus eros sapien, malesuada nec congue vel, feugiat id nisi. Vivamus id ex pulvinar, varius purus eget, tristique leo. Mauris interdum, sem eu hendrerit accumsan, dolor dui interdum tortor, in posuere velit turpis ut libero. Nam sodales hendrerit orci ut laoreet. Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
            q1.points = 10;
            q1.timeToAnswer = 20;
            q1.videoPath = "Question1.mp4";
            q1.isVideoPathRelative = true;
            q2.questionText = "Сколько длилась блокада.";
            q2.points = 10;
            q2.timeToAnswer = 20;
            q2.videoPath = null;
            q2.isVideoPathRelative = true;
            q3.questionText = "Dolor dui interdum tortor, in posuere velit turpis ut libero. Nam sodales hendrerit orci ut laoreet. Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
            q3.points = 10;
            q3.timeToAnswer = 20;
            q3.videoPath = null;
            q3.isVideoPathRelative = true;
            questions.Add(q1);questions.Add(q2);questions.Add(q3);

            quizManager = new QuizManager();
            quizManager.OnNewQuestion += QuizManager_OnNewQuestion;
            quizManager.OnPlayerButtonClicked += QuizManager_OnPlayerButtonClicked;
            quizManager.OnRightAnswer += QuizManager_OnRightAnswer;
            quizManager.OnWrongAnswer += QuizManager_OnWrongAnswer;
            quizManager.Init(questions, buttonConnector);

            AddPlayer();
            AddPlayer();

            Players[0].ChangeStatus(1);

            MediaBlock.LoadedBehavior = MediaState.Manual;
            MediaBlock.UnloadedBehavior = MediaState.Manual;
            MediaBlock.Volume = 0.5f;
        }

        #region PlayersSupportMethods

        /// <summary>
        /// Add the new player
        /// </summary>
        private void AddPlayer() {

            if (playersCount == MAX_PLAYERS_COUNT - 1) {
                AddPlayerBtn.Visibility = Visibility.Collapsed;
            }

            if (playersCount < defaultColors.Count) {
                Players.Add(new Player(string.Format("Игрок {0}", playersCount + 1), defaultColors[playersCount], playersCount));
            } else {
                Players.Add(new Player(string.Format("Игрок {0}", playersCount + 1), Color.FromRgb(48, 59, 63), playersCount));
            }

            if (!isRegistrationActive) {
                isRegistrationActive = true;
                Players.Last().ChangeStatus(1);
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

        /// <summary>
        /// Invoke when player button successfully registrate
        /// </summary>
        /// <param name="buttonIndex">Index of player button</param>
        private void RegistrationManager_OnPlayerRegistrated(int buttonIndex)
        {
            Players[registratingPlayerCounter].ChangeStatus(2);
            Players[registratingPlayerCounter].ButtonIndex = buttonIndex;

            registratingPlayerCounter++;

            if (registratingPlayerCounter < Players.Count) {
                Players[registratingPlayerCounter].ChangeStatus(1);         
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
            Players[registratingPlayerCounter].ChangeStatus(0);

            registratingPlayerCounter++;

            if (registratingPlayerCounter < Players.Count) {
                Players[registratingPlayerCounter].ChangeStatus(1);
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
            activePlayer = -1;
        }

        /// <summary>
        /// Invoke when player give a right answer
        /// </summary>
        /// <param name="points"></param>
        private void QuizManager_OnRightAnswer(int points)
        {
            if (activePlayer >= 0) {
                AddPoints(activePlayer, points);
            }
        }

        /// <summary>
        /// Invoke when player's button clicked
        /// </summary>
        /// <param name="buttonIndex">Index of clicked button</param>
        private void QuizManager_OnPlayerButtonClicked(int buttonIndex)
        {
            activePlayer = Players.Where(p => p.ButtonIndex == buttonIndex).ToList()[0].PlayerIndex;
        }

        /// <summary>
        /// Invoke when manager starts new question
        /// </summary>
        /// <param name="text">Text of question</param>
        /// <param name="kind">Type of question</param>
        /// <param name="answers">List of answers</param>
        /// <param name="videoPath">Path to video for question</param>
        /// <param name="isVideoPathRelative">Reletive path to video or not</param>
        private void QuizManager_OnNewQuestion(string text, QuestionKind kind, List<Answer> answers, string videoPath, bool isVideoPathRelative)
        {
            switch (kind) {
                case QuestionKind.WithVideo:
                    {
                        if (isVideoPathRelative) {
                            MediaBlock.Source = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, videoPath));
                        } else {
                            MediaBlock.Source = new Uri(videoPath);
                        }
                        MediaBlock.Visibility = Visibility.Visible;
                        PointBarsContainer.Visibility = Visibility.Hidden;
                        MediaBlock.Play();

                        break;
                    }
                case QuestionKind.Simple:
                    {
                        MediaBlock.Stop();
                        MediaBlock.Visibility = Visibility.Hidden;
                        PointBarsContainer.Visibility = Visibility.Visible;
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

            QuestionNumber++;
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

            registrationManager.StopManager();

            AddPoints(0, 3);
            AddPoints(1, 5);
            AddPoints(2, 8);

            Thread thread = new Thread(() => {
                Thread.Sleep(1000);
                foreach (Player p in Players) {
                    if (p.ButtonIndex == -1) {
                        Extensions.ExecuteInApplicationThread(() => {
                            Players.Remove(p);
                        });
                    }
                }
            });

            thread.Start();

        } 

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsBorder.Height != 0) { return; }

            AddPlayerBtn.Visibility = Visibility.Collapsed;

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

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
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
                        quizManager.StartClick();
                        break;
                    }
            }
        }

        #endregion

        
    }
}
