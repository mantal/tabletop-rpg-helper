using FluentAssertions;
using RPG.Engine;
using RPG.Engine.Services;
using Xunit;

namespace RPG.Tests
{
    public class BookTests
    {
		private readonly FunctionService _functionService;
		private readonly StatService _statService;
		private readonly Book _book;

		public BookTests()
		{
			_functionService = new FunctionService();
			_statService = new StatService(_functionService);
			_book = new Book(_statService, _functionService);
		}

		//TODO test errors

		[Fact]
		public void ImportStat()
		{
			_book.Populate(@"{""FOR"": ""2""}").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
		}

		[Fact]
		public void ImportStats()
		{
			_book.Populate(@"{""FOR"": ""2"", ""DEX"": ""3""}").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
			_statService.GetValue("DEX").Should().Be(3);
		}

		[Fact]
		public void ImportMultiExpressionStat()
		{
			_book.Populate(@"{""FOR"": { ""expr0"": ""2"", ""expr1"": "":value + 2"" } }").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
		}

		[Fact]
		public void ImportMultiExpressionStats()
		{
			_book.Populate(@"{""FOR"": { ""expr0"": ""2"", ""expr1"": "":value + 2"" }, ""DEX"": { ""expr0"": ""2"", ""expr1"": ""FOR + :value"" } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
			_statService.GetValue("DEX").Should().Be(6);
		}

		[Fact]
		public void ImportMultiExpressionWithPositionStat()
		{
			_book.Populate(@"{""FOR"": { ""expr0"": ""17"", ""expr1"": { ""Position"": -2, ""Expression"": "":value + 2"" } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(17);
		}

		[Fact]
		public void ImportStatWithDefault()
		{
			_book.Populate(@"{""$default"": ""2"", ""FOR"": "":value + 2""}")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
		}

		[Fact]
		public void ImportStatWithComplexDefault()
		{
			_book.Populate(@"{""$default"": { ""expr0"": "":base + 2"", ""expr1"": "":value - 2"" }, ""FOR"": "":value + 2""}")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
		}

		[Fact]
		public void ImportSection()
		{
			_book.Populate(@"{ ""#section"": { ""FOR"": ""2"" } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
			_book.Sections.ContainsKey("#section").Should().BeTrue("it should have imported the section");
			_book.Sections["#section"].Stats.Should().HaveCount(1);
		}

		[Fact]
		public void DefaultShouldCascade()
		{
			_book.Populate(@"{""$default"": ""2"", ""FOR"": "":value + 2""}")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
		}

#region Errors

		[Fact]
		public void HandleEmptyJson()
		{
			_book.Populate(@"")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleEmptyBody()
		{
			_book.Populate(@"{}")
				 .Should().BeEmpty();
		}

		[Fact]
		public void HandleDuplicateStats()
		{
			_book.Populate(@"{""FOR"": ""2"", ""FOR"": ""3""}")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleInvalidSection()
		{
			_book.Populate(@"{ ""section"": {""FOR"": ""2"", ""FOR"": ""3""} }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleInvalidStat()
		{
			_book.Populate(@"{ ""FOR"": [] }")
				 .Should().HaveCount(1);
		}

#endregion
	}
}