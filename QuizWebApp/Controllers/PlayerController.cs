using System;
using System.Linq;
using System.Web.Mvc;
using QuizWebApp.Models;

namespace QuizWebApp.Controllers
{
    [Authorize]
    public class PlayerController : Controller
    {
        public QuizWebAppDb DB { get; set; }

        public PlayerController()
        {
            this.DB = new QuizWebAppDb();
        }

        [HttpGet]
        public ActionResult Index()
        {
            var userInfo = this.DB.Users.Find(this.User.Identity.UserId());
            userInfo.AttendAsPlayerAt = DateTime.UtcNow;
            this.DB.SaveChanges();

            return View(this.DB);
        }

        [HttpGet]
        public ActionResult PlayerMainContent()
        {
            var context = this.DB.Contexts.First();
            var playerID = this.User.Identity.UserId();
            var questionID = this.DB.Contexts.First().CurrentQuestionID;
            var answer = this.DB.Answers.FirstOrDefault(a => a.PlayerID == playerID && a.QuestionID == questionID);
            if (answer == null)
            {
                answer = new Answer { PlayerID = playerID, QuestionID = questionID, ChosenOptionIndex = -1, Restriction = 0 };
                this.DB.Answers.Add(answer);
                this.DB.SaveChanges();
            }

            return PartialView("PlayerMainContent_" + context.CurrentState.ToString(), this.DB);
        }
    }
}
