using System.Collections.Generic;
using System.Linq;

namespace RPG.Engine.Parser
{
    public class ParenthesisNode : Node, IParentNode
	{
		public IEnumerable<Expression> Children => new[] { Child ?? Expression.Default };

		private Expression? Child;

		public ParenthesisNode(string text, NodeType type)
			: base(text, type, 8)
		{ }

		public override IEnumerable<string> IsValid(LinkedListNode<Node> start)
		{
			var errors = new List<string>();

			//TODO copied from ValueNode.IsValid
			// Only check the left to prevent producing too many errors 
			if (start.Previous == null || !start.Previous.Value.IsValidLeftOperand()
									   || start.Previous.Value is FunctionNode)
			{ }
			else
				errors.Add($"missing operator or argument separator around value {start.Value}");
			if (start.Next == null)
				return new[] { $"Expected expression after left parenthesis '(' but found nothing" };
			if (start.Next.Value.Type == NodeType.RightParenthesis)
				return new[] { $"Expected expression after left parenthesis '(' but found right parenthesis ')'" };

			var node = start.Next;
			var parenthesisCount = 1;
			for (; node != null && parenthesisCount > 0; node = node.Next)
			{
				if (node.Value.Type == NodeType.LeftParenthesis)
					parenthesisCount++;
				if (node.Value.Type == NodeType.RightParenthesis)
					parenthesisCount--;
			}

			if (parenthesisCount > 0)
				return errors.Append($"missing closing parenthesis");
			else if (parenthesisCount < 0)
				return errors.Append($"too many closing parenthesis");

			return errors;
		}

		public override LinkedListNode<Node> OnAfterValidation(LinkedListNode<Node> start)
		{
			var childNodes = new LinkedList<Node>();

			var node = start.Next!;

			while (node.Value.Type != NodeType.RightParenthesis)
			{
				node = node.Value.OnAfterValidation(node)!;
				childNodes.AddLast(node.Value);

				node = node.Consume()!;
			}

			node.List!.Remove(node); // remove '}'

			Child = new Expression(childNodes);

			return start;
		}

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			var value = new NumberNode(Child!.Resolve());

			var resultNode = node.List!.AddAfter(node, value);

			node.List!.Remove(node);

			return resultNode;
		}

		public override string ToString()
			=> $"({Child})";

		public override bool IsValidLeftOperand() => false;
		public override bool IsValidRightOperand() => true;

		public override Node Clone() => new ParenthesisNode(Text, Type);
	}
}
