using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quiz.Support.DataModels
{
    class Question
    {
        public string id { get; set; }
        public int points { get; set; }
        public int timeToAnswer { get; set; }
        public string questionText { get; set; }
        public List<Answer> answers { get; set; }
        public string videoPath { get; set; }
        public bool isVideoPathRelative { get; set; }

    }
}
