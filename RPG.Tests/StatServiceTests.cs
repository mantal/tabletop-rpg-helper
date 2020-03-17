using FluentAssertions;
using RPG.Services;
using Xunit;

namespace RPG.Tests
{
    public class AddStatFromStringTests
	{
		private readonly StatService _statService = new StatService();

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
		public void DetectUnknownId()
		{
			_statService.Add("FOR", 0, "DEX").Should().HaveCount(1);
		}

		[Fact]
		public void DetectFirstLevelCircularDependency()
		{
			_statService.Add("FOR", 0, "FOR").Should().HaveCount(1);
		}

		[Fact]
		public void DetectDeepCircularDependency()
		{
			_statService.Add("B", 0, "3");
			_statService.Add("E", 0, "B");
			_statService.Add("C", 0, "E");
			_statService.Add("F", 0, "A");
			_statService.Add("D", 0, "F");

			_statService.Add("A", 0, "B + C + D").Should().HaveCount(1);
		}
	}
}
