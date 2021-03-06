﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Helpers;
using Microsoft.Ajax.Utilities;
using SimpleVoter.Core;
using SimpleVoter.Core.Enums;
using SimpleVoter.Core.Models;
using SimpleVoter.Core.Repositories;

namespace SimpleVoter.Persistence.Repositories
{
    public class PollRepository : IPollRepository
    {
        protected readonly IApplicationDbContext Context;
        public PollRepository(IApplicationDbContext context)
        {
            Context = context;
        }

        public Poll GetSingle(int id)
        {
            return Context.Polls.Include(p => p.User).Include(p => p.Answers).Single(a => a.Id == id);
        }

        public IEnumerable<Poll> GetAll(PollTableInfo tableInfo, string userId = "", bool isAdmin = false)
        {
            IQueryable<Poll> pollQuery;
            if (userId == "" && !isAdmin)
            {
                pollQuery = Context.Polls
                    .Include(p => p.User)
                    .Where(p =>
                        p.Question.Contains(tableInfo.SearchText) ||
                        p.Id.ToString().Contains(tableInfo.SearchText) ||
                        p.Visibility.ToString().Contains(tableInfo.SearchText) ||
                        p.User.UserName.Contains(tableInfo.SearchText) ||
                        (p.User.UserName == null && "Anonymous".Contains(tableInfo.SearchText)) ||
                        tableInfo.SearchText == ""
                    )
                    .Where(p =>
                        (p.ExpirationDate != null
                         && p.ExpirationDate.Value > DateTime.Now
                         || p.ExpirationDate == null)
                        && p.Visibility == Visibility.Public);
            }
            else if (isAdmin)
            {
                pollQuery = Context.Polls
                    .Include(p => p.User)
                    .Where(p =>
                        p.Question.Contains(tableInfo.SearchText) ||
                        p.Id.ToString().Contains(tableInfo.SearchText) ||
                        p.Visibility.ToString().Contains(tableInfo.SearchText) ||
                        p.User.UserName.Contains(tableInfo.SearchText) ||
                        (p.User.UserName == null && "Anonymous".Contains(tableInfo.SearchText)) ||
                        tableInfo.SearchText == ""
                    );
            }
            else {
                pollQuery = Context.Polls
                    .Where(p => p.UserId == userId || userId == "")
                    .Where(p =>
                        p.Question.Contains(tableInfo.SearchText) ||
                        p.Id.ToString().Contains(tableInfo.SearchText) ||
                        p.Visibility.ToString().Contains(tableInfo.SearchText) ||
                        tableInfo.SearchText == ""
                    );
            }

            IEnumerable <Poll> polls = null;

            switch (tableInfo.SortBy)
            {
                case SortBy.Id:
                    polls = tableInfo.SortDirection == SortDirection.Ascending
                        ? pollQuery.OrderBy(p => p.Id)
                        : pollQuery.OrderByDescending(p => p.Id);
                    break;
                    case SortBy.Question:
                    polls = tableInfo.SortDirection == SortDirection.Ascending 
                        ? pollQuery.OrderBy(p => p.Question)
                        : pollQuery.OrderByDescending(p => p.Question);
                    break;
                    case SortBy.UserName:
                    polls = tableInfo.SortDirection == SortDirection.Ascending
                        ? pollQuery.OrderBy(p => p.User.UserName)
                        : pollQuery.OrderByDescending(p => p.User.UserName);
                    break;
                    case SortBy.Visibility:
                    polls = tableInfo.SortDirection == SortDirection.Ascending
                        ? pollQuery.OrderBy(p => p.Visibility)
                        : pollQuery.OrderByDescending(p => p.Visibility);
                    break;
            }

            tableInfo.PagingInfo.AllItems = polls.Count();
            tableInfo.PagingInfo.AllPages =
                (int) Math.Ceiling((double)tableInfo.PagingInfo.AllItems / tableInfo.PagingInfo.ItemsPerPage);
            return polls
                .Skip((tableInfo.PagingInfo.CurrentPage-1) * tableInfo.PagingInfo.ItemsPerPage)
                .Take(tableInfo.PagingInfo.ItemsPerPage)
                .ToList();
        }

        public void Add(Poll poll)
        {
            Context.Polls.Add(poll);
        }

        public void Remove(Poll poll)
        {
            Context.Polls.Remove(poll);
        }

        public int Count()
        {
            return Context.Polls.Count();
        }

        public int Count(Expression<Func<Poll, bool>> expression)
        {
            return Context.Polls.Count(expression);
        }

        public IEnumerable<Answer> GetAnswers(int pollId)
        {
            return Context.Polls.Single(p => p.Id == pollId).Answers.ToList();
        }

        public IEnumerable<Answer> GetBestAnswers(int pollId)
        {
            var poll = Context.Polls.Single(p => p.Id == pollId);

            return poll.Answers
                .Where(a => a.Votes != 0
                        && a.Votes == poll.Answers
                                .OrderByDescending(an => an.Votes)
                                .First().Votes).ToList();
        }
    }
}