using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using RPG.Engine;
using RPG.Engine.Ids;
using RPG.Engine.Parser;
using RPG.Engine.Services;
using Xunit;

namespace RPG.Tests
{
	public class StatTests
	{
		private static readonly Parser _parser = new ();
		private readonly ParsingContext _context;

		public StatTests()
		{
			var functionService = new FunctionService(new Random());
			_context = new ParsingContext(new StatService(functionService), functionService);
		}

		[Fact]
		public void ConvertToString()
		{
			_context.StatId = new StatId("FOR");
			_parser.Parse(out var stat, _context, "FOR").Should().BeEmpty();

			stat!.ToString().Should().Be("[0, 0]");
		}

		[Fact]
		public void ConvertToStringWithNumber()
		{
			_context.StatId = new StatId("FOR");
			_parser.Parse(out var stat, _context, "FOR", "10").Should().BeEmpty();

			stat!.ToString().Should().Be("[0, 10]");
		}

		[Fact]
		public void ConvertToStringWithStat()
		{
			_context.StatService.Add("FOR");
			_context.StatId = new StatId("ATT");
			_parser.Parse(out var stat, _context, "ATT", "FOR").Should().BeEmpty();

			stat!.ToString().Should().Be("[0, FOR]");
		}

		[Fact]
		public void ConvertToStringWithVariable()
		{
			_context.StatId = new StatId("ATT");
			_parser.Parse(out var stat, _context, "ATT", ".base").Should().BeEmpty();

			stat!.ToString().Should().Be("[0, ATT.base]");
		}
		[Fact]
		public void ConvertToStringWithMixed()
		{
			_context.StatService.Add("POW");
			_context.StatId = new StatId("ATT");
			_parser.Parse(out var stat, _context, "ATT", ".base + 20 + POW * 2").Should().BeEmpty();

			stat!.ToString().Should().Be("[0, ATT.base + 20 + POW * 2]");
		}

		[Fact]
		public void ConvertToStringWithMultipleExpressions()
		{
			_context.StatId = new StatId("FOR");
			var expressions = new Expression[2];
			_parser.Parse(out expressions[0]!, "2", _context);
			_parser.Parse(out expressions[1]!, ".value + 2", _context);

			var namedExpressions = new NamedExpression[]
			{
				new ("0", expressions[0].Nodes), 
				new ("1", expressions[1].Nodes),
			};
			new Stat(new StatId("FOR"), namedExpressions.ToList())
				.ToString()
				.Should()
				.Be("[0, 2][1, FOR.value + 2]");
		}

		[Fact]
		public void AddExpression()
		{
			_context.StatId = new StatId("FOR");
			_parser.Parse(out var stat, _context, "FOR", "2").Should().BeEmpty();

			_parser.Parse(out var expression, "FOR.var + 2", _context);
			stat!.AddExpression(expression!, "1").Should().BeEmpty();

			stat!.ToString().Should().Be("[0, 2][1, FOR.var + 2]");
			stat.TryGetVariable(new VariableId("FOR.var")).Should().Be(0);
		}

		[Fact]
		public void AddExpressionWithExplicitPosition()
		{
			_context.StatId = new StatId("FOR");
			_parser.Parse(out var stat, _context, "FOR", "2").Should().BeEmpty();

			_parser.Parse(out var expression, "FOR.value + 2", _context);
			stat!.AddExpression(expression!, "2").Should().BeEmpty();
			_parser.Parse(out expression, "FOR.value + 2", _context);
			stat!.AddExpression(expression!, "1", 1).Should().BeEmpty();

			stat!.ToString().Should().Be("[0, 2][1, FOR.value + 2][2, FOR.value + 2]");
		}

		[Fact]
		public void UpdateExpression()
		{
			_context.StatId = new StatId("FOR");
			_parser.Parse(out var stat, _context, "FOR", ".old").Should().BeEmpty();

			_parser.Parse(out var expression, ".new", _context);
			stat!.UpdateExpression(expression!, "0");

			stat!.ToString().Should().Be("[0, FOR.new]");
			//TODO?
			//stat.TryGetVariable(new VariableId("FOR.old")).Should().Be(null);
			stat.TryGetVariable(new VariableId("FOR.new")).Should().Be(0);
		}

		[Fact]
		public void RemoveExpression()
		{
			_context.StatId = new StatId("FOR");
			_parser.Parse(out var stat, _context, "FOR", ".var + 2").Should().BeEmpty();

			stat!.RemoveExpression("0");

			stat!.Expressions.Should().BeEmpty();
			//TODO?
			//stat!.TryGetVariable(new VariableId("FOR.var")).Should().BeNull();
		}

		[Fact]
		public void ResolveExpressionsWithImplicitChaining()
		{
			_context.StatId = new StatId("FOR");
			_parser.Parse(out var stat, _context, "FOR", "2").Should().BeEmpty();

			_parser.Parse(out var expression, "2", _context);
			stat!.AddExpression(expression!, "expr2").Should().BeEmpty();

			stat!.Resolve().Should().Be(4);
		}

		[Fact]
		public void HandleVariableWithCircularDependency()
		{
			var statService = new StatService(new FunctionService(new Random()));

			statService.Add("FOR", ".var").Should().BeEmpty();
			var stat = statService.Get("FOR");

			stat.AddOrUpdateVariable(new VariableId("FOR.var"),
									 new Expression(new LinkedList<Node>(new[] { new StatNode(statService, "FOR") })))
				.Should().HaveCount(1);
		}
	}
}