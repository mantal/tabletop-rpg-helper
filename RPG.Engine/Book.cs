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
		public IEnumerable<string> PopulateFromFile(string json)
		{
			var errors = new List<string>();
			var reader = new JsonTextReader(new StringReader(json));
			
			if (!reader.ReadSkipComments())
			{
				errors.Add(reader, "empty json");
				return errors;
			}
			if (reader.TokenType != JsonToken.StartObject)
			{
				errors.Add(reader, "expected section start");
				return errors;
			}
			
			var context = new ParsingContext
			{
				FunctionService = _functionService,
				StatService = _statService,
			};
			
			errors = errors.Concat(AddSection(reader, context, "#root")).ToList();

			return errors;
		}
		
		private readonly IDictionary<string, Stat> _defaults = new Dictionary<string, Stat>();
		private readonly IList<string> _sections = new List<string>();

		private IEnumerable<string> AddSection(JsonTextReader reader, ParsingContext context, string sectionId)
		{
			var errors = new List<string>();

			if (!sectionId.StartsWith('#'))
			{
				errors.Add(reader, "section id should start with '#'");
				return errors;
			}
			if (_sections.Contains(sectionId))
			{
				//TODO continue after error
				errors.Add(reader, $"section already exists: {sectionId}");
				return errors;
			}

			_sections.Add(sectionId);
			while (reader.ReadSkipComments() && reader.TokenType != JsonToken.EndObject)
			{
				var id = (string) reader.Value!;
				if (id.StartsWith('#'))
					errors = errors.Concat(AddSection(reader, context, id)).ToList();
				else if (id.IsEquivalentTo("$Default"))
					; //TODO
				else if (id.IsValidFunctionId())
					errors = errors.Concat(AddFunction(reader, context, id)).ToList();
				else if (id.IsValidStatId())
					errors = errors.Concat(AddStat(reader, context, id)).ToList();
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
			errors.Add("custom function are not implemented");
			return errors;
		}

		private IEnumerable<string> AddStat(JsonTextReader reader, ParsingContext context, string id)
		{
			var errors = new List<string>();

			context.StatId = new StatId(id);

			reader.ReadSkipComments();

			if (reader.TokenType.IsValue())
			{
				var expr = (string) reader.Value!;
				var statErrors = _parser.Parse(out var stat, context, id, expr).FormatErrors(reader);
				errors = errors.Concat(statErrors).ToList();
				if (statErrors.Any())
					return errors;
				return errors.Concat(_statService.Add(stat!).FormatErrors(reader)).ToList();
			}
			else if (reader.TokenType == JsonToken.StartObject)
			{
				var hasError = false;
				var stat = new Stat(context.StatId, Expression.Default);
				while (reader.ReadSkipComments() && reader.TokenType == JsonToken.PropertyName)
				{
					var expressionName = (string) reader.Value!;
					reader.ReadSkipComments();

					if (reader.TokenType.IsValue())
					{
						var rawExpression = (string) reader.Value!;
						var statErrors = _parser.Parse(out var expression, rawExpression, context).FormatErrors(reader);
						errors = errors.Concat(statErrors).ToList();
						if (statErrors.Any())
							continue;
						statErrors = stat.AddExpression(expression!, expressionName).FormatErrors(reader);
						//TODO AddOrUpdate
						errors = errors.Concat(statErrors).ToList();
						if (statErrors.Any())
							hasError = true;
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
							hasError = true;
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
								hasError = true;
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
						if (exprErrors.Any())
							hasError = true;
					}
					else
					{
						errors.Add(reader, "expected expression string or expression object");
						return errors; // TODO continue on error
					}
				}

				if (!hasError)
					_statService.Add(stat);
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
}
