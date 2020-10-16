using System.Collections.Generic;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class AdditionOperatorNode : BinaryOperatorNode
	{
		private readonly FunctionService _functionService;

		public AdditionOperatorNode(FunctionService functionService, string text, NodeType type) 
			: base(text, type, 2)
		{
			_functionService = functionService;
		}

		public override LinkedListNode<Node> OnBeforeValidation(LinkedListNode<Node> node)
		{
			var previous = node.Previous?.Value;
			var isUnary = previous == null || !previous.IsValidLeftOperand();

			if (previous is FunctionNode functionNode
				&& _functionService.Exists(functionNode.Id))
			{
				isUnary = _functionService.Get(functionNode.Id).RequiredParameterNumber > 0;
			}

			if (!isUnary) 
				return node;

			var type = Type == NodeType.PlusOperator ? NodeType.UnaryPlusOperator : NodeType.UnaryMinusOperator;

			var resultNode = node.List!.AddAfter(node, new UnaryAdditionOperatorNode(Text, type));
			node.List!.Remove(node);

			return resultNode;
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
		
		public override Node Clone() => new AdditionOperatorNode(_functionService, Text, Type);
	}
}