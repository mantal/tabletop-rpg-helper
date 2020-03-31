using FluentAssertions;
using RPG.Engine.Parser;
using RPG.Engine.Services;
using Xunit;

namespace RPG.Tests
{
    public class ParserTests
    {
		private readonly Parser _parser = new Parser();
		private readonly ParsingContext _parsingContext;

		public ParserTests()
		{
			var functionService = new FunctionService();
			_parsingContext = new ParsingContext
			{
				FunctionService = functionService,
				StatService = new StatService(functionService),
			};
		}

		[Fact]
		public void ParseFunction()
		{
			_parser.Parse(out var expression, "$MAX{0, 1}", _parsingContext).Should().BeEmpty();

			expression.ToString().Should().Be("$MAX{0, 1}");
		}

		[Fact]
		public void ParseNestedFunction()
		{
			_parser.Parse(out var expression, "$MAX{0, $MAX{0,1}}", _parsingContext).Should().BeEmpty();

			expression.ToString().Should().Be("$MAX{0, $MAX{0, 1}}");
		}

		[Fact]
		public void ParseZeroParameterFunction()
		{
			_parser.Parse(out var expression, "$ZERO{}", _parsingContext).Should().BeEmpty();

			expression.ToString().Should().Be("$ZERO");
		}

		[Fact]
		public void ParseZeroParameterFunctionWithShorthand()
		{
			_parser.Parse(out var expression, "$ZERO", _parsingContext).Should().BeEmpty();

			expression.ToString().Should().Be("$ZERO");
		}

		[Fact]
		public void ParseOneParameterFunctionWithShorthand()
		{
			_parser.Parse(out var expression, "$ABS 1", _parsingContext).Should().BeEmpty();

			expression.ToString().Should().Be("$ABS 1");
		}
	}
}
