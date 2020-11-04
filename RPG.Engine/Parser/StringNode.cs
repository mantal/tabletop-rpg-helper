using System;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Engine.Parser
{
    public class StringNode : Node
    {
		public StringNode(string text) 
			: base(text, NodeType.String, -1)
		{ }

		public override bool IsValidLeftOperand() => false;

		public override bool IsValidRightOperand() => false;

		public override IEnumerable<string> IsValid(LinkedListNode<Node> node)
		{
			if (Text.Length == 2)
				return new[] { "empty string" };
			return Enumerable.Empty<string>();
		}

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
			=> throw new InvalidOperationException("Can not convert string to number");

		public string Value => Text[1..^1];
		public override string ToString() => Text;

		public override Node Clone() => new StringNode(Text);
	}
}