using FluentAssertions;
using RPG.Engine;
using Xunit;

namespace RPG.Tests
{
	public class StatTests
	{
		[Fact]
		public void ConvertToString()
		{
			Stat.FromString(out var stat, "FOR").Should().BeEmpty();

			stat.ToString().Should().Be("");
		}

		[Fact]
		public void ConvertToStringWithStaticModifier()
		{
			Stat.FromString(out var stat, "FOR", "10").Should().BeEmpty();

			stat.ToString().Should().Be("10");
		}

		[Fact]
		public void ConvertToStringWithStatModifier()
		{
			Stat.FromString(out var stat, "ATT", "FOR").Should().BeEmpty();

			stat.ToString().Should().Be("FOR");
		}

		[Fact]
		public void ConvertToStringWithVariableModifier()
		{
			Stat.FromString(out var stat, "ATT", ":Base").Should().BeEmpty();

			stat.ToString().Should().Be("ATT:Base");
		}
		[Fact]
		public void ConvertToStringWithModifiers()
		{
			Stat.FromString(out var stat, "ATT", ":Base + 20 + POW * 2").Should().BeEmpty();

			stat.ToString().Should().Be("ATT:Base + 20 + POW * 2");
		}
	}
}