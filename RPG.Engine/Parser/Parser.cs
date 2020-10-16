using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class Parser
	{
		private bool IsSingleToken(char c)
			=> "+-*/%(){},&|^".Contains(c);

		/// <summary>
		/// Special token are only made of special characters
		/// </summary>
		private bool IsSpecialToken(char c)
			=> "<>=~".Contains(c);

		private bool IsTokenStart(char c)
			=> c == '$'
			   || char.IsWhiteSpace(c);

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
			
			if (!errors.Any())
				stat = new Stat(new StatId(id), expression!);

			return errors;
		}

		public IEnumerable<string> Parse(out Expression? expression, string s, ParsingContext context)
		{
			var errors = new List<string>();
			expression = null;

			var nodes = new LinkedList<Node>();
			var last = 0;


			for (var i = 0; i <= s.Length; i++)
			{
				if (i == s.Length
					|| IsTokenStart(s[i]))
                {
                    var token = s[last..i].Trim();
                    if (token.Length > 0)
                        nodes.AddLast(Node.FromString(token, context));
					last = i;
				}
				else if (IsSingleToken(s[i]))
                {
					var token = s[last..i].Trim();
					if (token.Length > 0)
						nodes.AddLast(Node.FromString(token, context));
					nodes.AddLast(Node.FromString(s[i].ToString(), context));
					last = i + 1;
				}
				else if (IsSpecialToken(s[i]))
				{
					if (i > 0 && IsSpecialToken(s[i - 1]))
					{ }
					else
					{
						var token = s[last..i].Trim();
						if (token.Length > 0)
							nodes.AddLast(Node.FromString(token, context));
						last = i;
					}
				}
				else if (i > 0 && IsSpecialToken(s[i - 1]))
				{
					var token = s[last..i].Trim();
					if (token.Length > 0)
						nodes.AddLast(Node.FromString(token, context));
					last = i;
				}
				else
				{ }
			}
			
			if (nodes.First == null)
				return new[] { $"expected empty expression" }; //TODO use context to say which stat or function this expression belongs

			for (var node = nodes.First; node != null; node = node?.Next)
				node = node.Value.OnBeforeValidation(node);

			for (var node = nodes.First; node != null; node = node.Next) 
				errors = errors.Concat(node.Value.IsValid(node)).ToList();

			if (errors.Any())
				return errors;

			for (var node = nodes.First; node != null; node = node?.Next)
				node = node.Value.OnAfterValidation(node);

			expression = new Expression(nodes);

			return errors;
		}
	}

	public class ParsingContext
	{
		public StatId? StatId { get; set; }
		public StatService StatService { get; }
		public FunctionService FunctionService { get; }

		public ParsingContext(StatService statService, FunctionService functionService)
		{
			StatService = statService;
			FunctionService = functionService;
		}
	}
}
