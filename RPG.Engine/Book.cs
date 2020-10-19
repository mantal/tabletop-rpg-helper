using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using RPG.Engine.Functions;
using RPG.Engine.Ids;
using RPG.Engine.Parser;
using RPG.Engine.Services;
using RPG.Engine.Utils;
using Node = RPG.Engine.Utils.BookParser.Node;
using NodeType = RPG.Engine.Utils.BookParser.NodeType;

namespace RPG.Engine
{
    public class Book
	{
		//public IEnumerable<Section> Sections => _sections.Values; //TODO
		public IReadOnlyDictionary<string, Section> Sections => (IReadOnlyDictionary<string, Section>) _sections;
		private readonly IDictionary<string, Section> _sections = new Dictionary<string, Section>();

		private readonly StatService _statService;
		private readonly FunctionService _functionService;
		private readonly Parser.Parser _parser;

		public Book(StatService statService, FunctionService functionService)
		{
			_statService = statService;
			_functionService = functionService;
			_parser = new Parser.Parser();
		}

		public IEnumerable<string> Populate(string json)
		{
			try
			{
				return Populate(new BookParser(new StringReader(json)).Parse());
			}
			catch (Exception e)
			{
				return new[] { e.Message }; //TODO better
			}
		}

		//TODO static? / DI
		public IEnumerable<string> Populate(Node node)
		{
			var errors = new List<string>();
			var context = new ParsingContext(_statService, _functionService);

			if (!node.Children.Any())
				return new[] { "empty book" };

			errors = errors.Concat(AddSection(node, context, null)).ToList();

			//TODO tant qu'on ne continue pas apres une erreur on ne peut pas verifier si on a atteint la fin du fichier
			//if (_jsonHasNext && reader.ReadSkipComments())
			//	errors.Add(reader, "multiple root sections detected");

			return errors;
		}

		private IEnumerable<string> AddSection(Node node, ParsingContext context, string? parentSectionId)
		{
			var errors = new List<string>();

			if (_sections.ContainsKey(node.Value))
			{
				//TODO continue after error
				errors.Add(node, $"section already exists: {node.Value}");
				return errors;
			}

			if (parentSectionId == null)
				_sections[node.Value] = new Section(node.Value);
			else
				_sections[node.Value] = new Section(node.Value, _sections[parentSectionId].Default);

			foreach(var child in node.Children)
			{
				if (child.IsFunction())
					errors = errors.Concat(AddFunction(child, context)).ToList();
				else if (child.IsSection())
					errors = errors.Concat(AddSection(child, context, node.Value)).ToList();
				else if (child.Value.IsEquivalentTo("_default"))
					errors = errors.Concat(SetDefault(child, context, node.Value, parentSectionId)).ToList();
				else if (child.IsStat())
					errors = errors.Concat(AddStat(child, context, node.Value)).ToList();
				else
				{
					errors.Add(child, $"expected a valid function, stat or section id but found '{child.Value}'");
					return errors;
				}

				if (errors.Any())
					return errors;
			}

			return errors;
		}

		private IEnumerable<string> AddFunction(Node node, ParsingContext context)
		{
			var errors = new List<string>();

			context.FunctionId = new FunctionId(node.Value);

			if (node.Type == NodeType.PropertyIdentifier)
			{
				var expressionNode = node.Children.First();
				errors = _parser.Parse(out var expression, expressionNode.Value, context).FormatErrors(expressionNode).ToList();
				if (errors.Any())
					return errors;

				errors = errors.Concat(_functionService.Add(new UserFunction(context.FunctionId, expression!, _functionService)).FormatErrors(node))
							   .FormatErrors(expressionNode)
							   .ToList();
			}
			else
			{
				errors.Add(node, "no"); //TODO
			}

			context.FunctionId = null;

			return errors;
		}

		private IEnumerable<string> SetDefault(Node node, ParsingContext context, string sectionId, string? parentSectionId)
		{
			var errors = ParseStat(node, context, sectionId, out var stat).ToList();

			if (errors.Any())
				return errors;

			_sections[sectionId].Default = stat!; //TODO gerer par path => sect1#A != sect2#A

			return errors;
		}

		private IEnumerable<string> AddStat(Node node, ParsingContext context, string sectionId)
		{
			var errors = ParseStat(node, context, sectionId, out var stat);

			if (!errors.Any())
				errors = errors.Concat(_statService.Add(stat).FormatErrors(node));
			if (!errors.Any())
				_sections[sectionId].Stats.Add(stat!);

			return errors;
		}

