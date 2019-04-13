using Quiz.Support.DataModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Quiz.Support
{
    class DataBaseWorker
    {
        private const string GET_QUESTIONS_COMMAND = "SELECT Questions.Id, QuestionText, Points, TimeToAnswer, RightAnswer, " +
                                                            "ImagePath, IsImagePathRelative, VideoPath, IsVideoPathRelative, " +
                                                            "AnswerText, AnswerSign FROM Questions " +
                                                            "LEFT JOIN Answers ON Questions.Id = Answers.Id AND Questions.RoundId = Answers.RoundId " +
                                                            "WHERE Questions.RoundId = {0} ";

        private string _connectionString;
        private int roundIndex = 0;

        public void Init(int roundId) {
            roundIndex = roundId;
            _connectionString = ConfigurationManager.ConnectionStrings["Quiz.Properties.Settings.QuestionsBaseConnectionString"].ConnectionString;
        }

        public List<Question> GetQuestions() {
            List<Question> questionsList = new List<Question>();

            using (SqlDataAdapter dataAdapter = new SqlDataAdapter(string.Format(GET_QUESTIONS_COMMAND, roundIndex), _connectionString)) {
                DataTable table = new DataTable();
                dataAdapter.Fill(table);

                int lastQuestionId = 0;
                List<Answer> answers = new List<Answer>();
                Question q = new Question();
                foreach (DataRow question in table.Rows) {
                    int currentId = (int)question.ItemArray[0];

                    if (lastQuestionId == currentId) {
                        answers.Add(new Answer(question.ItemArray[10].ToString(), question.ItemArray[9].ToString()));
                    } else {
                        if (answers.Count != 0) {
                            q.Answers = answers;
                        }
                        questionsList.Add(q);

                        answers = new List<Answer>();
                        if (!string.IsNullOrEmpty(question.ItemArray[9].ToString())) {
                            answers.Add(new Answer(question.ItemArray[10].ToString(), question.ItemArray[9].ToString()));
                        }

                        q = new Question();
                        q.QuestionText = question.ItemArray[1].ToString(); 
                        q.Points = (int)question.ItemArray[2];
                        q.TimeToAnswer = (int)question.ItemArray[3];
                        q.RightAnswer = question.ItemArray[4].ToString();

                        if (!string.IsNullOrEmpty(question.ItemArray[5].ToString())) {
                            if ((bool)question.ItemArray[6]) {
                                q.ImagePath = new Uri(Path.Combine(Environment.CurrentDirectory, question.ItemArray[5].ToString()));
                            } else {
                                q.ImagePath = new Uri(question.ItemArray[5].ToString());
                            }
                        }

                        if (!string.IsNullOrEmpty(question.ItemArray[7].ToString())) {
                            if ((bool)question.ItemArray[8]) {
                                q.VideoPath = new Uri(question.ItemArray[7].ToString(), UriKind.Relative);
                            } else {
                                q.VideoPath = new Uri(question.ItemArray[7].ToString());
                            }
                        }
                    }
                    lastQuestionId = currentId;
                }

                q.Answers = answers;
                questionsList.Add(q);
                questionsList.RemoveAt(0);
            }

            return questionsList;
        }
    }
}
