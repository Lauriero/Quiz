using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quiz.Support.DataModels
{
    class MediaAnswer
    {
        public Uri AnswerVideoPath { get; set; }
        public Uri AnswerImagePath { get; set; }
        public AnswerKind Kind { get; set; }
    }
}
