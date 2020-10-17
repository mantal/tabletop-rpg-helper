using FluentAssertions;
using RPG.Engine.Services;
using Xunit;

namespace RPG.Tests
{
    public class StatServiceTests
    {
        private readonly StatService _statService = new StatService(new FunctionService());

#region Add
        [Fact]
        public void AddEmpty()
        {
            _statService.Add("FOR").Should().BeEmpty();
        }

        [Fact]
        public void AddStat()
        {
            _statService.Add("DEX");
            _statService.Add("FOR", "DEX").Should().BeEmpty();
        }

		[Fact]
		public void AddShortVariable()
		{
			_statService.Add("FOR", ".base").Should().BeEmpty();
			_statService.GetValue("FOR.base").Should().Be(0);
		}

		[Fact]
		public void AddVariable()
		{
			_statService.Add("DEX", ".base").Should().BeEmpty();
			_statService.Add("FOR", "DEX.base").Should().BeEmpty();

			_statService.GetValue("FOR").Should().Be(0);
		}

		[Fact]
		public void AddMixed()
		{
			const string expression = "A.base + B - C * D / E + 1 - 2 * 3 / 4";

			_statService.Add("B");
			_statService.Add("C");
			_statService.Add("D");
			_statService.Add("E");

			_statService.Add("A", expression).Should().BeEmpty();

			_statService.Get("A").ToString().Should().Be($"[0, {expression}]");
		}

		[Fact]
        public void AddNotAndDetectUnknownStat()
        {
            _statService.Add("FOR", "DEX").Should().HaveCount(1);
        }

		[Fact]
		public void AddNotAndDetectUnknownVariable()
		{
			_statService.Add("DEX");
			_statService.Add("FOR", "DEX.no").Should().HaveCount(1);
		}

		[Fact]
		public void AddNotAndDetectUnknownStatVariable()
		{
			_statService.Add("FOR", "DEX.no").Should().HaveCount(1);
		}

		[Fact]
		public void AddNotAndDetectInvalidId()
		{
			_statService.Add("1FOR", "DEX").Should().HaveCount(1);
		}

		[Fact]
		public void AddNotAndDetectDuplicateId()
		{
			_statService.Add("FOR");
			_statService.Add("FOR").Should().HaveCount(1);
		}

        [Fact]
        public void AddNotAndDetectFirstLevelCircularDependency()
        {
            _statService.Add("FOR", "FOR").Should().HaveCount(1);
        }

        [Fact]
        public void AndNotAndDetectDeepCircularDependency()
		{
			_statService.Add("A").Should().BeEmpty();
			_statService.Add("B", "3").Should().BeEmpty();
            _statService.Add("E", "B").Should().BeEmpty();
            _statService.Add("C", "E").Should().BeEmpty();
            _statService.Add("F", "A").Should().BeEmpty();
            _statService.Add("D", "F").Should().BeEmpty();

			// A->D->F->A
			// A->F->A
            _statService.Update("A", "B + C + D + $ABS $ABS F").Should().HaveCount(2);
        }

        #endregion

#region Update

		[Fact]
		public void Update()
		{
			_statService.Add("FOR");

			_statService.Update("FOR", "418").Should().BeEmpty();

			_statService.Get("FOR").ToString().Should().Be("[0, 418]");
		}

		[Fact]
		public void UpdateStat()
		{
			_statService.Add("FOR");
			_statService.Add("DEX");

			_statService.Update("FOR", "DEX").Should().BeEmpty();

			_statService.Get("FOR").ToString().Should().Be("[0, DEX]");
		}

		[Fact]
		public void UpdateMixed()
		{
			const string expression = "B - C * D / E + 1 - 2 * 3 / 4";

			_statService.Add("A");
			_statService.Add("B");
			_statService.Add("C");
			_statService.Add("D");
			_statService.Add("E");

			_statService.Update("A", expression).Should().BeEmpty();

			_statService.Get("A").ToString().Should().Be($"[0, {expression}]");
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
		public void ResolveEmpty()
		{
			_statService.Add("A");

			_statService.GetValue("A").Should().Be(0);
		}

		[Fact]
		public void ResolveNumber()
		{
			_statService.Add("A", "2");

			_statService.GetValue("A").Should().Be(2);
		}

		[Fact]
		public void ResolveStat()
		{
			_statService.Add("A", "2");
			_statService.Add("B", "A");

			_statService.GetValue("B").Should().Be(2);
		}

		[Fact]
		public void ResolveVariable()
		{
			_statService.Add("A", "1");

			_statService.GetValue("A").Should().Be(1);
		}

		[Fact]
		public void ResolveFunction()
		{
			_statService.Add("A", "$MAX{0, 1}");

			_statService.GetValue("A").Should().Be(1);
		}

		//TODo ,ove
		[Fact]
		public void ResolveSimpleIfYes()
		{
			_statService.Add("A", "$IF {1 = 1, 17}");

			_statService.GetValue("A").Should().Be(17);
		}

		//TODo ,ove
		[Fact]
		public void ResolveSimpleIfNo()
		{
			_statService.Add("A", "$IF {1 = 0, 17}");

			_statService.GetValue("A").Should().Be(0);
		}

		[Fact]
		public void ResolveSimpleIfElseIf()
		{
			_statService.Add("A", "$IF {1 = 0, -22, 1 = 1, 17}");

			_statService.GetValue("A").Should().Be(17);
		}

		[Fact]
		public void ResolveSimpleIfElseIfElse()
		{
			_statService.Add("A", "$IF {1 = 0, -22, 1 = 0, -40, 17}");

			_statService.GetValue("A").Should().Be(17);
		}

		[Fact]
		public void ResolveDie()
		{
			_statService.Add("A", "$D 100").Should().BeEmpty();

			_statService.GetValue("A").Should().BeInRange(1, 100);
		}

		[Fact]
		public void ResolveDice()
		{
			_statService.Add("A", "$D{ 100, 2 }").Should().BeEmpty();

			_statService.GetValue("A").Should().BeInRange(100, 200);
		}

		[Fact]
		public void ResolveMixed()
		{
			_statService.Add("A", "5");
			_statService.Add("B", "2");
			_statService.Add("C", "A * B + A * B - A * 2 / 1 + $MIN {$ZERO{}, 1}");

			_statService.GetValue("C").Should().Be(10);
		}

		[Fact]
		public void ResolveSuperMixed()
		{
			_statService.Add("A", "5");
			_statService.Add("B", "2");
			_statService.Add("C", "A & B | 0 ^ 1 >= 0");

			_statService.GetValue("C").Should().Be(0);
		}

#endregion
	}
}