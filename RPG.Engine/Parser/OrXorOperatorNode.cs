using System;
using System.Collections.Generic;
using RPG.Engine.Utils;

namespace RPG.Engine.Parser
{
	public class OrXorOperatorNode : BinaryOperatorNode
	{
		public OrXorOperatorNode(string text, NodeType type)
			: base(text, type, 2)
		{ }

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			var a = ((ValueNode)node.Previous!.Value).GetValue().ToBool();
			var b = ((ValueNode)node.Next!.Value).GetValue().ToBool();

			var result = Type switch
						 {
							 NodeType.OrOperator  => a || b,
							 NodeType.XorOperator => a ^ b,
							 _                    => throw new InvalidOperationException()
						 };

			return ReplaceSelfWithResult(node, result.ToDouble());
		}

		public override Node Clone() => new RelationalOperatorNode(Text, Type);
	}
}