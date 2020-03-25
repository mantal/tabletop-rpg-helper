using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class Parser
	{
		private const string _breakChars = "+-*/$(){}";

		public IEnumerable<string> Parse(out Stat? stat, ParsingContext context, string id, string? rawExpression = "0")
		{
			rawExpression ??= "0";
			IEnumerable<string> errors = new List<string>();

			stat = null;
			if (!id.IsValidStatId())
			{
				errors = errors.Append($"Invalid stat id: {id}");
				return errors;
			}
			errors = errors.Concat(Parse(out var expression, rawExpression, context));
			
			stat = new Stat(new StatId(id), expression);

			return errors;
		}

		public IEnumerable<string> Parse(out LinkedList<Node> tokens, string s, ParsingContext context)
		{
			var errors = new List<string>();

			tokens = new LinkedList<Node>();
			var last = 0;
			for (var i = 0; i <= s.Length; i++)
			{
				if (i == s.Length
					|| char.IsWhiteSpace(s[i])
					|| _breakChars.Contains(s[i]))
				{
					var token = s[last..i].Trim();
					if (token.Length > 0)
						tokens.AddLast(Node.FromString(token, context));
					last = i;
				}
			}
			
			if (tokens.First == null)
				return new[] { $"Empty expression: '{s}'" };

			for (var t = tokens.First; t != null; t = t.Next) 
				errors = errors.Concat(t.Value.IsValid(t, context)).ToList();

			//if (!errors.Any())
			//{
			//	foreach (var token in tokens)
			//	{
			//		if (token is VariableNode v)
			//			context.StatService.AddOrUpdate(v.Id);
			//	}
			//}

			return errors;
		}
	}

	public class ParsingContext
	{
		public StatId StatId { get; set; }
		public StatService StatService { get; set; }
	}
}
