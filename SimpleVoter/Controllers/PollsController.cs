﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;
using SimpleVoter.Core;
using SimpleVoter.Core.Enums;
using SimpleVoter.Core.Models;
using SimpleVoter.Core.ViewModels.PollViewModels;

namespace SimpleVoter.Controllers
{
    [Authorize]
    public class PollsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public PollsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        //TODO: Add ability to change email
        //TODO: Add ability to choose unique user name on registration and display it instead of email
        //TODO: Add option 'Only logged in users can display'
        //TODO: Add option 'Only logged in users can vote'
        //TODO: Add visibility 'Personal' where only users invited by owner can vote
        //TODO: Auto deletion anonymous polls after 24h using sql server agent - job schedule

        [AllowAnonymous]
        public ActionResult ShowPublicPolls()
        {
            var pagingInfo = new PagingInfo
            {
                ItemsPerPage = Int32.Parse(WebConfigurationManager.AppSettings["PollsPerPage"]),
                CurrentPage = 1
            };

            return View("PollList", pagingInfo);
        }

        [AllowAnonymous]
        public ActionResult RenderPollTable(string json)
        {
            var tableInfo = JsonConvert.DeserializeObject<PollTableInfo>(json);
            var polls = _unitOfWork.Polls.GetAll(tableInfo);

            var viewModelList = new List<PollListViewModel>();
            var userManager = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();

            foreach (var poll in polls)
            {
                viewModelList.Add(new PollListViewModel
                {
                    PollId = poll.Id,
                    Question = poll.Question,
                    UserName = poll.UserId == null ? "Anonymous" : userManager.Users.Single(u => u.Id == poll.UserId).UserName
                });
            }

            return PartialView("_PollsTable", new Tuple<IEnumerable<PollListViewModel>, PagingInfo>(viewModelList, tableInfo.PagingInfo));
        }

        public ActionResult ShowUserPolls()
        {
            var pagingInfo = new PagingInfo
            {
                ItemsPerPage = Int32.Parse(WebConfigurationManager.AppSettings["PollsPerPage"]),
                CurrentPage = 1
            };

            return View("UserPollList", pagingInfo);
        }

