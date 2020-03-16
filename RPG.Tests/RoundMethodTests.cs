using FluentAssertions;
using RPG.Services;
using Xunit;

namespace RPG.Tests
{
    public class RoundMethodTests
    {
		[Fact]
		public void NotRound()
		{
			RoundingMethod.None.Convert(3.14).Should().Be(3.14);
		}

		[Fact]
		public void Floor()
		{
			RoundingMethod.Floor.Convert(3.14).Should().Be(3);
		}

		[Fact]
		public void Ceiling()
		{
			RoundingMethod.Ceiling.Convert(3.14).Should().Be(4);
		}
	}
}
