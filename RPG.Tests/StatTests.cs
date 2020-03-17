using FluentAssertions;
using RPG.Services;
using Xunit;

namespace RPG.Tests
{
	public class StatTests
	{
		[Fact]
		public void ConvertToString()
		{
			var stat = new Stat("POW", 10);

			stat.ToString().Should().Be("10");
		}

		[Fact]
		public void ConvertToStringWithModifier()
		{
			var stat = new Stat("POW")
			{
				Base = 10,
				Modifiers = new []
				{
					new StatModifier(ModifierType.Add, "FOR"), 
				},
			};

			stat.ToString().Should().Be("10 + FOR");
		}

		[Fact]
		public void ConvertToStringWithModifiers()
		{
			var stat = new Stat("POW")
			{
				Base = 10,
				Modifiers = new Modifier[]
				{
					new StatModifier(ModifierType.Add, "FOR"),
					new StatModifier(ModifierType.Add, "DEX"),
					new StaticModifier(ModifierType.Mult, 2), 
				},
			};

			stat.ToString().Should().Be("10 + FOR + DEX * 2");
		}
	}
}