        public ActionResult RenderUserPollTable(string json)
        {
            var tableInfo = JsonConvert.DeserializeObject<PollTableInfo>(json);

            var polls = _unitOfWork.Polls.GetAll(tableInfo, User.Identity.GetUserId());

            var viewModelList = new List<PollListViewModel>();
            var userManager = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();

            foreach (var poll in polls)
            {
                viewModelList.Add(new PollListViewModel
                {
                    PollId = poll.Id,
                    Question = poll.Question,
                    UserName = poll.UserId == null ? "Anonymous" : userManager.Users.Single(u => u.Id == poll.UserId).UserName,
                    ExpirationDate = poll.ExpirationDate,
                    Visibility = poll.Visibility
                });
            }

            return PartialView("_UserPollsTable", new Tuple<IEnumerable<PollListViewModel>, PagingInfo>(viewModelList, tableInfo.PagingInfo));
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult ShowAllPolls()
        {
            var pagingInfo = new PagingInfo
            {
                ItemsPerPage = int.Parse(WebConfigurationManager.AppSettings["PollsPerPage"]),
                CurrentPage = 1
            };

            return View("AdminPollList", pagingInfo);
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult RenderAdminPollTable(string json)
        {
            var tableInfo = JsonConvert.DeserializeObject<PollTableInfo>(json);

            var polls = _unitOfWork.Polls.GetAll(tableInfo, "", true);

            var viewModelList = new List<PollListViewModel>();
            var userManager = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();

            foreach (var poll in polls)
            {
                viewModelList.Add(new PollListViewModel
                {
                    PollId = poll.Id,
                    Question = poll.Question,
                    UserName = poll.UserId == null ? "Anonymous" : userManager.Users.Single(u => u.Id == poll.UserId).UserName,
                    ExpirationDate = poll.ExpirationDate,
                    Visibility = poll.Visibility
                });
            }

            return PartialView("_AdminPollsTable", new Tuple<IEnumerable<PollListViewModel>, PagingInfo>(viewModelList, tableInfo.PagingInfo));
        }

        [AllowAnonymous]
        public ActionResult Details(int id)
        {
            var poll = _unitOfWork.Polls.GetSingle(id);
            return View(poll);
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            var poll = _unitOfWork.Polls.GetSingle(id);
            var viewModel = new UpdateViewModel
            {
                Id = poll.Id,
                Question = poll.Question,
                AllowMultipleAnswers = poll.AllowMultipleAnswers,
                Answers = poll.Answers.ToList(),
                ExpirationDate = poll.ExpirationDate,
                Visibility = poll.Visibility,
                ChartType = poll.ChartType
            };

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult Update(UpdateViewModel viewModel)
        {
            if (viewModel != null)
            {
                var poll = _unitOfWork.Polls.GetSingle(viewModel.Id);
                if (ModelState.IsValid)
                {
                    if (poll.Visibility != viewModel.Visibility)
                    {
                        if (viewModel.Visibility == Visibility.Public)
                        {
                            _unitOfWork.DailyStatistics.Increase_NewPublicPolls();
                            _unitOfWork.DailyStatistics.Increase_DeletedPrivatePolls();
                        }
                        else if (viewModel.Visibility == Visibility.Private)
                        {
                            _unitOfWork.DailyStatistics.Increase_NewPrivatePolls();
                            _unitOfWork.DailyStatistics.Increase_DeletedPublicPolls();
                        }
                    }

                    if (poll.Answers.All(a => a.Votes == 0))
                    {
                        poll.Question = viewModel.Question;
                        _unitOfWork.Answers.RemoveRange(poll.Answers.ToList());
                        poll.Answers = viewModel.Answers
                            .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                            .DistinctBy(a => a.Content).ToList();
                    }

                    poll.AllowMultipleAnswers = viewModel.AllowMultipleAnswers;
                    poll.UpdateDate = DateTime.Now;
                    poll.ExpirationDate = viewModel.ExpirationDate;
                    poll.Visibility = viewModel.Visibility;
                    poll.ChartType = viewModel.ChartType;

                    _unitOfWork.Complete();
                    return RedirectToAction("Details", new { id = poll.Id });
                }

                if (poll.Answers.Any(a => a.Votes > 0))
                    ViewBag.ContainsVotes = true;
                else
                    ViewBag.ContainsVotes = false;
                viewModel.Answers = viewModel.Answers.Where(a => !a.Content.IsNullOrWhiteSpace()).ToList();
            }

            return View(viewModel);
        }

        public ActionResult Delete(int id)
        {
            var poll = _unitOfWork.Polls.GetSingle(id);
            if (poll.Visibility == Visibility.Public)
                _unitOfWork.DailyStatistics.Increase_DeletedPublicPolls();
            else if (poll.Visibility == Visibility.Private)
                _unitOfWork.DailyStatistics.Increase_DeletedPrivatePolls();

            _unitOfWork.Polls.Remove(poll);
            _unitOfWork.Complete();

            return Json(new { success = true });
        }

        public ActionResult End(int id)
        {
            var poll = _unitOfWork.Polls.GetSingle(id);
            poll.ExpirationDate = DateTime.Now;
            poll.UpdateDate = DateTime.Now;
            poll.EndedByOwner = true;
            _unitOfWork.Complete();

            return Json(new { success = true });
        }

        public ActionResult Renew(int id, DateTime expirationDate)
        {
            if (expirationDate <= DateTime.Now)
                return Json(new { success = false, responseText = "Date must be greater than current date!" });

            var poll = _unitOfWork.Polls.GetSingle(id);
            poll.ExpirationDate = expirationDate;
            poll.UpdateDate = DateTime.Now;
            poll.RenewingDate = DateTime.Now;
            _unitOfWork.Complete();

            return Json(new { success = true });
        }

        [AllowAnonymous]
        public ActionResult Vote(int[] ids, DateTime? expirationDate)
        {
            if (expirationDate != null && DateTime.Now > expirationDate.Value)
                return Json(new { success = false, responseText = "Poll has expired." });

            var userIp = Request.UserHostAddress;
            var poll = _unitOfWork.Answers.GetPoll(ids[0]);

            if (Session["Voted-Poll" + poll.Id] != null || Request.Cookies["Voted-Poll" + poll.Id] != null || _unitOfWork.UniqueVisitors.HasAnsweredPoll(userIp, poll.Id))
            {
                if (Session["Voted-Poll" + poll.Id] == null)
                    Session["Voted-Poll" + poll.Id] = true;

                if (Request.Cookies["Voted-Poll" + poll.Id] == null)
                {
                    var cookie = new HttpCookie("Voted-Poll" + poll.Id, "true");
                    cookie.Expires.AddYears(10);
                    Response.SetCookie(cookie);
                }

                return Json(new { success = false, responseText = "You've already participated in this poll." });
            }
            
            foreach (var id in ids)
            {
                var answer = _unitOfWork.Answers.Get(id);
                answer.Votes++;
            }

            Session["Voted-Poll" + poll.Id] = true;
            var httpCookie = new HttpCookie("Voted-Poll" + poll.Id, "true");
            httpCookie.Expires.AddYears(10);
            Response.SetCookie(httpCookie);

            if (_unitOfWork.UniqueVisitors.Exists(userIp))
            {
                var user = _unitOfWork.UniqueVisitors.Get(userIp);
                user.PollsParticipated.Add(poll);
            }
            else
            {
                var visitor = new UniqueVisitor
                {
                    IpAdress = userIp,
                    PollsParticipated = new List<Poll> {poll}
                };
                _unitOfWork.UniqueVisitors.Add(visitor);
            }

            _unitOfWork.Complete();
            return Json(new { success = true });
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Create(CreateViewModel viewModel)
        {
            if (!User.Identity.IsAuthenticated)
            {
                viewModel.ExpirationDate = DateTime.Now.AddDays(1);
                viewModel.Visibility = Visibility.Public;
                viewModel.ChartType = ChartType.Doughnut;
            }

            if (viewModel != null && ModelState.IsValid)
            {
                var poll = new Poll
                {
                    AllowMultipleAnswers = viewModel.AllowMultipleAnswers,
                    Question = viewModel.Question,
                    UserId = User.Identity.GetUserId(),
                    Answers = viewModel.Answers
                                .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                                .DistinctBy(a => a.Content).ToList(),
                    CreationDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    ExpirationDate = viewModel.ExpirationDate,
                    Visibility = viewModel.Visibility,
                    ChartType = viewModel.ChartType
                };

                if (poll.Visibility == Visibility.Public)
                    _unitOfWork.DailyStatistics.Increase_NewPublicPolls();
                else if (poll.Visibility == Visibility.Private)
                    _unitOfWork.DailyStatistics.Increase_NewPrivatePolls();

                _unitOfWork.Polls.Add(poll);
                _unitOfWork.Complete();

                return RedirectToAction("Details", new { id = poll.Id });
            }

            return View(viewModel);
        }
    }
}