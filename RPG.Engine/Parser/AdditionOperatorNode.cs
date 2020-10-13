using System.Collections.Generic;

namespace RPG.Engine.Parser
{
	public class AdditionOperatorNode : BinaryOperatorNode
	{
		public AdditionOperatorNode(string text, NodeType type) 
			: base(text, type, 2)
		{ }

		public override LinkedListNode<Node> OnBeforeValidation(LinkedListNode<Node> node)
		{
			var isUnary = node.Previous == null || !node.Previous.Value.IsValidLeftOperand();
			if (!isUnary)
				return node;

			var type = Type == NodeType.PlusOperator ? NodeType.UnaryPlusOperator : NodeType.UnaryMinusOperator;

			var newNode = node.List!.AddAfter(node, new UnaryAdditionOperatorNode(Text, type));
			node.List!.Remove(node);

			return newNode;
		}
		
		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			var a = ((ValueNode)node.Previous!.Value).GetValue();
			var b = ((ValueNode)node.Next!.Value).GetValue();

			var value = new NumberNode(Type == NodeType.PlusOperator ? a + b : a - b);

			var result = node.List!.AddAfter(node.Next!, value);

			result.List!.Remove(result.Previous!.Previous!.Previous!);
			result.List!.Remove(result.Previous!.Previous!);
			result.List!.Remove(result.Previous!);

			return result;
		}
		
		public override Node Clone() => new AdditionOperatorNode(Text, Type);
	}
}