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
			// unary plus is always a no op
			if (Type == NodeType.UnaryPlusOperator)
			{
				var next = node.Next;
				node.List.Remove(node);

				return next;
			}

			var b = ((ValueNode)node.Next.Value).GetValue();

			var value = new NumberNode(-b);

			var result = node.List.AddAfter(node.Next, value);

			result.List.Remove(result.Previous.Previous);
			result.List.Remove(result.Previous);

			return result;
		}

		public override Node Clone() => new UnaryAdditionOperatorNode(Text, Type);
	}
}