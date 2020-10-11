using System.Collections.Generic;
using System.Linq;

namespace RPG.Engine.Parser
{
	public abstract class BinaryOperatorNode : Node
	{
		protected BinaryOperatorNode(string text, NodeType type, int priority) 
			: base(text, type, priority)
		{ }

		public override IEnumerable<string> IsValid(LinkedListNode<Node> node)
		{
			var errors = new List<string>();

			if (node.Previous == null)
				return new[] { $"Expected operand before operator '{Text}' but found nothing" };

			var previous = node.Previous.Value;
			if (!previous.IsValidLeftOperand())
			{
				errors.Add($"Expected operand before operator '{Text}' but found '{previous}'");
				return errors;
			}

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

		public override bool IsValidRightOperand() => false;

		protected LinkedListNode<Node> ReplaceSelfWithResult(LinkedListNode<Node> node, double result)
		{
			var value = new NumberNode(result);

			var resultNode = node.List.AddAfter(node.Next, value);

			resultNode.List.Remove(resultNode.Previous.Previous.Previous);
			resultNode.List.Remove(resultNode.Previous.Previous);
			resultNode.List.Remove(resultNode.Previous);

			return resultNode;
		}
	}
}