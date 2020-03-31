using System;
using System.Collections.Generic;
using System.Globalization;
using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
    public abstract class Node
	{
		public const int MinPriority = 0;
		public const int MaxPriority = 4;

		/// <summary>
		/// 4 = parenthesis
		/// 3 = function
		/// 2 = * / %
		/// 1 = + -
		/// 0 = stats variables
		/// </summary>
		public int Priority { get; }
		public NodeType Type { get; }
		protected readonly StatService StatService;
		
		protected Node(StatService statService, NodeType type, int priority)
		{
			StatService = statService;
			Type = type;
			Priority = priority;
		}

		public static Node FromString(string text, ParsingContext context)
		{
			if (string.Compare(text, ",", StringComparison.InvariantCultureIgnoreCase) == 0)
				return new GrammarNode(context.StatService, text, NodeType.ArgumentDivider, -1);
			if (string.Compare(text, "(", StringComparison.InvariantCultureIgnoreCase) == 0)
				return new GrammarNode(context.StatService, text, NodeType.LeftParenthesis, 4);
			if (string.Compare(text, ")", StringComparison.InvariantCultureIgnoreCase) == 0)
				return new GrammarNode(context.StatService, text, NodeType.RightParenthesis, -1);
			if (string.Compare(text, "{", StringComparison.InvariantCultureIgnoreCase) == 0)
				return new GrammarNode(context.StatService, text, NodeType.LeftBracket, -1);
			if (string.Compare(text, "}", StringComparison.InvariantCultureIgnoreCase) == 0)
				return new GrammarNode(context.StatService, text, NodeType.RightBracket, -1);
			if (string.Compare(text, "+", StringComparison.InvariantCultureIgnoreCase) == 0)
				return new AdditionOperatorNode(context.StatService, NodeType.PlusOperator);
			if (string.Compare(text, "-", StringComparison.InvariantCultureIgnoreCase) == 0)
				return new AdditionOperatorNode(context.StatService, NodeType.MinusOperator);
			if (string.Compare(text, "*", StringComparison.InvariantCultureIgnoreCase) == 0)
				return new MultiplierOperatorNode(context.StatService, NodeType.MultiplierOperator);
			if (string.Compare(text, "/", StringComparison.InvariantCultureIgnoreCase) == 0)
				return new MultiplierOperatorNode(context.StatService, NodeType.DivideOperator);
			if (string.Compare(text, "%", StringComparison.InvariantCultureIgnoreCase) == 0)
				return new MultiplierOperatorNode(context.StatService, NodeType.ModuloOperator);
			if (text.IsValidFunctionId())
				return new FunctionNode(context.StatService, context.FunctionService, text);
			if (text.IsValidVariableId())
				return new VariableNode(context.StatService, text, context.StatId);
			if (double.TryParse(text, NumberStyles.Float, null, out _))
				return new NumberNode(context.StatService, text);
			if (text.IsValidStatId())
				return new StatNode(context.StatService, text);

			return new InvalidNode(context.StatService, text);
		}

		// Does resolving this yield a ValueNode
		public abstract bool IsExpression();

		public virtual LinkedListNode<Node> Transform(LinkedListNode<Node> token) => token;

		public abstract IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context);
		public abstract LinkedListNode<Node> Apply(LinkedListNode<Node> node);

		public new abstract string ToString();

		public enum NodeType
		{
			Invalid = 0,
			Number = 1,
			PlusOperator = 2,
			MinusOperator = 3,
			MultiplierOperator = 4,
			DivideOperator = 5,
			ModuloOperator = 6,
			Stat = 7,
			Variable = 8,
			Function = 9,
			LeftParenthesis = 10,
			RightParenthesis = 11,
			LeftBracket = 12,
			RightBracket = 13,
			ArgumentDivider = 14,
			UnaryPlusOperator = 15,
			UnaryMinusOperator = 16,
		}
	}

	public abstract class ValueNode : Node
	{
		protected ValueNode(StatService statService, NodeType type, int priority)
			: base(statService, type, priority)
		{ }

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
			=> node;

		public abstract double GetValue();

		public override bool IsExpression() => true;
	}

	public class InvalidNode : Node
	{
		private readonly string _text;

		public InvalidNode(StatService statService, string text)
			: base(statService, NodeType.Invalid, -1)
		{
			_text = text;
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
			=> new[] { $"Invalid token: {_text}" };

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node) 
			=> throw new InvalidOperationException();

		public override string ToString() => _text;
		public override bool IsExpression() => false;
	}

	public static class LinkedListNodeExtensions
	{
		public static LinkedListNode<T>? Consume<T>(this LinkedListNode<T> node)
		{
			if (node.Next == null)
			{
				node.List.Remove(node);
				return null;
			}

			node = node.Next;
			// ReSharper disable once AssignNullToNotNullAttribute
			node.List.Remove(node.Previous);
			return node;
		}
	}
}
