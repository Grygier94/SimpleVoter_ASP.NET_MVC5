﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleVoter.Core;
using SimpleVoter.Core.Models;
using SimpleVoter.Persistence;
using SimpleVoter.Persistence.Repositories;
using SimpleVoter.Tests.Extensions;

namespace SimpleVoter.Tests.Persistence.Repositories
{
    [TestClass]
    public class PollRepositoryTests
    {
        private PollRepository _pollRepository;
        private Mock<IDbSet<Poll>> _mockPolls;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockPolls = new Mock<IDbSet<Poll>>();
            var mockContext = new Mock<IApplicationDbContext>();

            mockContext.SetupGet(c => c.Polls).Returns(_mockPolls.Object);
            _pollRepository = new PollRepository(mockContext.Object);
        }

        [TestMethod]
        public void Get_PollDoesntExist_ShouldThrowNullReferenceException()
        {
            _pollRepository.Invoking(m => m.Get(1)).ShouldThrow<NullReferenceException>();
        }

        [TestMethod]
        public void Get_PollExists_ShouldBeReturned()
        {
            var poll = new Poll { Id = 1, Question = "Question1" };
            _mockPolls.SetSource(new[] { poll });

            var pollFromDb = _pollRepository.Get(1);

            pollFromDb.Should().NotBeNull();
            pollFromDb.Should().Be(poll);
        }

        [TestMethod]
        public void GetAnswers_PollDoesntExist_ShouldThrowNullReferenceException()
        {
            _pollRepository.Invoking(m => m.GetAnswers(1)).ShouldThrow<NullReferenceException>();
        }

        [TestMethod]
        public void GetPollAnswers_PollHasNoAnswers_ShouldReturnNull()
        {
            var poll = new Poll { Id = 1, Question = "Question1" };
            _mockPolls.SetSource(new[] { poll });

            var answers = _pollRepository.GetAnswers(1);

            answers.Should().BeEmpty();
        }

        [TestMethod]
        public void GetAnswers_PollExistsAndHasOneAnswer_ShouldReturnOneAnswer()
        {
            var answer1 = new Answer { Id = 1, Content = "Content1", PollId = 1, Votes = 3 };
            var answerList = new List<Answer> { answer1 };
            var poll = new Poll { Id = 1, Question = "Question1", Answers = answerList };

            _mockPolls.SetSource(new[] { poll });

            var answers = _pollRepository.GetAnswers(1);

            answers.Should().Contain(answerList);
        }

        [TestMethod]
        public void GetAnswers_PollExistsAndHasSeveralAnswers_ShouldReturnSeveralAnswers()
        {
            var answer1 = new Answer { Id = 1, Content = "Content1", PollId = 1, Votes = 3 };
            var answer2 = new Answer { Id = 2, Content = "Content2", PollId = 1, Votes = 6 };
            var answer3 = new Answer { Id = 3, Content = "Content3", PollId = 1, Votes = 9 };
            var answerList = new List<Answer> { answer1, answer2, answer3 };
            var poll = new Poll { Id = 1, Question = "Question1", Answers = answerList };

            _mockPolls.SetSource(new[] { poll });

            var answers = _pollRepository.GetAnswers(1);

            answers.Should().Contain(answerList);
        }

        [TestMethod]
        public void GetBestAnswers_PollHasNoAnswers_ShouldNotBeReturned()
        {
            var poll = new Poll { Id = 1, Question = "Question1" };
            _mockPolls.SetSource(new[] { poll });

            var bestAnswers = _pollRepository.GetBestAnswers(1);
            bestAnswers.Should().BeEmpty();
        }

        [TestMethod]
        public void GetBestAnswers_PollHasOneBestAnswer_ShouldReturnListWithOneAnswer()
        {
            var answer1 = new Answer { Id = 1, Content = "Content1", PollId = 1, Votes = 3 };
            var answer2 = new Answer { Id = 2, Content = "Content2", PollId = 1, Votes = 6 };
            var answer3 = new Answer { Id = 3, Content = "Content3", PollId = 1, Votes = 9 };
            var answerList = new List<Answer> { answer1, answer2, answer3 };
            var poll = new Poll { Id = 1, Question = "Question1", Answers = answerList };

            _mockPolls.SetSource(new[] { poll });
            var bestAnswers = _pollRepository.GetBestAnswers(1);

            bestAnswers.Should().Contain(answer3);
            bestAnswers.Should().HaveCount(1);
        }

        [TestMethod]
        public void GetBestAnswers_PollHaveSeveralAnswersWithHigestVotes_ShouldReturnListWithSeveralAnswers()
        {
            var answer1 = new Answer { Id = 1, Content = "Content1", PollId = 1, Votes = 9 };
            var answer2 = new Answer { Id = 2, Content = "Content2", PollId = 1, Votes = 9 };
            var answer3 = new Answer { Id = 3, Content = "Content3", PollId = 1, Votes = 9 };
            var answer4 = new Answer { Id = 4, Content = "Content4", PollId = 1, Votes = 4 };
            var answerList = new List<Answer> { answer1, answer2, answer3, answer4 };
            var poll = new Poll { Id = 1, Question = "Question1", Answers = answerList };

            _mockPolls.SetSource(new[] { poll });

            var bestAnswers = _pollRepository.GetBestAnswers(1);
            var resultList = new List<Answer> { answer1, answer2, answer3 };
            bestAnswers.Should().Contain(resultList);
            bestAnswers.Should().HaveCount(3);
        }

        [TestMethod]
        public void GetBestAnswers_AnswersHasNoVotes_ShouldNotBeReturned()
        {
            var answer1 = new Answer { Id = 1, Content = "Content1", PollId = 1, Votes = 0 };
            var answer2 = new Answer { Id = 2, Content = "Content2", PollId = 1 };
            var answer3 = new Answer { Id = 3, Content = "Content3", PollId = 1, Votes = 0 };
            var answerList = new List<Answer> { answer1, answer2, answer3 };
            var poll = new Poll { Id = 1, Question = "Question1", Answers = answerList };

            _mockPolls.SetSource(new[] { poll });

            var bestAnswers = _pollRepository.GetBestAnswers(1);

            bestAnswers.Should().BeEmpty();
        }
    }
}
