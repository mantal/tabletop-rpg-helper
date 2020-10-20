using System.Collections.Generic;
using System.Linq;

namespace RPG.Engine.Parser
{
	public abstract class UnaryOperatorNode : Node, IParentNode
	{
		public IEnumerable<Expression> Children => new[] { Operand };

		protected Expression? Operand { get; set; }

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

		public override LinkedListNode<Node> OnAfterValidation(LinkedListNode<Node> node)
		{
			var next = node.Next!.Value.OnAfterValidation(node.Next!);
			Operand = new Expression(new LinkedList<Node>(new [] { next!.Value }));

			node.List!.Remove(node.Next!);

			return node;
		}

		public override bool IsValidLeftOperand() => false;

		public override bool IsValidRightOperand() => true;

		protected LinkedListNode<Node> ReplaceSelfWithResult(LinkedListNode<Node> node, double result)
		{
			var value = new NumberNode(result);

			var resultNode = node.List!.AddAfter(node, value);

			node.List!.Remove(node);

			return resultNode;
		}

		public override string ToString()
			=> Text + (Operand?.ToString() ?? "");
	}
}