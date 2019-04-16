using Quiz.Support.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Quiz.Support
{
    class QuizManager
    {
        private List<Question> questions;
        private Question currentQuestion;

        private ButtonModuleConnector moduleConnector;

        private int questionCounter = 0;
        private bool isQuestionAnswered = false;
        private bool isAnswerWaiting = false;
        private bool isPlayerAnswering = false;
        private bool isWrongPlayer = false;
        private bool isWrongAnswer = true;

        private Thread waitThread;

        public event Action<int, string, string, int, QuestionKind, List<Answer>, Uri, MediaAnswer> OnNewQuestion;
        public event Action<double, bool> OnRightAnswer;
        public event Action<int, double> OnPlayerButtonClicked;
        public event Action OnWrongAnswer;
        public event Action OnRoundEnd;

        public void Init(List<Question> questions, ButtonModuleConnector moduleConnector)
        {
            this.questions = questions;
            this.moduleConnector = moduleConnector;

            questionCounter = 0;
            isQuestionAnswered = false;
            isWrongAnswer = true;
        }

        public void StartQuiz()
        {
            NextQuestion();
        }

        public void StopQuiz()
        {
            moduleConnector.AbortListener();
            waitThread?.Abort();
        }

        public void AddExtraQuestion() {
            Question q = new Question();
            q.Id = questions.Count();
            q.Points = 2;
            q.TimeToAnswer = 30;
            q.QuestionText = "Дополнительный вопрос";
            questions.Add(q);
        }

        public void StartButtonListener(QuizTimerDelegate del)
        {
            if (isWrongAnswer || isQuestionAnswered || isWrongPlayer) {
                isQuestionAnswered = false;
                isWrongAnswer = false;
                isAnswerWaiting = true;
                isWrongPlayer = false;

                waitThread = new Thread(WaitClick);
                waitThread.Start();

                del?.Invoke();
            }
        }

        public void Next() {
            if (!isQuestionAnswered && !isAnswerWaiting && !isPlayerAnswering && !isWrongAnswer) {
                NextQuestion();
            }
        }

        public void RightAnswerClick()
        {
            if (isPlayerAnswering) {
                isPlayerAnswering = false;
                isQuestionAnswered = false;
                isWrongAnswer = false;
                OnRightAnswer?.Invoke(currentQuestion.Points, false);
            }
        }

        public void WrongAnswerClick()
        {
            if (isPlayerAnswering) {
                isQuestionAnswered = false;
                isWrongAnswer = true;
                isPlayerAnswering = false;
                OnWrongAnswer?.Invoke();
            }
        }

        public void WrongPlayer(QuizTimerDelegate del) {
            isWrongPlayer = true;
            StartButtonListener(del);
            isPlayerAnswering = false;
            isWrongAnswer = false;
            isAnswerWaiting = true;
        }

        private void WaitClick()
        {
            int buttonIndex = moduleConnector.GetButtonClick();
            Extensions.ExcecuteWithAppIdleDispatcher(() => OnPlayerButtonClicked?.Invoke(buttonIndex, currentQuestion.Points));

            isPlayerAnswering = true;
            isAnswerWaiting = false;
        }

        private void NextQuestion()
        {
            if (questionCounter >= questions.Count) {
                OnRoundEnd?.Invoke();
                return;
            }

            Question q = questions[questionCounter];
            currentQuestion = q;

            MediaAnswer answer = new MediaAnswer();
            if (q.AnswerVideoPath != null) {
                answer.AnswerVideoPath = q.AnswerVideoPath;
                answer.Kind = AnswerKind.WithVideo;
            } else if (q.AnswerImagePath != null) {
                answer.AnswerImagePath = q.AnswerImagePath;
                answer.Kind = AnswerKind.WithImage;
            } else {
                answer.Kind = AnswerKind.Simple;
            }

            if (q.VideoPath != null) {
                Extensions.ExcecuteWithAppIdleDispatcher(() => OnNewQuestion?.Invoke(q.Id, q.QuestionText, q.RightAnswer, q.TimeToAnswer, QuestionKind.WithVideo, null, q.VideoPath, answer));
            } else if (q.ImagePath != null) {
                Extensions.ExcecuteWithAppIdleDispatcher(() => OnNewQuestion?.Invoke(q.Id, q.QuestionText, q.RightAnswer, q.TimeToAnswer, QuestionKind.WithImage, null, q.ImagePath, answer));
            } else if (q.Answers != null) {
                Extensions.ExcecuteWithAppIdleDispatcher(() => OnNewQuestion?.Invoke(q.Id, q.QuestionText, q.RightAnswer, q.TimeToAnswer, QuestionKind.WithAnswers, q.Answers, null, answer));
            } else {
                Extensions.ExcecuteWithAppIdleDispatcher(() => OnNewQuestion?.Invoke(q.Id, q.QuestionText, q.RightAnswer, q.TimeToAnswer, QuestionKind.Simple, null, null, answer));
            }

            questionCounter++;
            isQuestionAnswered = true;
            isWrongAnswer = false;
            isWrongPlayer = false;
        }

        public bool AddedPoints() {
            isWrongAnswer = false;

            if (isPlayerAnswering == true) {
                return true;
            } else {
                return false;
            }
        }

        public delegate void QuizTimerDelegate();
    }

    public enum QuestionKind
    {
        Simple,
        WithVideo,
        WithImage,
        WithAnswers
    }

    public enum AnswerKind
    {
        Simple,
        WithVideo,
        WithImage
    }
}
