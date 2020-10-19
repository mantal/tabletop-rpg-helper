using System;
using FluentAssertions;
using RPG.Engine.Ids;
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
			var functionService = new FunctionService(new Random());
			_parsingContext = new ParsingContext(new StatService(functionService), functionService);
		}

		[Fact]
		public void Parse()
		{
			const string input = "$MAX{$ZERO,$ABS .1}++2--3*stat/.var>4>=5=6~=7<=8<9&10|11^12~13";
			const string expected = "$MAX{$ZERO, $ABS 0.1} + +2 - -3 * stat / stat.var > 4 >= 5 = 6 ~= 7 <= 8 < 9 & 10 | 11 ^ 12 ~13";

			_parsingContext.StatService.Add("stat");
			_parsingContext.StatService.Get("stat").AddOrUpdateVariable(new VariableId("stat.var"), 0);
			_parsingContext.StatId = new StatId("stat");

			_parser.Parse(out var expression, input, _parsingContext).Should().BeEmpty();

			expression!.ToString().Should().Be(expected);
		}

		[Fact]
		public void ParseUnary()
		{
			const string e = "+1";

			_parser.Parse(out var expression, e, _parsingContext).Should().BeEmpty();

			expression!.ToString().Should().Be(e);
		}

		[Fact]
		public void ParseGreaterThan()
		{
			_parser.Parse(out var expression, "1>=1", _parsingContext).Should().BeEmpty();

			expression!.ToString().Should().Be("1 >= 1");
		}

		#region Function

		[Fact]
		public void ParseFunction()
		{
			_parser.Parse(out var expression, "$MAX{0, 1}", _parsingContext).Should().BeEmpty();

			expression!.ToString().Should().Be("$MAX{0, 1}");
		}

		[Fact]
		public void ParseNestedFunction()
		{
			_parser.Parse(out var expression, "$MAX{0, $MAX{0,1}}", _parsingContext).Should().BeEmpty();

			expression!.ToString().Should().Be("$MAX{0, $MAX{0, 1}}");
		}

		[Fact]
		public void ParseZeroParameterFunctionWithEmptyBrackets()
		{
			_parser.Parse(out var expression, "$ZERO{}", _parsingContext).Should().BeEmpty();

			expression!.ToString().Should().Be("$ZERO");
		}

		[Fact]
		public void ParseZeroParameterFunctionWithShorthand()
		{
			_parser.Parse(out var expression, "$ZERO", _parsingContext).Should().BeEmpty();

			expression!.ToString().Should().Be("$ZERO");
		}

		[Fact]
		public void ParseZeroParameterFunctionWithShorthandFollowedByUnaryOperator()
		{
			_parser.Parse(out var expression, "$ZERO - 1", _parsingContext).Should().BeEmpty();

			expression!.ToString().Should().Be("$ZERO - 1");
		}

		[Fact]
		public void ParseOneParameterFunctionWithShorthand()
		{
			_parser.Parse(out var expression, "$ABS 1", _parsingContext).Should().BeEmpty();

			expression!.ToString().Should().Be("$ABS 1");
		}

		[Fact]
		public void ParseOneParameterFunctionWithShorthandFollowedByUnaryOperator()
		{
			_parser.Parse(out var expression, "$ABS -1", _parsingContext).Should().BeEmpty();

			expression!.ToString().Should().Be("$ABS - 1");//TODO fix expression with unary operator to string
		}

		[Fact]
		public void ParseMixedFunction()
		{
			_parser.Parse(out var expression, "$MAX{$ABS 1, $ZERO, $ABS{$MIN{0,1}}, $ABS $ABS {--$ZERO}}", _parsingContext).Should().BeEmpty();

			expression!.ToString().Should().Be("$MAX{$ABS 1, $ZERO, $ABS $MIN{0, 1}, $ABS $ABS --$ZERO}");
		}

		[Fact]
		public void ParseWithNotEnoughArguments()
		{
			_parser.Parse(out _, "$ABS", _parsingContext).Should().HaveCount(1);
		}

		[Fact]
		public void ParseWithTooManyArguments()
		{
			_parser.Parse(out _, "$ZERO 1", _parsingContext).Should().HaveCount(1);
		}

		[Fact(Skip = "TODO add a function with batch argument")]
		public void ParseWithWrongBatchArguments()
		{
			_parser.Parse(out _, "$IF{1, 1, 2, 3}", _parsingContext).Should().HaveCount(1);
		}

		[Fact]
		public void ParseFunctionWithUnbalancedLeftBracket()
		{
			_parser.Parse(out _, "$MIN{", _parsingContext).Should().HaveCount(1);
		}

		[Fact]
		public void ParseFunctionWithUnbalancedRightBracket()
		{
			_parser.Parse(out _, "$ABS}", _parsingContext).Should().HaveCount(1);
		}

#endregion

		[Fact]
		public void HandleMissingOperators()
		{
			_parsingContext.StatService.Add("A");
			_parsingContext.StatService.Add("B");
			_parser.Parse(out _, "A A.b $ZERO $ABS 1 $MIN{0,1}", _parsingContext).Should().HaveCount(5);
		}
	}
}