using System;
using FluentAssertions;
using RPG.Engine;
using RPG.Engine.Ids;
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
			_functionService = new FunctionService(new Random());
			_statService = new StatService(_functionService);
			_book = new Book(_statService, _functionService);
		}

		//TODO test errors

		[Fact]
		public void ImportIntStat()
		{
			_book.Populate(@"FOR: 2").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
		}

		[Fact]
		public void ImportFloatStat()
		{
			_book.Populate(@"FOR: .2").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(.2);
		}

		[Fact]
		public void ImportStringStat()
		{
			_book.Populate(@"FOR: 1 + 1").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
		}

		[Fact]
		public void ImportStatWithVariable()
		{
			_book.Populate("FOR { .var: 33\n expr: 2\n }").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
			_statService.Get("FOR").TryGetVariable(new VariableId("FOR.var")).Should().Be(33);
		}

		[Fact]
		public void ImportStats()
		{
			_book.Populate("FOR: 2\n DEX: 3").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
			_statService.GetValue("DEX").Should().Be(3);
		}

		[Fact]
		public void ImportMultiExpressionStat()
		{
			_book.Populate("FOR { expr0: 2\n expr1: .value + 2\n }").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
		}

		[Fact]
		public void ImportMultiExpressionStats()
		{
			_book.Populate("FOR { expr0: 2\n expr1: .value + 2\n } DEX { expr0: 2\n expr1: FOR + .value\n } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
			_statService.GetValue("DEX").Should().Be(6);
		}

		[Fact]
		public void ImportMultiExpressionWithPositionStat()
		{
			_book.Populate("FOR { should_run_last: 1\n should_run_first { Position: -2\n Expression: .value / 2\n } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(1);
		}

		[Fact]
		public void ImportDefault()
		{
			_book.Populate("_default: 2\n FOR: .value + 2\n")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
		}

		[Fact]
		public void ImportDefaultWithVariable()
		{
			_book.Populate("_default { .var: 2\n } FOR: .var + 2\n")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
		}

		[Fact]
		public void ImportMultiExpressionDefault()
		{
			_book.Populate("_default { expr0: .base + 1\n expr1: .value + 1\n } FOR: .value + 2\n}")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
		}

		[Fact]
		public void OverrideDefaultVariable()
		{
			_book.Populate("_default { .var: 2\n expr: .var\n } FOR { .var: 4\n } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
		}

		[Fact]
		public void ImportSection()
		{
			_book.Populate("#section { FOR: 2\n } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
			_book.Sections.ContainsKey("#section").Should().BeTrue("it should have imported the section");
			_book.Sections["#section"].Stats.Should().HaveCount(1);
		}

		[Fact]
		public void DefaultShouldCascade()
		{
			_book.Populate("_default: 2\n #section { FOR: .value + 2\n }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
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
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleDuplicateStats()
		{
			_book.Populate("FOR: 2\n FOR: 3\n")
				 .Should().HaveCount(1);
		}

		//TODO
		[Fact(Skip = "not_impl")]
		public void HandleDuplicateExpressionInSameBlock()
		{
			_book.Populate("FOR { expr: 2\n expr: 3\n }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleInvalidStatBody()
		{
			_book.Populate("FOR: []")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleInvalidShortStatId()
		{
			_book.Populate("F°R: 1")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleInvalidStatId()
		{
			_book.Populate("F°R: { val: 2\n }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void IgnoreCaseInExpressionProperties()
		{
			_book.Populate("FOR { should_run_last: 1\n should_run_first { PoSitIoN: -2\n ExPrEsSiOn\n: .value / 2\n }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(1);
		}

		[Fact(Skip = "needs continue after error")]
		public void HandleUnnamedSection()
		{
			_book.Populate("{ }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleExpressionObjectWithMissingProperty()
		{
			_book.Populate("FOR { expr { position: 1\n } }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleExpressionObjectWithExtraProperties()
		{
			_book.Populate("FOR { expr { expression: 1\n extra: 0\n } }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleUnexpectedObject()
		{
			_book.Populate("FOR: { expr: 1\n }")
				 .Should().HaveCount(1);
		}

#endregion

		[Fact]
		public void HandleUserFunction()
		{
			_book.Populate("$foo: 17\nFOR: $foo")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(17);
		}

		[Fact]
		public void HandleUserFunctionWithInvalidArgument()
		{
			_book.Populate("$foo: $0 + 7")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleUserFunctionWithArgument()
		{
			_book.Populate("$foo: $1 + 7\nFOR: $foo 10")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(17);
		}
	}
}