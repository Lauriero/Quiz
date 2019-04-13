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
        private int buttonIndexTemp;
        private bool isQuestionAnswered = false;
        private bool isClickWaiting = false;
        private bool isWrongAnswer = true;

        public event Action<string, int, QuestionKind, List<Answer>, Uri> OnNewQuestion;
        public event Action<int> OnRightAnswer;
        public event Action<int, int> OnPlayerButtonClicked;
        public event Action OnWrongAnswer;

        public void Init(List<Question> questions, ButtonModuleConnector moduleConnector)
        {
            this.questions = questions;
            this.moduleConnector = moduleConnector;
        }

        public void StartQuiz()
        {
            NextQuestion();
        }

        public void StartButtonListener()
        {
            if (isWrongAnswer || isQuestionAnswered) {
                isQuestionAnswered = false;
                isWrongAnswer = false;

                Thread thread = new Thread(WaitClick);
                thread.Start();
            }
        }

        public void RightAnswerClick()
        {
            OnRightAnswer?.Invoke(questions[questionCounter - 1].Points);
            NextQuestion();
            isWrongAnswer = false;
        }

        public void WrongAnswerClick()
        {
            OnWrongAnswer?.Invoke();
            isWrongAnswer = true;
        }

        private void WaitClick()
        {
            int buttonIndex = moduleConnector.GetButtonClick();
            Extensions.ExcecuteWithAppIdleDispatcher(() => OnPlayerButtonClicked?.Invoke(buttonIndex, currentQuestion.Points)); 
        }

        private void NextQuestion()
        {
            Question q = questions[questionCounter];
            currentQuestion = q;
            if (q.VideoPath != null) {
                Extensions.ExcecuteWithAppIdleDispatcher(() => OnNewQuestion?.Invoke(q.QuestionText, q.TimeToAnswer, QuestionKind.WithVideo, null, q.VideoPath));
            } else if (q.ImagePath != null) {
                Extensions.ExcecuteWithAppIdleDispatcher(() => OnNewQuestion?.Invoke(q.QuestionText, q.TimeToAnswer, QuestionKind.WithImage, null, q.ImagePath));
            } else if (q.Answers != null) {
                Extensions.ExcecuteWithAppIdleDispatcher(() => OnNewQuestion?.Invoke(q.QuestionText, q.TimeToAnswer, QuestionKind.WithAnswers, q.Answers, null));
            } else {
                Extensions.ExcecuteWithAppIdleDispatcher(() => OnNewQuestion?.Invoke(q.QuestionText, q.TimeToAnswer, QuestionKind.Simple, null, null));
            }

            questionCounter++;
            isQuestionAnswered = true;
            isWrongAnswer = false;
        }

        public void Click()
        {
            OnPlayerButtonClicked?.Invoke(1, 3);
        }
    }

    public enum QuestionKind
    {
        Simple,
        WithVideo,
        WithImage,
        WithAnswers
    }
}