		private IEnumerable<string> ParseStat(Node statNode, ParsingContext context, string sectionId, out Stat stat)
		{
			var errors = new List<string>();

			context.StatId = new StatId(statNode.Value);
			stat = new Stat(_sections[sectionId].Default, context.StatId);

			if (statNode.Type == NodeType.PropertyIdentifier)
			{
				var statExpressionNode = statNode.Children.First();
				errors = _parser.Parse(out var expression, statExpressionNode.Value, context).FormatErrors(statExpressionNode).ToList();
				if (errors.Any())
					return errors;

				//TODO AddOrUpdate
				return errors.Concat(stat.AddOrUpdateExpression(expression!, sectionId + '_' + stat.Id)).FormatErrors(statExpressionNode);
			}
			else if (statNode.Type == NodeType.ObjectIdentifier)
			{
				foreach (var expressionId in statNode.Children)
				{
					if (expressionId.Value.StartsWith('.'))
					{
						if (!expressionId.Value.IsValidVariableId())
						{
							errors.Add(expressionId, $"{expressionId.Value} looks like a variable declaration (starts with '.') but is not a valid variable id");
							continue;
						}

						if (expressionId.Type != NodeType.PropertyIdentifier)
						{
							errors.Add(expressionId, $"{expressionId.Value} looks like a variable declaration (starts with '.') but is followed by '{{'");
							continue;
						}

						var expressionNode = expressionId.Children.First();
						if (!expressionNode.IsNumber())
						{
							errors.Add(expressionId, $"{expressionId.Value} looks like a variable declaration (starts with '.') but it's value ({expressionNode.Value}) is not a number");
							continue;
						}

						var value = double.Parse(expressionNode.Value, NumberStyles.Float);
						stat.AddOrUpdateVariable(new VariableId(expressionId.Value, stat.Id), value);
					}
					else if (expressionId.Type == NodeType.PropertyIdentifier)
					{
						//TODO check name validity

						var expressionValueNode = expressionId.Children.First();

						var exprErrors = _parser.Parse(out var expression, expressionValueNode.Value, context).FormatErrors(expressionValueNode);
						errors = errors.Concat(exprErrors).ToList();
						if (exprErrors.Any())
							continue;
						exprErrors = stat.AddOrUpdateExpression(expression!, expressionId.Value).FormatErrors(expressionValueNode);
						//TODO AddOrUpdate
						errors = errors.Concat(exprErrors).ToList();
					}
					else if (expressionId.Type == NodeType.ObjectIdentifier)
					{
						//TODO handle duplicates
						
						var position = -1;
						var positionNode = expressionId.Children.FirstOrDefault(c => c.Value.IsEquivalentTo("position"));
						if (positionNode != null)
						{
							var positionValueNode = positionNode.Children.First();
							if (positionValueNode.Type != NodeType.Integer)
							{
								//TODO stat path
								errors.Add(positionNode.Children.First(), $"expected position to be an integer but found {positionNode.Value}");
							}

							position = int.Parse(positionValueNode.Value);
						}

						var extraNodes = expressionId.Children.Where(c => !c.Value.IsEquivalentTo("expression")
																		  && !c.Value.IsEquivalentTo("position"));
						if (extraNodes.Any())
						{
							// TODO add stat.expression path to error
							errors.Add(expressionId, $"unknown properties ({string.Join(',', $"'{string.Join("", extraNodes.Select(n => n.Value))}'")}) will be ignored. Allowed properties are 'expression' and 'position'. Did you forget an '#' at the start of a parent section name?");
						}

						// TODO rename to 'value'
						var valueIdNode = expressionId.Children.FirstOrDefault(c => c.Value.IsEquivalentTo("expression"));
						if (valueIdNode == null)
						{
							errors.Add(expressionId, $"expression {expressionId} of stat {statNode.Value} should have an \"expression\" property");
							continue;
						}
						if (valueIdNode.Type != NodeType.PropertyIdentifier)
						{
							errors.Add(valueIdNode, $"TODO value type error"); //TODO
							continue;
						}

						var valueNode = valueIdNode.Children.First();
						if (!valueNode.IsValue())
						{
							errors.Add(valueIdNode.Children.First(), $"TODO value error");
							continue;
						}

						var valueErrors = _parser.Parse(out var expression, valueNode.Value, context).FormatErrors(valueNode);
						errors = errors.Concat(valueErrors).ToList();
						if (valueErrors.Any())
							continue;
						valueErrors = stat.AddOrUpdateExpression(expression!, expressionId.Value, position).FormatErrors(valueNode);
						errors = errors.Concat(valueErrors).ToList();
					}
					else
					{
						//TODO this should never happen??
						errors.Add(expressionId, "expected expression string or expression object");
						return errors; // TODO continue on error
					}
				}
			}
			else
			{
				// TODO this should never happen
				errors.Add(statNode, "expected expression string or expression object");
				return errors;
			}

			return errors;
		}
	}

	//TODO make an Errors class
	public static class BookJsonExtensions
	{
		public static void Add(this IList<string> errors, Node node, string error)
		{
			//TODO file name
			errors.Add($"error: {node.LineNumber}:{node.LinePosition} {error}");
		}

		public static IEnumerable<string> FormatErrors(this IEnumerable<string> errors, Node node)
		{
			//TODO file name
			//TODO more precise line number / position
			return errors.Select(e => $"error: {node.LineNumber}:{node.LinePosition} {e}");
		}

		public static bool IsValidSectionId(this string s)
			=> s.StartsWith('#')
			   && s.Skip(1).All(c => char.IsLetterOrDigit(c)
							 || c == '-'
							 || c == '_');

		public static bool IsStat(this Node node)
			=> node.Value.IsValidStatId()
			   && (node.Type == NodeType.PropertyIdentifier
					|| node.Type == NodeType.ObjectIdentifier);

		public static bool IsFunction(this Node node)
			=> node.Value.IsValidFunctionId();

		public static bool IsSection(this Node node)
			=> node.Type == NodeType.ObjectIdentifier
			   && node.Value.IsValidSectionId();

		public static bool IsNumber(this Node node)
			=> node.Type == NodeType.Integer
			   || node.Type == NodeType.Float;

		public static bool IsValue(this Node node)
			=> node.IsNumber()
			   || node.Type == NodeType.String;
	}

	public class Section
	{
		public string Name { get; }
		public Stat Default { get; set; }
		public IList<Stat> Stats { get; set; }

		public Section(string name, Stat? @default = null)
		{
			Name = name;
			Default = @default ?? new Stat(new StatId(name.Substring(1) + "_default"), Expression.Default);
			Stats = new List<Stat>();
		}
	}
}
