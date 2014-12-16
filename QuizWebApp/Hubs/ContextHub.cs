using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using QuizWebApp.Models;

namespace QuizWebApp.Hubs
{
    [HubName("Context")]
    public class ContextHub : Hub
    {
        public void UpdateCurrentState(ContextStateType state)
        {
            using (var db = new QuizWebAppDb())
            {
                var context = db.Contexts.First();
                context.CurrentState = state;

                // if change state to "3:show answer", judge to all players.
                if (state == ContextStateType.ShowCorrectAnswer)
                {
                    var answers = db
                        .Answers
                        .Where(a => a.QuestionID == context.CurrentQuestionID)
                        .ToList();
                    var currentQuestion = db.Questions.Find(context.CurrentQuestionID);
                    var users = db.Users.ToList();

                    // If chosen option is correct, answer state is set to "Correct".
                    answers
                        .ForEach(a => a.Status =
                            a.ChosenOptionIndex == currentQuestion.IndexOfCorrectOption
                            ? AnswerStateType.Correct : AnswerStateType.Incorrect);

                    //配布ポイント決定
                    var correctAnswers = answers.Where(a => a.QuestionID == context.CurrentQuestionID && a.Status == AnswerStateType.Correct).ToList();
                    // **SORT**
                    correctAnswers.Sort((a, b) => a.Number - b.Number);

                    int additionalPointRatio = 80;
                    int totalNum = correctAnswers.Count();
                    int aPointNum = totalNum * additionalPointRatio / 100;
                    int distributePoint = 100;
                    int orderNum = 1;

                    //正解ユーザに配布
                     foreach (var a in correctAnswers)
                     {
                         var playerID = a.PlayerID;
                         var user = users.First(u => u.UserId == playerID);
                         int score = user.Score;
                         if(orderNum > totalNum - aPointNum)
                         {
                             user.Score = score + (orderNum - totalNum + aPointNum) * distributePoint / aPointNum;
                         }
                         else
                         {
                         	user.Score = score + 1;
                         }
                         orderNum ++;
                     }
                }
                db.SaveChanges();
            }

            Clients.All.CurrentStateChanged(state.ToString());
        }

        public void PlayerSelectedOptionIndex(int answerIndex)
        {
            using (var db = new QuizWebAppDb())
            {
            	// Userが選択した際の挙動
                var playerId = Context.User.Identity.UserId();
                var questionId = db.Contexts.First().CurrentQuestionID;
                var answer = db.Answers.First(a => a.PlayerID == playerId && a.QuestionID == questionId);
                answer.ChosenOptionIndex = answerIndex;
                answer.Status = AnswerStateType.Pending;/*entried*/
                //answer.AnsweredTime = DateTime.UtcNow;
                //★ AddArrivalNum
                var context = db.Contexts.First();
                answer.Number = context.ArrivalNum;
                context.ArrivalNum += 1;

                db.SaveChanges();
            }

            Clients.Others.PlayerSelectedOptionIndex();
        }
    }
}