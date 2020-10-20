using System.Collections.Generic;

namespace RPG.Engine.Parser
{
	public class UnaryAdditionOperatorNode : UnaryOperatorNode
	{
		public UnaryAdditionOperatorNode(string text, NodeType type) 
			: base(text, type)
		{ }

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			var result = Operand!.Resolve();
			result = Type == NodeType.UnaryPlusOperator ? result : -result;

			return ReplaceSelfWithResult(node, result);
		}

		public override Node Clone() => new UnaryAdditionOperatorNode(Text, Type);
	}
}