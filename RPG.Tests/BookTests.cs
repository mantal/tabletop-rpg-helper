﻿using FluentAssertions;
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
		public void ImportStatWithVariable()
		{
			_book.Populate(@"{""FOR"": { "":var"": 33, ""base"": ""2"" } }").Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(2);
			_statService.Get("FOR").TryGetVariable(new VariableId("FOR:var")).Should().Be(33);
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
			_book.Populate(@"{""FOR"": { ""should_run_last"": ""1"", ""should_run_first"": { ""Position"": -2, ""Expression"": "":value / 2"" } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(1);
		}

		[Fact]
		public void ImportDefault()
		{
			_book.Populate(@"{""$default"": ""2"", ""FOR"": "":value + 2""}")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
		}

		[Fact]
		public void ImportDefaultWithVariable()
		{
			_book.Populate(@"{""$default"": { "":var"": 2 }, ""FOR"": "":var + 2""}")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
		}

		[Fact]
		public void ImportMultiExpressionDefault()
		{
			_book.Populate(@"{""$default"": { ""expr0"": "":base + 1"", ""expr1"": "":value + 1"" }, ""FOR"": "":value + 2""}")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
		}

		[Fact]
		public void OverrideDefaultVariable()
		{
			_book.Populate(@"{""$default"": { "":var"": 2, ""expr"": "":var"" }, ""FOR"": { "":var"": 4 } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(4);
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
				 .Should().BeEmpty();
		}

		[Fact]
		public void HandleImplicitRootSection()
		{
			_book.Populate(@"test: {}")
				 .Should().BeEmpty();
		}

		[Fact]
		public void HandleDuplicateStats()
		{
			_book.Populate(@"{""FOR"": ""2"", ""FOR"": ""3""}")
				 .Should().HaveCount(1);
		}

		//TODO
		[Fact(Skip = "not_impl")]
		public void HandleDuplicateExpressionInSameBlock()
		{
			_book.Populate(@"{ ""FOR"": {""expr"": ""2"", ""expr"": ""3""} }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleEmptyStatBody()
		{
			_book.Populate(@"{ ""FOR"": """" }")
				 .Should().HaveCount(0);
		}

		[Fact]
		public void HandleInvalidStatBody()
		{
			_book.Populate(@"{ ""FOR"": [] }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleInvalidShortStatId()
		{
			_book.Populate(@"{ ""F°R"": 1 }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleInvalidStatId()
		{
			_book.Populate(@"{ ""F°R"": {} }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleInteger()
		{
			_book.Populate(@"{ ""FOR"": 1 }")
				 .Should().HaveCount(0);
			_statService.GetValue("FOR").Should().Be(1);
		}

		[Fact]
		public void HandleFloat()
		{
			_book.Populate(@"{ ""FOR"": .5 }")
				 .Should().HaveCount(0);
			_statService.GetValue("FOR").Should().Be(.5);
		}

		[Fact]
		public void IgnoreCaseInExpressionProperties()
		{
			_book.Populate(@"{""FOR"": { ""should_run_last"": ""1"", ""should_run_first"": { ""PoSitIoN"": -2, ""ExPrEsSiOn"": "":value / 2"" } }")
				 .Should().BeEmpty();
			_statService.GetValue("FOR").Should().Be(1);
		}

		[Fact(Skip = "needs continue after error")] 
		public void HandleUnnamedSection()
		{
			_book.Populate(@"{ ""FOR"": 0 }{ }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleExpressionObjectWithMissingProperty()
		{
			_book.Populate(@"{ ""FOR"": { ""expr"": { ""position"": 1 } }")
				 .Should().HaveCount(1);
		}

		[Fact]
		public void HandleExpressionObjectWithExtraProperties()
		{
			_book.Populate(@"{ ""FOR"": { ""expr"": { ""position"": 1 } }")
				 .Should().HaveCount(1);
		}

		#endregion
	}
}