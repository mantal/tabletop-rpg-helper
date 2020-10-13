using System;
using System.Collections.Generic;
using RPG.Engine.Utils;

namespace RPG.Engine.Parser
{
	public class RelationalOperatorNode : BinaryOperatorNode
	{
		public RelationalOperatorNode(string text, NodeType type) 
			: base(text, type, 2)
		{ }
		
		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			var a = ((ValueNode)node.Previous!.Value).GetValue();
			var b = ((ValueNode)node.Next!.Value).GetValue();

			var result = Type switch
					{
						NodeType.GreaterThanOperator        => a > b,
						NodeType.GreaterThanOrEqualOperator => a >= b,
						NodeType.EqualOperator              => a.IsCloseTo(b),
						NodeType.NotEqualOperator           => !a.IsCloseTo(b),
						NodeType.LessThanOrEqualOperator    => a <= b,
						NodeType.LessThanOperator           => a < b,
						_                                   => throw new InvalidOperationException(),
					};

			return ReplaceSelfWithResult(node, result ? 1 : 0);
		}

		public override Node Clone() => new RelationalOperatorNode(Text, Type);
	}
}