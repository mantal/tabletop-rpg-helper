using System.Collections.Generic;
using System.Linq;

namespace RPG.Engine.Parser
{
	public abstract class UnaryOperatorNode : Node
	{
		protected UnaryOperatorNode(string text, NodeType type) 
			: base(text, type, 8)
		{ }
		
		public override IEnumerable<string> IsValid(LinkedListNode<Node> node)
		{
			var errors = new List<string>();

			if (node.Next == null)
				return new[] { $"Expected operand after operator '{Text}' but found nothing" };

			var next = node.Next.Value;

			if (!next.IsValidRightOperand())
			{
				errors.Add($"Expected operand after operator '{Text}' but found '{next}'");
				return errors;
			}

			return Enumerable.Empty<string>();
		}

		public override bool IsValidLeftOperand() => false;

		public override bool IsValidRightOperand() => true;

		protected LinkedListNode<Node> ReplaceSelfWithResult(LinkedListNode<Node> node, double result)
		{
			var value = new NumberNode(result);

			var resultNode = node.List!.AddAfter(node.Next!, value);

			resultNode.List!.Remove(resultNode.Previous!.Previous!);
			resultNode.List!.Remove(resultNode.Previous!);

			return resultNode;
		}
	}
}