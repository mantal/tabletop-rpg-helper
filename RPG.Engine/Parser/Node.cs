using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
    public abstract class Node
	{
		public const int MinPriority = 0;
		public const int MaxPriority = 3;

		/// <summary>
		/// 3 = parenthesis
		/// 2 = * / %
		/// 1 = + -
		/// 0 = numbers stats variables function
		/// </summary>
		public int Priority { get; }
		protected readonly NodeType Type;
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
				return new GrammarNode(context.StatService, text, NodeType.LeftParenthesis, 1);
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
			if (text.StartsWith('$')
				&& text.All(c => char.IsLetterOrDigit(c)
									|| c == '_'
									|| c == '-'
									|| c == '$'))
				return new FunctionNode(context.StatService, text);
			if (text.IsValidVariableId())
				return new VariableNode(context.StatService, text, context.StatId);
			if (double.TryParse(text, NumberStyles.Float, null, out _))
				return new NumberNode(context.StatService, text);
			if (text.IsValidStatId())
				return new StatNode(context.StatService, text);

			return new InvalidNode(context.StatService, text);
		}

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
		}
	}

	public abstract class ValueNode : Node
	{
		protected ValueNode(StatService statService, NodeType type)
			: base(statService, type, 0)
		{ }

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
			=> throw new InvalidOperationException();

		public abstract double GetValue();
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
	}
}
