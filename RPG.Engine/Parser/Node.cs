using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Utils;

namespace RPG.Engine.Parser
{
	public interface IParentNode
	{
		IEnumerable<Expression> Children { get; }
	}

    public abstract class Node
	{
		public const int MinPriority = 1;
		public const int MaxPriority = 8;

		/// <summary>
		/// 8  parenthesis
		/// 7  unary operators: + - ~
		/// 6  * / %
		/// 5  + -
		/// 4  relational operators: <![CDATA[ > >= < <= = ]]>
		/// 3  and operator: &
		/// 2  or and xor operators: | ^
		/// 1  function, stats and variables
		/// </summary>
		public int Priority { get; }
		public NodeType Type { get; }

		protected string Text { get; }
		
		protected Node(string text, NodeType type, int priority)
		{
			Text = text;
			Type = type;
			Priority = priority;
		}

		public static Node FromString(string text, ParsingContext context)
		{
			if (text.IsEquivalentTo(","))
				return new ArgumentSeparatorNode(text, NodeType.ArgumentDivider);
			if (text.IsEquivalentTo("("))
				return new ParenthesisNode(text, NodeType.LeftParenthesis);
			if (text.IsEquivalentTo(")"))
				return new GrammarNode(text, NodeType.RightParenthesis, -1);
			if (text.IsEquivalentTo("{"))
				return new GrammarNode(text, NodeType.LeftBracket, -1);
			if (text.IsEquivalentTo("}"))
				return new GrammarNode(text, NodeType.RightBracket, -1);
			if (text.IsEquivalentTo("+"))
				return new AdditionOperatorNode(context.FunctionService, text, NodeType.PlusOperator);
			if (text.IsEquivalentTo("-"))
				return new AdditionOperatorNode(context.FunctionService, text, NodeType.MinusOperator);
			if (text.IsEquivalentTo("*"))
				return new MultiplierOperatorNode(text, NodeType.MultiplierOperator);
			if (text.IsEquivalentTo("/"))
				return new MultiplierOperatorNode(text, NodeType.DivideOperator);
			if (text.IsEquivalentTo("%"))
				return new MultiplierOperatorNode(text, NodeType.ModuloOperator);
			if (text.IsEquivalentTo(">"))
				return new RelationalOperatorNode(text, NodeType.GreaterThanOperator);
			if (text.IsEquivalentTo(">="))
				return new RelationalOperatorNode(text, NodeType.GreaterThanOrEqualOperator);
			if (text.IsEquivalentTo("="))
				return new RelationalOperatorNode(text, NodeType.EqualOperator);
			if (text.IsEquivalentTo("~="))
				return new RelationalOperatorNode(text, NodeType.NotEqualOperator);
			if (text.IsEquivalentTo("<"))
				return new RelationalOperatorNode(text, NodeType.LessThanOperator);
			if (text.IsEquivalentTo("<="))
				return new RelationalOperatorNode(text, NodeType.LessThanOrEqualOperator);
			if (text.IsEquivalentTo("&"))
				return new AndOperatorNode(text, NodeType.AndOperator);
			if (text.IsEquivalentTo("|"))
				return new OrXorOperatorNode(text, NodeType.OrOperator);
			if (text.IsEquivalentTo("^"))
				return new OrXorOperatorNode(text, NodeType.XorOperator);
			if (text.IsEquivalentTo("~"))
				return new NotOperatorNode(text, NodeType.UnaryNotOperator);
			if (text.IsValidFunctionId())
				return new FunctionNode(context.FunctionService, text, context.FunctionId);
			if (text.IsValidVariableId())
				return new VariableNode(context.StatService, text, context.StatId);
			if (double.TryParse(text, NumberStyles.Float, null, out _))
				return new NumberNode(text);
			if (text.IsValidStatId())
				return new StatNode(context.StatService, text);

			return new InvalidNode(text);
		}

		public abstract bool IsValidLeftOperand();
		public abstract bool IsValidRightOperand();

		/// <summary>
		/// Transform nodes before validation happen
		/// </summary>
		/// <returns>the current node or the one that replaced it</returns>
		public virtual LinkedListNode<Node> OnBeforeValidation(LinkedListNode<Node> node) => node;

		public abstract IEnumerable<string> IsValid(LinkedListNode<Node> node);

		/// <summary>
		/// Transform nodes after validation happen
		/// </summary>
		/// <returns>the current node or the one that replaced it</returns>
		public virtual LinkedListNode<Node> OnAfterValidation(LinkedListNode<Node> node) => node;

		public abstract LinkedListNode<Node> Apply(LinkedListNode<Node> node);

		public override string ToString() => Text;

		public enum NodeType
		{
			Invalid,
			Number,
			PlusOperator,
			MinusOperator,
			MultiplierOperator,
			DivideOperator,
			ModuloOperator,
			Stat,
			Variable,
			Function,
			LeftParenthesis,
			RightParenthesis,
			LeftBracket,
			RightBracket,
			ArgumentDivider,
			UnaryPlusOperator,
			UnaryMinusOperator,
			GreaterThanOperator,
			GreaterThanOrEqualOperator,
			LessThanOperator,
			LessThanOrEqualOperator,
			EqualOperator,
			NotEqualOperator,
			AndOperator,
			OrOperator,
			XorOperator,
			UnaryNotOperator,
		}

		public abstract Node Clone();
	}

	public abstract class ValueNode : Node
	{
		protected ValueNode(string text, NodeType type, int priority)
			: base(text, type, priority)
		{ }

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
			=> node;

		public abstract double GetValue();

		public override IEnumerable<string> IsValid(LinkedListNode<Node> node)
		{
			// Only check the left to prevent producing too many errors 
			if (node.Previous == null || !node.Previous.Value.IsValidLeftOperand() || node.Previous.Value is FunctionNode)
				return Enumerable.Empty<string>();
			return new[] { $"missing operator or argument separator around value {node.Value}" };
		}

		public override bool IsValidLeftOperand() => true;
		public override bool IsValidRightOperand() => true;
	}

	public class InvalidNode : Node
	{
		public InvalidNode(string text)
			: base(text, NodeType.Invalid, -1)
		{ }

		public override IEnumerable<string> IsValid(LinkedListNode<Node> node)
			=> new[] { @$"invalid expression ""{Text}"". Valid expression are number, stat, variable or function name, and operators" };

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node) 
			=> throw new InvalidOperationException();

		public override bool IsValidLeftOperand() => false;
		public override bool IsValidRightOperand() => false;
		public override Node Clone() => new InvalidNode(Text);
	}

	public static class LinkedListNodeExtensions
	{
		public static LinkedListNode<T>? Consume<T>(this LinkedListNode<T> node)
		{
			if (node.Next == null)
			{
				node.List!.Remove(node);
				return null;
			}

			node = node.Next;
			node.List!.Remove(node.Previous!);

			return node;
		}
	}
}
