using System.Collections.Generic;
using RPG.Engine.Services;
using RPG.Engine.Utils;

namespace RPG.Engine.Parser
{
	public class AndOperatorNode : BinaryOperatorNode
	{
		public AndOperatorNode(string text, NodeType type)
			: base(text, type, 2)
		{ }

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			var a = ((ValueNode)node.Previous.Value).GetValue().ToBool();
			var b = ((ValueNode)node.Next.Value).GetValue().ToBool();

			return ReplaceSelfWithResult(node, (a && b).ToDouble());
		}

		public override Node Clone() => new RelationalOperatorNode(Text, Type);
	}
}