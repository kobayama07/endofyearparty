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

                    // If chosen option is correct, answer state is set to "Correct".
                    answers
                        .ForEach(a => a.Status =
                            a.ChosenOptionIndex == currentQuestion.IndexOfCorrectOption
                            ? AnswerStateType.Correct : AnswerStateType.Incorrect);
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
                // answer.AnsweredTime = DateTime.UtcNow;

                db.SaveChanges();
            }

            Clients.Others.PlayerSelectedOptionIndex();
        }
    }
}