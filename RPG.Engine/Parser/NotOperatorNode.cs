using System.Collections.Generic;
using RPG.Engine.Utils;

namespace RPG.Engine.Parser
{
	public class NotOperatorNode : UnaryOperatorNode
	{
		public NotOperatorNode(string text, NodeType type) 
			: base(text, type)
		{ }

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			var result = !Operand!.Resolve().ToBool();

			return ReplaceSelfWithResult(node, result.ToDouble());
		}

		public override Node Clone() => new NotOperatorNode(Text, Type);
	}
}