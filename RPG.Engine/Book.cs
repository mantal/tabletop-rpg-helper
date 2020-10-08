using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RPG.Engine.Ids;
using RPG.Engine.Parser;
using RPG.Engine.Services;
using RPG.Engine.Utils;

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
		private bool _jsonHasNext = false;

		public Book(StatService statService, FunctionService functionService)
		{
			_statService = statService;
			_functionService = functionService;
			_parser = new Parser.Parser();
		}

		//TODO static? / DI
		public IEnumerable<string> Populate(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
				return new[] { "Empty json" };
			if (json[0] != '{')
				json = '{' + json + '}';

			_jsonHasNext = false;
			var errors = new List<string>();
			var reader = new JsonTextReader(new StringReader(json));
			var context = new ParsingContext
			{
				FunctionService = _functionService,
				StatService = _statService,
			};
			
			errors = errors.Concat(AddSection(reader, context, "#root", null)).ToList();

			//TODO tant qu'on ne continue pas apres une erreur on ne peut pas verifier si on a atteint la fin du fichier
			//if (_jsonHasNext && reader.ReadSkipComments())
			//	errors.Add(reader, "multiple root sections detected");

			return errors;
		}

		private IEnumerable<string> AddSection(JsonTextReader reader, ParsingContext context, string sectionId, string? parentSectionId)
		{
			var errors = new List<string>();

			if (!sectionId.StartsWith('#'))
			{
				errors.Add(reader, "section id should start with '#'");
				return errors;
			}
			if (_sections.ContainsKey(sectionId))
			{
				//TODO continue after error
				errors.Add(reader, $"section already exists: {sectionId}");
				return errors;
			}

			if (parentSectionId == null)
				_sections[sectionId] = new Section(sectionId, new Stat(new StatId("RootDefault"), Expression.Default));
			else
				_sections[sectionId] = new Section(sectionId, _sections[parentSectionId].Default);

			_jsonHasNext = reader.ReadSkipComments();
			if (!_jsonHasNext || reader.TokenType != JsonToken.StartObject)
			{
				errors.Add(reader, $"expected section start but got: {(string?)reader.Value}");
				return errors; //TODO continue after error
			}

			while ((_jsonHasNext = reader.ReadSkipComments()) && reader.TokenType != JsonToken.EndObject)
			{
				if (reader.TokenType != JsonToken.PropertyName)
				{
					errors.Add(reader, $"expected stat, function or section name but got: {(string?)reader.Value}");
					return errors; //TODO continue after error
				}

				var id = (string) reader.Value!;
				if (id.StartsWith('#'))
					errors = errors.Concat(AddSection(reader, context, id, sectionId)).ToList();
				else if (id.IsEquivalentTo("$Default"))
					errors = errors.Concat(SetDefault(reader, context, sectionId, parentSectionId)).ToList();
				else if (id.IsValidFunctionId())
					errors = errors.Concat(AddFunction(reader, context, id)).ToList();
				else if (id.IsValidStatId())
					errors = errors.Concat(AddStat(reader, context, sectionId, id)).ToList();
				else
				{
					errors.Add(reader, $"expected a valid function or stat id but found {id}");
					return errors; //TODO continue after error
				}

				if (errors.Any())
					return errors;
			}

			return errors;
		}

		private IEnumerable<string> AddFunction(JsonTextReader reader, ParsingContext context, string id)
		{
			var errors = new List<string>();

			if (reader.TokenType != JsonToken.StartObject)
			{
				//TODO continue on error
				errors.Add(reader, "expected function object");
				return errors;
			}
			errors.Add("custom functions are not implemented yet");
			return errors;
		}

		private IEnumerable<string> SetDefault(JsonTextReader reader, ParsingContext context, string sectionId, string? parentSectionId)
		{
			var errors = ParseStat(reader, context, sectionId, "default", out var stat).ToList();

			if (errors.Any())
				return errors;

			_sections[sectionId].Default = stat!; //TODO gerer par path => sect1#A != sect2#A

			return errors;
		}

		private IEnumerable<string> AddStat(JsonTextReader reader, ParsingContext context, string sectionId, string id)
		{
			var errors = ParseStat(reader, context, sectionId, id, out var stat);

			if (!errors.Any())
				errors = errors.Concat(_statService.Add(stat!).FormatErrors(reader));
			if (!errors.Any())
				_sections[sectionId].Stats.Add(stat!);

			return errors;
		}

		private IEnumerable<string> ParseStat(JsonTextReader reader, ParsingContext context, string sectionId, string id, out Stat? stat)
		{
			var errors = new List<string>();

			context.StatId = new StatId(id);
			stat = new Stat(_sections[sectionId].Default, context.StatId);

			_jsonHasNext = reader.ReadSkipComments();

			if (reader.TokenType.IsNumberOrString())
			{
				var rawExpression = GetValueAsString(reader);
				if (rawExpression == null)
				{
					errors.Add(reader, $"expected a string or a number but got {reader.Value?.ToString()}");
					return errors;
				}

				if (string.IsNullOrWhiteSpace(rawExpression))
					return errors;
				errors = _parser.Parse(out var expression, rawExpression, context).FormatErrors(reader).ToList();
				if (errors.Any())
					return errors;

				//TODO AddOrUpdate
				return errors.Concat(stat.AddOrUpdateExpression(expression!, sectionId.Replace("#", "_") + stat.Id)).FormatErrors(reader);
			}
			else if (reader.TokenType == JsonToken.StartObject)
			{
				while ((_jsonHasNext = reader.ReadSkipComments()) && reader.TokenType == JsonToken.PropertyName)
				{
					var expressionName = (string) reader.Value!;
					_jsonHasNext = reader.ReadSkipComments();
					if (!_jsonHasNext)
					{
						errors.Add(reader, "unexpected end");
						return errors;
					}

					if (expressionName.StartsWith(':'))
					{
						if (!expressionName.IsValidVariableId())
						{
							errors.Add(reader, $"{expressionName} looks like a variable declaration (starts with ':') but is not a valid variable id");
							continue;
						}
						var rawValue = GetValueAsString(reader);
						if (rawValue == null || !double.TryParse(rawValue, NumberStyles.Float, null, out var value))
						{
							errors.Add(reader, $"expected a number after variable declaration but got {reader.Value?.ToString()}");
							continue;
						}
						stat.AddOrUpdateVariable(new VariableId(expressionName, stat.Id), value);
					}
					else if (reader.TokenType.IsNumberOrString())
					{
						var rawExpression = GetValueAsString(reader);
						if (rawExpression == null)
						{
							errors.Add(reader, $"expected a string or a number after expression declaration but got {reader.Value?.ToString()}");
							continue;
						}

						// Since all stats defaults to 0 we can allow empty expressions
						// This is useful when default already provide everything
						if (string.IsNullOrWhiteSpace(rawExpression))
							continue;

						var exprErrors = _parser.Parse(out var expression, rawExpression, context).FormatErrors(reader);
						errors = errors.Concat(exprErrors).ToList();
						if (exprErrors.Any())
							continue;
						exprErrors = stat.AddOrUpdateExpression(expression!, expressionName).FormatErrors(reader);
						//TODO AddOrUpdate
						errors = errors.Concat(exprErrors).ToList();
					}
					else if (reader.TokenType == JsonToken.StartObject)
					{
						var props = DeserializeFlatObject(reader, true);
						var position = -1;

						if (!props.ContainsKey("expression"))
						{
							//TODO better msg
							errors.Add(reader, $"expression {expressionName} should have an \"expression\" property");
							props["expression"] = "+0"; //default value so we can get detect other errors
						}
						if (props.ContainsKey("position"))
						{
							var isInt = int.TryParse(props["position"], out position);
							if (!isInt)
							{
								//TODO fix line number / char pos
								//TODO better msg
								errors.Add(reader, $"expected position to be an integer but found {props["position"]}");
								position = -1; //default value so we can get detect other errors
							}
						}

						var exprErrors = _parser.Parse(out var expression, props["expression"], context).FormatErrors(reader);
						errors = errors.Concat(exprErrors).ToList();
						if (exprErrors.Any())
							continue;
						exprErrors = stat.AddOrUpdateExpression(expression!, expressionName, position).FormatErrors(reader);
						errors = errors.Concat(exprErrors).ToList();
						//TODO AddOrUpdate
					}
					else
					{
						errors.Add(reader, "expected expression string or expression object");
						return errors; // TODO continue on error
					}
				}
			}
			else
			{
				errors.Add(reader, "expected expression string or expression object");
				return errors;
			}

			return errors;
		}

		private IDictionary<string, string> DeserializeFlatObject(JsonTextReader reader, bool lowerPropertyNames = false)
		{
			var props = new Dictionary<string, string>();
			while ((_jsonHasNext = reader.ReadSkipComments()) && reader.TokenType != JsonToken.EndObject)
			{
				var name = (string) reader.Value!;
				if (lowerPropertyNames)
					name = name.ToLowerInvariant();

				var value = reader.ReadAsString()!;
				props[name] = value;
			}

			return props;
		}

		private string? GetValueAsString(JsonTextReader reader)
			=> reader.TokenType switch
			   {
				   _ when reader.Value == null => null,
				   JsonToken.String            => (string) reader.Value,
				   JsonToken.Integer           => ((long) reader.Value).ToString(NumberFormatInfo.InvariantInfo),
				   JsonToken.Float             => ((double) reader.Value).ToString(NumberFormatInfo.InvariantInfo),
				   _                           => null
			   };
	}

	//TODO make an Errors class
	public static class BookJsonExtensions
	{
		public static void Add(this IList<string> errors, JsonTextReader reader, string error)
		{
			//TODO file name
			errors.Add($"error: {reader.LineNumber}:{reader.LinePosition} {error}");
		}

		public static IEnumerable<string> FormatErrors(this IEnumerable<string> errors, JsonTextReader reader)
		{
			//TODO file name
			return errors.Select(e => $"error: {reader.LineNumber}:{reader.LinePosition} {e}");
		}

		public static bool ReadSkipComments(this JsonTextReader reader)
		{
			bool ret;
			while ((ret = reader.Read()) && reader.TokenType == JsonToken.Comment)
			{ }

			return ret;
		}
		
		public static bool IsNumberOrString(this JsonToken token)
			=> token switch
			   {
				   JsonToken.Integer => true,
				   JsonToken.Float   => true,
				   JsonToken.String  => true,
				   JsonToken.Boolean => true,
				   _                 => false,
			   };
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
