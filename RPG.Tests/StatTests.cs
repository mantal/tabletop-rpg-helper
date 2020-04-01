using FluentAssertions;
using RPG.Engine.Ids;
using RPG.Engine.Parser;
using RPG.Engine.Services;
using Xunit;

namespace RPG.Tests
{
	public class StatTests
	{
		private static readonly Parser _parser = new Parser();
		private readonly ParsingContext _context = new ParsingContext
		{
			StatService = new StatService(new FunctionService()),
		};

		[Fact]
		public void ConvertToString()
		{
			_context.StatId = new StatId("FOR");
			_parser.Parse(out var stat, _context, "FOR").Should().BeEmpty();

			stat.ToString().Should().Be("0");
		}

		[Fact]
		public void ConvertToStringWithNumber()
		{
			_context.StatId = new StatId("FOR");
			_parser.Parse(out var stat, _context, "FOR", "10").Should().BeEmpty();

			stat.ToString().Should().Be("10");
		}

		[Fact]
		public void ConvertToStringWithStat()
		{
			_context.StatService.Add("FOR");
			_context.StatId = new StatId("ATT");
			_parser.Parse(out var stat, _context, "ATT", "FOR").Should().BeEmpty();

			stat.ToString().Should().Be("FOR");
		}

		[Fact]
		public void ConvertToStringWithVariable()
		{
			_context.StatId = new StatId("ATT");
			_parser.Parse(out var stat, _context, "ATT", ":base").Should().BeEmpty();

			stat.ToString().Should().Be("ATT:base");
		}
		[Fact]
		public void ConvertToStringWithMixed()
		{
			_context.StatService.Add("POW");
			_context.StatId = new StatId("ATT");
			_parser.Parse(out var stat, _context, "ATT", ":base + 20 + POW * 2").Should().BeEmpty();

			stat.ToString().Should().Be("ATT:base + 20 + POW * 2");
		}
	}
}