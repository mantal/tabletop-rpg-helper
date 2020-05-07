using System;
using System.Collections.Generic;
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

		public Book(StatService statService, FunctionService functionService)
		{
			_statService = statService;
			_functionService = functionService;
			_parser = new Parser.Parser();
		}

		//TODO static? / DI
		public IEnumerable<string> Populate(string json)
		{
			var errors = new List<string>();
			var reader = new JsonTextReader(new StringReader(json));
			var context = new ParsingContext
			{
				FunctionService = _functionService,
				StatService = _statService,
			};

			if (string.IsNullOrWhiteSpace(json))
				return new[] { "Empty json" };

			errors = errors.Concat(AddSection(reader, context, "#root", null)).ToList();

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

			reader.ReadSkipComments();
			if (reader.TokenType != JsonToken.StartObject)
			{
				errors.Add(reader, $"expected section start but got: {(string?)reader.Value}");
				return errors; //TODO continue after error
			}

			while (reader.ReadSkipComments() && reader.TokenType != JsonToken.EndObject)
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

			reader.ReadSkipComments();

			if (reader.TokenType.IsValue())
			{
				var rawExpression = (string) reader.Value!;
				errors = _parser.Parse(out var expression, rawExpression, context).FormatErrors(reader).ToList();
				if (errors.Any())
					return errors;

				stat.AddExpression(expression!, "last"); //TODO expression name??
				return errors;
			}
			else if (reader.TokenType == JsonToken.StartObject)
			{
				while (reader.ReadSkipComments() && reader.TokenType == JsonToken.PropertyName)
				{
					var expressionName = (string) reader.Value!;
					reader.ReadSkipComments();

					//TODO handle variables
					if (reader.TokenType.IsValue())
					{
						var rawExpression = (string) reader.Value!;
						var exprErrors = _parser.Parse(out var expression, rawExpression, context).FormatErrors(reader);
						errors = errors.Concat(exprErrors).ToList();
						if (exprErrors.Any())
							continue;
						exprErrors = stat.AddExpression(expression!, expressionName).FormatErrors(reader);
						//TODO AddOrUpdate
						errors = errors.Concat(exprErrors).ToList();
					}
					else if (reader.TokenType == JsonToken.StartObject)
					{
						var props = DeserializeFlatObject(reader);
						var position = -1;

						//TODO case insensitive
						if (!props.ContainsKey("Expression"))
						{
							//TODO better msg
							errors.Add(reader, $"expression {expressionName} should have an \"Expression\" property");
							props["Expression"] = "+0"; //default value so we can get detect other errors
						}
						if (props.ContainsKey("Position"))
						{
							var isInt = int.TryParse(props["Position"], out position);
							if (!isInt)
							{
								//TODO fix line number / char pos
								//TODO better msg
								errors.Add(reader, $"expected position to be an integer but found {props["Position"]}");
								position = -1; //default value so we can get detect other errors
							}
						}

						var exprErrors = _parser.Parse(out var expression, props["Expression"], context).FormatErrors(reader);
						errors = errors.Concat(exprErrors).ToList();
						if (exprErrors.Any())
							continue;
						exprErrors = stat.AddExpression(expression!, expressionName, position).FormatErrors(reader);
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

			return errors;
		}

		private IDictionary<string, string> DeserializeFlatObject(JsonReader reader)
		{
			var props = new Dictionary<string, string>();
			while (reader.Read() && reader.TokenType != JsonToken.EndObject)
			{
				var name = (string) reader.Value!;
				var value = reader.ReadAsString()!;
				props[name] = value;
			}

			return props;
		}
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

		public static bool ReadSkipComments(this JsonReader reader)
		{
			bool ret;
			while ((ret = reader.Read()) && reader.TokenType == JsonToken.Comment)
			{ }

			return ret;
		}

		public static bool IsValue(this JsonToken token)
			=> token switch
			   {
				   JsonToken.None             => false,
				   JsonToken.StartObject      => false,
				   JsonToken.StartArray       => false,
				   JsonToken.StartConstructor => false,
				   JsonToken.PropertyName     => false,
				   JsonToken.Comment          => false,
				   JsonToken.Raw              => false,
				   JsonToken.Null             => false,
				   JsonToken.Undefined        => false,
				   JsonToken.EndObject        => false,
				   JsonToken.EndArray         => false,
				   JsonToken.EndConstructor   => false,
				   JsonToken.Integer          => true,
				   JsonToken.Float            => true,
				   JsonToken.String           => true,
				   JsonToken.Boolean          => true,
				   JsonToken.Date             => true,
				   JsonToken.Bytes            => true,
				   _                          => throw new ArgumentOutOfRangeException(nameof(token), token, null)
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
