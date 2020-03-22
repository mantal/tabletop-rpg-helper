using FluentAssertions;
using RPG.Engine.Ids;
using RPG.Engine.Modifiers;
using Xunit;

namespace RPG.Tests
{
    public class ModifierTests
    {
		[Fact]
		public void ConvertStatModifierToString()
		{
			var modifier = new StatModifier(ModifierType.Add, (StatId) "FOR");

			modifier.ToString().Should().Be("+ FOR");
		}

		[Fact]
		public void ConvertVariableModifierToString()
		{
			var modifier = new VariableModifier(ModifierType.Add, (VariableId) "FOR:Base");

			modifier.ToString().Should().Be("+ FOR:Base");
		}

		[Fact]
		public void ConvertStaticModifierToString()
		{
			var modifier = new StaticModifier(ModifierType.Add, 2);

			modifier.ToString().Should().Be("+ 2");
		}
	}
}
