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
			_book.PopulateFromFile(@"{""FOR"": ""2""}").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
		}

		[Fact]
		public void ImportStats()
		{
			_book.PopulateFromFile(@"{""FOR"": ""2"", ""DEX"": ""3""}").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
			_statService.GetValue("DEX").Should().Be(3);
		}

		[Fact]
		public void ImportMultiExpressionStat()
		{
			_book.PopulateFromFile(@"{""FOR"": { ""expr0"": ""2"", ""expr1"": "":value + 2"" } }").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
		}

		[Fact]
		public void ImportMultiExpressionStats()
		{
			_book.PopulateFromFile(@"{""FOR"": { ""expr0"": ""2"", ""expr1"": "":value + 2"" }, ""DEX"": { ""expr0"": ""2"", ""expr1"": ""FOR + :value"" } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
			_statService.GetValue("DEX").Should().Be(6);
		}

		[Fact]
		public void ImportMultiExpressionWithPositionStat()
		{
			_book.PopulateFromFile(@"{""FOR"": { ""expr0"": ""17"", ""expr1"": { ""Position"": -2, ""Expression"": "":value + 2"" } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(17);
		}
	}
}