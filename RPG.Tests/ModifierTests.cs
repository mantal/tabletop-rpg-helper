using FluentAssertions;
using RPG.Services;
using Xunit;

namespace RPG.Tests
{
    public class ModifierTests
    {
		[Fact]
		public void ConvertStatModifierToString()
		{
			var modifier = new StatModifier(ModifierType.Add, "FOR");

			modifier.ToString().Should().Be("+ FOR");
		}

		[Fact]
		public void ConvertStaticModifierToString()
		{
			var modifier = new StaticModifier(ModifierType.Add, 2);

			modifier.ToString().Should().Be("+ 2");
		}
	}
}
