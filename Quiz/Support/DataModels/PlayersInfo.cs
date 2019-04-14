using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quiz.Support.DataModels
{
    class QuizInfo
    {
        public int RoundIndex { get; set; }

        public List<string> PlayersNames { get; set; }
        public List<int> PlayersPoints { get; set; }

        public QuizInfo() {
            RoundIndex = 0;
            PlayersNames = new List<string>();
            PlayersPoints = new List<int>();
        }
    }
}
