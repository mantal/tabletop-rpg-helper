using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class GrammarNode : Node
	{
		public string Text { get; }

		public GrammarNode(StatService statService, string text, NodeType type, int priority)
			: base(statService, type, priority)
		{
			Text = text;
		}

		public override bool IsExpression() => Type == NodeType.LeftParenthesis;

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
			=> Enumerable.Empty<string>();

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
			=> throw new InvalidOperationException();

		public override string ToString() => Text;
	}
}