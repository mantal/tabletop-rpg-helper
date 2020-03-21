﻿using FluentAssertions;
using RPG.Services;
using Xunit;

namespace RPG.Tests
{
    public class StatServiceTests
    {
        private readonly StatService _statService = new StatService();

#region Add
        [Fact]
        public void Add()
        {
            _statService.Add("FOR").Should().BeEmpty();
        }

        [Fact]
        public void AddWithModifier()
        {
            _statService.Add("DEX");
            _statService.Add("FOR", 0, "DEX").Should().BeEmpty();
        }

		[Fact]
		public void AddWithModifiers()
		{
			const string modifiers = "+ B - C * D / E + 1 - 2 * 3 / 4";

			_statService.Add("B");
			_statService.Add("C");
			_statService.Add("D");
			_statService.Add("E");
			_statService.Add("A", 418, modifiers).Should().BeEmpty();
			_statService.Get("A").ToString().Should().Be("418 " + modifiers);
		}

		[Fact]
        public void AddNotAndDetectUnknownId()
        {
            _statService.Add("FOR", 0, "DEX").Should().HaveCount(1);
        }

		[Fact]
		public void AddNotAndDetectInvalidId()
		{
			_statService.Add("1FOR", 0, "DEX").Should().HaveCount(1);
		}

        [Fact]
		public void AddNotAndDetectDuplicateId()
		{
			_statService.Add("FOR");
			_statService.Add("FOR", 0, "DEX").Should().HaveCount(2);
		}

        [Fact]
        public void AddNotAndDetectFirstLevelCircularDependency()
        {
            _statService.Add("FOR", 0, "FOR").Should().HaveCount(1);
        }

        [Fact]
        public void AndNotAndDetectDeepCircularDependency()
        {
            _statService.Add("B", 0, "3");
            _statService.Add("E", 0, "B");
            _statService.Add("C", 0, "E");
            _statService.Add("F", 0, "A");
            _statService.Add("D", 0, "F");

            _statService.Add("A", 0, "B + C + D").Should().HaveCount(1);
        }

        #endregion

        #region Update

		[Fact]
		public void Update()
		{
			_statService.Add("FOR");
			_statService.Update("FOR", 418).Should().BeEmpty();
			_statService.Get("FOR").ToString().Should().Be("418");
		}

		[Fact]
		public void UpdateWithModifier()
		{
			_statService.Add("FOR");
			_statService.Add("DEX");
			_statService.Update("FOR", 418, "DEX").Should().BeEmpty();
			_statService.Get("FOR").ToString().Should().Be("418 + DEX");
		}

		[Fact]
		public void UpdateWithModifiers()
		{
			const string modifiers = "+ B - C * D / E + 1 - 2 * 3 / 4";

			_statService.Add("A");
			_statService.Add("B");
			_statService.Add("C");
			_statService.Add("D");
			_statService.Add("E");
			_statService.Update("A", 418, modifiers).Should().BeEmpty();
			_statService.Get("A").ToString().Should().Be("418 " + modifiers);
		}

		[Fact]
		public void UpdateNotAndDetectUnknownId()
		{
			_statService.Update("FOR").Should().HaveCount(1);
		}

		[Fact]
		public void UpdateNotAndDetectFirstLevelCircularDependency()
		{
			_statService.Add("FOR");
			_statService.Update("FOR", 0, "FOR").Should().HaveCount(1);
		}

		[Fact]
		public void UpdateNotAndDetectDeepCircularDependency()
		{
			_statService.Add("A");
			_statService.Add("B", 0, "3");
			_statService.Add("E", 0, "B");
			_statService.Add("C", 0, "E");
			_statService.Add("F", 0, "A");
			_statService.Add("D", 0, "F");

			_statService.Update("A", 0, "B + C + D").Should().HaveCount(1);
		}

        #endregion

        #region Delete

        [Fact]
		public void Delete()
		{
			_statService.Add("FOR");
			_statService.Remove("FOR").Should().BeEmpty();
			_statService.Exists("FOR").Should().BeFalse();
		}

		[Fact]
		public void DeleteAndCascade()
		{
			_statService.Add("FOR");
			_statService.Add("DEX", 0, "FOR");
			_statService.Remove("FOR", true).Should().BeEmpty();
			_statService.Exists("FOR").Should().BeFalse();
			_statService.Exists("DEX").Should().BeFalse();
		}

		[Fact]
		public void DeleteNotAndDetectUnknownId()
		{
			_statService.Remove("FOR").Should().HaveCount(1);
		}

        [Fact]
		public void DeleteNotAndDetectDependencies()
		{
			_statService.Add("FOR");
			_statService.Add("DEX", 0, "FOR");
			_statService.Remove("FOR", false).Should().HaveCount(1);
			_statService.Exists("FOR").Should().BeTrue();
		}

        #endregion
    }
}
