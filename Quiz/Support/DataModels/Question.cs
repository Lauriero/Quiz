using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quiz.Support.DataModels
{
    class Question
    {
        public int Id { get; set; }
        public double Points { get; set; }
        public int TimeToAnswer { get; set; }
        public string QuestionText { get; set; }
        public string RightAnswer { get; set; }
        public List<Answer> Answers { get; set; }
        public Uri VideoPath { get; set; }
        public Uri ImagePath { get; set; }
        public Uri AnswerVideoPath { get; set; }
        public Uri AnswerImagePath { get; set; }

    }
}
