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

	public class NumberNode : ValueNode
	{
		private readonly double _value;

		public NumberNode(StatService statService, string token) : base(statService, NodeType.Number)
		{
			_value = double.Parse(token, NumberStyles.Float, null);
		}

		public NumberNode(StatService statService, double value) : base(statService, NodeType.Number)
		{
			_value = value;
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
			=> Enumerable.Empty<string>();

		public override double GetValue() => _value;

		public override string ToString() => _value.ToString(CultureInfo.InvariantCulture);
	}

	public class GrammarNode : Node
	{
		public string Text { get; }

		public GrammarNode(StatService statService, string text, NodeType type, int priority)
			: base(statService, type, priority)
		{
			Text = text;
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
			=> Enumerable.Empty<string>();

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
			=> throw new InvalidOperationException();

		public override string ToString() => Text;
	}

	public class AdditionOperatorNode : Node
	{
		private readonly char _symbol;

		public AdditionOperatorNode(StatService statService, NodeType type) 
			: base(statService, type, 2)
		{
			if (type == NodeType.PlusOperator)
				_symbol = '+';
			else if (type == NodeType.MinusOperator)
				_symbol = '-';
			else
				throw new ArgumentException();
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			var errors = new List<string>();

			if (token.Next == null)
				return new [] { $"Expected operand after operator '{_symbol}' but found nothing" };

			var next = token.Next.Value;

			if (next is GrammarNode grammarNode)
			{
				errors.Add($"Expected operand after operator '{_symbol}' but found '{grammarNode.Text}'");
				return errors;
			}
			
			return Enumerable.Empty<string>();
		}

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			var previous = node.Previous?.Value as ValueNode;
			var isBinary = previous != null;

			var a = 0d;
			if (isBinary)
				a = previous.GetValue();

			// should never happen so let it blow
			// ReSharper disable once PossibleNullReferenceException
			var b = ((ValueNode)node.Next.Value).GetValue();

			var value = new NumberNode(StatService, Type == NodeType.PlusOperator ? a + b : a - b);

			var result = node.List.AddAfter(node.Next, value);

			if (isBinary)
				result.List.Remove(result.Previous.Previous.Previous);
			result.List.Remove(result.Previous.Previous);
			result.List.Remove(result.Previous);

			return result;
		}

		public override string ToString() => _symbol.ToString();
	}

	public class MultiplierOperatorNode : Node
	{
		private readonly char _symbol;

		public MultiplierOperatorNode(StatService statService, NodeType type) 
			: base(statService, type, 3)
		{
			if (type == NodeType.MultiplierOperator)
				_symbol = '*';
			else if (type == NodeType.DivideOperator)
				_symbol = '/';
			else if (type == NodeType.ModuloOperator)
				_symbol = '%';
			else
				throw new ArgumentException();
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			var errors = new List<string>();

			if (token.Previous == null)
				return new[] { $"Expected operand before operator '{_symbol}' but found nothing" };

			var previous = token.Previous.Value;
			if (previous is GrammarNode previousNode)
			{
				errors.Add($"Expected operand before operator '{_symbol}' but found '{previousNode.Text}'");
				return errors;
			}

			if (token.Next == null)
				return new[] { $"Expected operand after operator '{_symbol}' but found nothing" };

			var next = token.Next.Value;
			if (next is GrammarNode nextNode)
			{
				errors.Add($"Expected operand after operator '{_symbol}' but found '{nextNode.Text}'");
				return errors;
			}

			return Enumerable.Empty<string>();
		}

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			// should never happen so let it blow
			// ReSharper disable once PossibleNullReferenceException
			var a = ((ValueNode)node.Previous.Value).GetValue();
			// should never happen so let it blow
			// ReSharper disable once PossibleNullReferenceException
			var b = ((ValueNode)node.Next.Value).GetValue();

			var x = Type switch
					{
						NodeType.MultiplierOperator => a * b,
						NodeType.DivideOperator     => a / b,
						NodeType.ModuloOperator     => a % b,
						_                           => throw new InvalidOperationException(),
					};

			var value = new NumberNode(StatService, x);

			var result = node.List.AddAfter(node.Next, value);

			result.List.Remove(result.Previous.Previous.Previous);
			result.List.Remove(result.Previous.Previous);
			result.List.Remove(result.Previous);

			return result;
		}

		public override string ToString() => _symbol.ToString();
	}

	public class StatNode : ValueNode
	{
		public readonly StatId Id;

		public StatNode(StatService statService, string text)
			: base(statService, NodeType.Variable)
		{
			Id = new StatId(text);
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			if (!context.StatService.Exists(Id))
				return new[] { $"Undeclared stat id: {Id}" };
			return Enumerable.Empty<string>();
		}

		public override double GetValue() => StatService.GetValue(Id);

		public override string ToString() => Id.ToString();
	}

	public class VariableNode : ValueNode
	{
		public readonly VariableId Id;

		public VariableNode(StatService statService, string text, StatId parentId)
			: base(statService, NodeType.Stat)
		{
			Id = new VariableId(text, parentId);
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			if (Id.StatId != context.StatId && !context.StatService.Exists(Id))
				return new[] { $"Undeclared variable id: {Id}" };
			return Enumerable.Empty<string>();
		}

		public override double GetValue() => StatService.GetValue(Id);

		public override string ToString() => Id.ToString();
	}

	public class FunctionNode : ValueNode
	{
		//todo quand impl: update une fonction ne peut pas changer son numbre d'args
		public FunctionNode(StatService statService, string text) 
			: base(statService, NodeType.Function)
		{ }

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			throw new NotImplementedException();
		}

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			throw new NotImplementedException();
		}

		public override double GetValue() { throw new NotImplementedException(); }

		public override string ToString() => "non_func";
	}
}
