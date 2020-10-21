using System;
using System.Collections.Generic;

namespace RPG.Engine.Parser
{
	public class MultiplierOperatorNode : BinaryOperatorNode
	{
		public MultiplierOperatorNode(string text, NodeType type) 
			: base(text, type, 6)
		{ }
		
		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			var a = ((ValueNode)node.Previous!.Value).GetValue();
			var b = ((ValueNode)node.Next!.Value).GetValue();

			var result = Type switch
						 {
							 NodeType.MultiplierOperator => a * b,
							 NodeType.DivideOperator     => a / b,
							 NodeType.ModuloOperator     => a % b,
							 _                           => throw new InvalidOperationException(),
						 };

			return ReplaceSelfWithResult(node, result);
		}

		public override Node Clone() => new MultiplierOperatorNode(Text, Type);
	}
}