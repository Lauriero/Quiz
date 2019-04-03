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
        private ButtonModuleConnector moduleConnector;

        private int questionCounter = 0;
        private int buttonIndexTemp;
        private bool questionAnswered = false;
        private bool isClickWaiting = false;

        public event Action<string, QuestionKind, List<Answer>, string, bool> OnNewQuestion;
        public event Action<int> OnRightAnswer;
        public event Action<int> OnPlayerButtonClicked;
        public event Action OnWrongAnswer;

        public void Init(List<Question> questions, ButtonModuleConnector moduleConnector) {
            this.questions = questions;
            this.moduleConnector = moduleConnector;
        }

        public void StartQuiz() {
            Thread workerThread = new Thread(QuizWorker);
            workerThread.Start();
        }

        public void StopQuiz() {

        }

        private void QuizWorker() {
            foreach (Question q in questions) {
                if (q.videoPath != null) {
                    Extensions.ExecuteInApplicationThread(() => OnNewQuestion?.Invoke(q.questionText, QuestionKind.WithVideo, null, q.videoPath, q.isVideoPathRelative));
                } else if (q.answers != null) {
                    Extensions.ExecuteInApplicationThread(() => OnNewQuestion?.Invoke(q.questionText, QuestionKind.WithAnswers, q.answers, null, false));
                } else {
                    Extensions.ExecuteInApplicationThread(() => OnNewQuestion?.Invoke(q.questionText, QuestionKind.Simple, null, null, false));
                }

                
            }
        }

        public void StartClick() {
            if (!questionAnswered) {
                NextQuestion();
            } else {
                Thread thread = new Thread(WaitClick);
                thread.Start();
            }
        }

        public void RightAnswerClick() {
            if (questionAnswered) {
                OnRightAnswer?.Invoke(questions[questionCounter - 1].points);
                NextQuestion();
            }
        }

        public void WrongAnswerClick() {
            if (questionAnswered) {
                OnWrongAnswer?.Invoke();
            }
        }

        private void WaitClick() {
            int buttonIndex = moduleConnector.GetButtonClick();
            Extensions.ExecuteInApplicationThread(() => OnPlayerButtonClicked?.Invoke(buttonIndex));
            Extensions.ExecuteInApplicationThread(() => { MessageBox.Show(buttonIndex.ToString()); });
        }

        private void NextQuestion() {
            Question q = questions[questionCounter];
            if (q.videoPath != null) {
                Extensions.ExecuteInApplicationThread(() => OnNewQuestion?.Invoke(q.questionText, QuestionKind.WithVideo, null, q.videoPath, q.isVideoPathRelative));
            } else if (q.answers != null) {
                Extensions.ExecuteInApplicationThread(() => OnNewQuestion?.Invoke(q.questionText, QuestionKind.WithAnswers, q.answers, null, false));
            } else {
                Extensions.ExecuteInApplicationThread(() => OnNewQuestion?.Invoke(q.questionText, QuestionKind.Simple, null, null, false));
            }

            questionCounter++;
            questionAnswered = true;
        } 
    }

    public enum QuestionKind
    {
        Simple,
        WithVideo,
        WithAnswers
    }
}
