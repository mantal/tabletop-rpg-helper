using FluentAssertions;
using RPG.Engine.Services;
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
            _statService.Add("FOR", "DEX").Should().BeEmpty();
        }

		[Fact]
		public void AddWithSelfVariableModifier()
		{
			_statService.Add("FOR", ":Base").Should().BeEmpty();
			_statService.GetValue("FOR:Base").Should().Be(0);
		}

		[Fact]
		public void AddWithVariableModifier()
		{
			_statService.Add("DEX", ":Base").Should().BeEmpty();
			_statService.Add("FOR", "DEX:Base").Should().BeEmpty();

			_statService.GetValue("FOR").Should().Be(0);
		}

		[Fact]
		public void AddWithModifiers()
		{
			const string modifiers = "B - C * D / E + 1 - 2 * 3 / 4";

			_statService.Add("B");
			_statService.Add("C");
			_statService.Add("D");
			_statService.Add("E");

			_statService.Add("A", 418, modifiers).Should().BeEmpty();

			_statService.Get("A").ToString().Should().Be("A:base + " + modifiers);
		}

		[Fact]
        public void AddNotAndDetectUnknownId()
        {
            _statService.Add("FOR", "DEX").Should().HaveCount(1);
        }

		[Fact]
		public void AddNotAndDetectInvalidId()
		{
			_statService.Add("1FOR", "DEX").Should().HaveCount(1);
		}

		[Fact]
		public void AddNotAndDetectVariableId()
		{
			_statService.Add("DEX");
			_statService.Add("FOR", "DEX:no").Should().HaveCount(1);
		}

		[Fact]
		public void AddNotAndDetectDuplicateId()
		{
			_statService.Add("FOR");
			_statService.Add("FOR", "DEX").Should().HaveCount(2);
		}

        [Fact]
        public void AddNotAndDetectFirstLevelCircularDependency()
        {
            _statService.Add("FOR", "FOR").Should().HaveCount(1);
        }

        [Fact]
        public void AndNotAndDetectDeepCircularDependency()
        {
            _statService.Add("B", "3");
            _statService.Add("E", "B");
            _statService.Add("C", "E");
            _statService.Add("F", "A");
            _statService.Add("D", "F");

            _statService.Add("A", "B + C + D").Should().HaveCount(1);
        }

        #endregion

#region Update

		[Fact]
		public void Update()
		{
			_statService.Add("FOR");

			_statService.Update("FOR", "418").Should().BeEmpty();

			_statService.Get("FOR").ToString().Should().Be("418");
		}

		[Fact]
		public void UpdateWithStatModifier()
		{
			_statService.Add("FOR");
			_statService.Add("DEX");

			_statService.Update("FOR", "DEX").Should().BeEmpty();

			_statService.Get("FOR").ToString().Should().Be("DEX");
		}

		[Fact]
		public void UpdateWithModifiers()
		{
			const string modifiers = "B - C * D / E + 1 - 2 * 3 / 4";

			_statService.Add("A");
			_statService.Add("B");
			_statService.Add("C");
			_statService.Add("D");
			_statService.Add("E");

			_statService.Update("A", modifiers).Should().BeEmpty();

			_statService.Get("A").ToString().Should().Be(modifiers);
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
			_statService.Update("FOR", "FOR").Should().HaveCount(1);
		}

		[Fact]
		public void UpdateNotAndDetectDeepCircularDependency()
		{
			_statService.Add("A");
			_statService.Add("B", "3");
			_statService.Add("E", "B");
			_statService.Add("C", "E");
			_statService.Add("F", "A");
			_statService.Add("D", "F");

			_statService.Update("A", "B + C + D").Should().HaveCount(1);
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
			_statService.Add("DEX", "FOR");
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
			_statService.Add("DEX", "FOR");
			_statService.Remove("FOR", false).Should().HaveCount(1);
			_statService.Exists("FOR").Should().BeTrue();
		}

#endregion

#region Resolve

		[Fact]
		public void ResolveWithoutModifier()
		{
			_statService.Add("A");

			_statService.GetValue("A").Should().Be(0);
		}

		[Fact]
		public void ResolveWithStaticModifier()
		{
			_statService.Add("A", "2");

			_statService.GetValue("A").Should().Be(2);
		}

		[Fact]
		public void ResolveWithStatModifier()
		{
			_statService.Add("A", "2");
			_statService.Add("B", "A");

			_statService.GetValue("B").Should().Be(2);
		}

		[Fact]
		public void ResolveWithVariableModifier()
		{
			_statService.Add("A", 1);

			_statService.GetValue("A").Should().Be(1);
		}

		[Fact]
		public void ResolveWithMixedModifiers()
		{
			_statService.Add("A", 5);
			_statService.Add("B", 2);
			_statService.Add("C", 0, "A * B + A * B - A * 2 / 1");

			_statService.GetValue("C").Should().Be(10);
		}

		#endregion
	}
}