using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quiz.Support.DataModels
{
    class Answer
    {
        public string type { get; set; }
        public string answerText { get; set; }

        public Answer(string type, string text)
        {
            this.type = type;
            this.answerText = text;
        }
    }
}
