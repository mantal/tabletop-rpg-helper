﻿using System;
using FluentAssertions;
using RPG.Engine.Ids;
using RPG.Engine.Parser;
using RPG.Engine.Services;
using Xunit;

namespace RPG.Tests
{
    public class NodeTests
	{
		private readonly ParsingContext _context;

		public NodeTests()
		{
			var functionService = new FunctionService(new Random());
			_context = new ParsingContext(new StatService(functionService), functionService);
		}

		[Fact]
		public void ParseNumber()
		{
			var node = Node.FromString("3.14", _context);

			node.Should().BeOfType<NumberNode>();
			((NumberNode) node).GetValue().Should().Be(3.14);
		}

		[Fact]
		public void ParseStat()
		{
			var node = Node.FromString("F_O-R", _context);

			node.Should().BeOfType<StatNode>();
		}

		[Fact]
		public void ParseVariable()
		{
			var node = Node.FromString("F_O-R.b_as-e", _context);

			node.Should().BeOfType<VariableNode>();
		}

		[Fact]
		public void ParseShorthandVariable()
		{
			_context.StatId = new StatId("F_O-R");
			var node = Node.FromString(".b_as-e", _context);

			node.Should().BeOfType<VariableNode>();
		}

		[Fact]
		public void DetectInvalidVariable()
		{
			_context.StatId = new StatId("F_O-R");
			var node = Node.FromString(".b_as:-e", _context);

			node.Should().BeOfType<InvalidNode>();
		}

		[Fact]
		public void ParseFunction()
		{
			var node = Node.FromString("$F-UN_C", _context);

			node.Should().BeOfType<FunctionNode>();
		}

		[Fact]
		public void DetectInvalidFunction()
		{
			var node = Node.FromString("$", _context);

			node.Should().BeOfType<InvalidNode>();
		}

		[Fact]
		public void DetectInvalidFunction2()
		{
			var node = Node.FromString("$a.", _context);

			node.Should().BeOfType<InvalidNode>();
		}

		[Fact]
		public void ParseAddition()
		{
			var node = Node.FromString("+", _context);

			node.Should().BeOfType<AdditionOperatorNode>();
		}

		[Fact]
		public void ParseSubtractions()
		{
			var node = Node.FromString("-", _context);

			node.Should().BeOfType<AdditionOperatorNode>();
		}

		[Fact]
		public void ParseMultiplier()
		{
			var node = Node.FromString("*", _context);

			node.Should().BeOfType<MultiplierOperatorNode>();
		}

		[Fact]
		public void ParseDivide()
		{
			var node = Node.FromString("/", _context);

			node.Should().BeOfType<MultiplierOperatorNode>();
		}

		[Fact]
		public void ParseModulo()
		{
			var node = Node.FromString("%", _context);

			node.Should().BeOfType<MultiplierOperatorNode>();
		}

		[Fact]
		public void ParseInvalid()
		{
			var node = Node.FromString("bl--||", _context);

			node.Should().BeOfType<InvalidNode>();
		}
	}
}
