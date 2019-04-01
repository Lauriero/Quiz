using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;

namespace Quiz
{
    class QuestionManager
    {
        private bool _isTimeRuns = false;
        private bool _isRightAnswer = false;
        private bool _isTimeStarted = false;
        private int _questionPrise;
        private int _responseTime;

        private List<string> _managingQuestions;
        private Dictionary<int, string> _groupPlacement;
        private NewResponse _responseFunction;
        private NewRightAnswer _rightAnswerFunction;
        private TextBlock _questionsBlock;
        private TextBlock _questionCountingBlock;

        public delegate void NewResponse(string a);
        public delegate void NewRightAnswer(int b);

        public QuestionManager(List<string> questions, Dictionary<int, string> groupsPlaces, NewResponse response, NewRightAnswer rightAnswer, TextBlock questionsBlock, TextBlock questionCountingBlock) {
            _managingQuestions = questions; //Пихаем в глобальные переменные
            _groupPlacement = groupsPlaces;
            _responseFunction = response;
            _rightAnswerFunction = rightAnswer;
            _questionsBlock = questionsBlock;
            _questionCountingBlock = questionCountingBlock;
        }

        public void Start() {
            Thread workerThread = new Thread(Worker);
            workerThread.Start(SynchronizationContext.Current);
        }

        public void Worker(object state) {
            foreach (string question in _managingQuestions) {
                //SynchronizationContext context = state as SynchronizationContext;
                //context.Post(() => {

                //});
                _isTimeStarted = false;
                while (!_isRightAnswer) { } //Топ КПД цикл (ограничивает перебор
                _isRightAnswer = false;
            }

        }

        public void RightAnswer() {
            if (!_isTimeRuns) {
                return;
            }

            _isRightAnswer = true;
            _isTimeRuns = false;
            _isTimeStarted = false;

            _rightAnswerFunction.Invoke(_questionPrise); //Invoke - вызов для делегата
        }

        public void WrongAnswer() {
            if (!_isTimeRuns) {
                return;
            }

            _isRightAnswer = false;
            _isTimeRuns = false;
            _isTimeStarted = true;
        }

        public void StartTime() {
            _isTimeRuns = true;
            
        }

    }
}
