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
                                                            "WHERE Questions.RoundId = {0} AND Questions.Id > {1}";
        private const string GET_QUIZ_INFO_COMMAND = "SELECT * FROM QuizInfo";
        private const string GET_PLAYERS_INFO_COMMAND = "SELECT * FROM Players";
        private const string SET_ROUND_ID_COMMAND = "UPDATE QuizInfo SET CurrentRound = {0}";
        private const string SET_QUESTION_ID_COMMAND = "UPDATE QuizInfo SET CurrentQuestion = {0}";
        private const string ADD_PLAYER_COMMAND = "INSERT INTO Players (Id, PlayerName, PointsCount) VALUES ({0}, N'{1}', 0)";
        private const string UPDATE_PLAYER_NAME_COMMAND = "UPDATE Players SET PlayerName = N'{0}' WHERE Id = {1}";
        private const string UPDATE_PLAYER_POINTS_COMMAND = "UPDATE Players SET PointsCount = {0} WHERE Id = {1}";
        private const string DELETE_PLAYER_COMMAND = "DELETE FROM Players WHERE Id = {0}";


        private string _connectionString;

        private int roundIndex = 1;
        private int startQuestionIndex = 0;

        private QuizInfo currentPlayersInfo;

        /// <summary>
        /// Initializate db connector
        /// </summary>
        public void Init() {
            _connectionString = ConfigurationManager.ConnectionStrings["Quiz.Properties.Settings.QuestionsBaseConnectionString"].ConnectionString;
        }

        public List<Question> GetQuestions() {
            List<Question> questionsList = new List<Question>();

            SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            
            using (SqlDataAdapter dataAdapter = new SqlDataAdapter(string.Format(GET_QUESTIONS_COMMAND, roundIndex, startQuestionIndex), connection)) {
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
                        q.Id = currentId;
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

            connection.Close();

            return questionsList;
        }

        public QuizInfo GetFullInfo() {
            QuizInfo info = new QuizInfo();

            SqlConnection connection = new SqlConnection(_connectionString);            

            using (SqlDataAdapter quizInfoAdapter = new SqlDataAdapter(GET_QUIZ_INFO_COMMAND, connection)) {
                DataTable quizInfoTable = new DataTable();
                quizInfoAdapter.Fill(quizInfoTable);

                roundIndex = Convert.ToInt32(quizInfoTable.Rows[0].ItemArray[0]);
                info.RoundIndex = roundIndex;

                startQuestionIndex = Convert.ToInt32(quizInfoTable.Rows[0].ItemArray[1]);

            }

            using (SqlDataAdapter playersAdapter = new SqlDataAdapter(GET_PLAYERS_INFO_COMMAND, connection)) {
                DataTable playersTable = new DataTable();
                playersAdapter.Fill(playersTable);

                foreach (DataRow player in playersTable.Rows) {
                    info.PlayersNames.Add(player.ItemArray[1].ToString());
                    info.PlayersPoints.Add(Convert.ToInt32(player.ItemArray[2]));
                }
            }

            connection.Close();

            currentPlayersInfo = info;
            return info;
        }

        public void AddOrUpdatePlayerInfo(int playerIndex, string playerName, int points) {
            if (playerIndex >= currentPlayersInfo.PlayersNames.Count) {
                AddPlayer(playerName);
                currentPlayersInfo.PlayersNames.Add(playerName);
                currentPlayersInfo.PlayersPoints.Add(0);
                return;
            }

            if (currentPlayersInfo.PlayersNames[playerIndex] != playerName) {
                currentPlayersInfo.PlayersNames[playerIndex] = playerName;
                UpdateName(playerIndex, playerName);
            }
        }

        public void DeletePlayer(int playerIndex) {
            if (playerIndex >= currentPlayersInfo.PlayersNames.Count) { return; }

            currentPlayersInfo.PlayersNames.RemoveAt(playerIndex);
            currentPlayersInfo.PlayersPoints.RemoveAt(playerIndex);

            using (SqlConnection connection = new SqlConnection(_connectionString)) {
                connection.Open();

                SqlCommand command = new SqlCommand(string.Format(DELETE_PLAYER_COMMAND, playerIndex), connection);
                command.ExecuteNonQuery();
            }
        }

        public void UpdateCurrentQuestion(int value)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString)) {
                connection.Open();

                SqlCommand command = new SqlCommand(string.Format(SET_QUESTION_ID_COMMAND, value), connection);
                command.ExecuteNonQuery();
            }

            startQuestionIndex = value;
        }

        public void UpdateCurrentRound(int value)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString)) {
                SqlCommand command = new SqlCommand(string.Format(SET_ROUND_ID_COMMAND, value));
                command.ExecuteNonQuery();
            }

            roundIndex = value;
        }

        public void UpdatePoints(int playerIndex, int newPointsValue)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString)) {
                connection.Open();

                SqlCommand command = new SqlCommand(string.Format(UPDATE_PLAYER_POINTS_COMMAND, newPointsValue, playerIndex), connection);
                command.ExecuteNonQuery();
            }
        }

        private void AddPlayer(string playerName)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString)) {
                connection.Open();

                SqlCommand command = new SqlCommand(string.Format(ADD_PLAYER_COMMAND, currentPlayersInfo.PlayersNames.Count, playerName), connection);
                command.ExecuteNonQuery();
            }
        }

        

        private void UpdateName(int playerIndex, string name)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString)) {
                connection.Open();

                SqlCommand command = new SqlCommand(string.Format(UPDATE_PLAYER_NAME_COMMAND, name, playerIndex), connection);
                command.ExecuteNonQuery();
            }
        }
    }
}
