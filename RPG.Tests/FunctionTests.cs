using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using RPG.Engine.Functions;
using RPG.Engine.Ids;
using RPG.Engine.Parser;
using RPG.Engine.Services;
using Xunit;

namespace RPG.Tests
{
    public class FunctionTests
	{
		private readonly FunctionService _functionService;
		private readonly StatService _statService;

		public FunctionTests()
		{
			_functionService = new FunctionService(new Random());
			_statService = new StatService(_functionService);
		}

		[Fact]
		public void HaveAStack()
		{
			AddFunction("$F", "-$1").Should().BeEmpty();
			AddFunction("$G", "$F -$1").Should().BeEmpty();
			_statService.Add("A", "$G 1 + $G 2").Should().BeEmpty();
			_statService.GetValue("A").Should().Be(3);
		}

		[Fact]
		public void HandleStackOverflows()
		{
			AddFunction("$F", "$F");
			_statService.Add("A", "$F").Should().BeEmpty();
			Action a = () => _statService.GetValue("A");
			a.Should().Throw<Exception>();
		}

		private IEnumerable<string> AddFunction(string functionId, string functionBody, ParsingContext? context = null)
		{
			context ??= new ParsingContext(new StatService(_functionService), _functionService);
			context.FunctionId = new FunctionId(functionId);

			var errors = new Parser().Parse(out var expression, functionBody, context);
			if (errors.Any())
				return errors;

			return _functionService.Add(new UserFunction(new FunctionId(functionId), expression!));
		}
	}
}
