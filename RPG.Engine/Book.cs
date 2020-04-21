using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RPG.Engine.Ids;
using RPG.Engine.Parser;
using RPG.Engine.Services;

namespace RPG.Engine
{
    public class Book
    {
		private readonly StatService _statService;
		private readonly FunctionService _functionService;

		public Book(StatService statService, FunctionService functionService)
		{
			_statService = statService;
			_functionService = functionService;
		}

		//TODO static? / DI
		public IEnumerable<string> PopulateFromFile(string json)
		{
			var errors = new List<string>();
			var reader = new JsonTextReader(new StringReader(json));
			
			if (!reader.Read())
			{
				errors.Add(reader, "empty json");
				return errors;
			}
			if (reader.TokenType != JsonToken.StartObject)
			{
				errors.Add(reader, "expected section start");
			}
			
			var context = new ParsingContext
			{
				FunctionService = _functionService,
				StatService = _statService,
			};
			var parser = new Parser.Parser();
			while (reader.Read() && reader.TokenType != JsonToken.EndObject)
			{
				while (reader.TokenType == JsonToken.Comment && reader.Read())
				{ }

				if (reader.TokenType == JsonToken.PropertyName)
				{
					//todo gerer le $default
					var stringId = (string) reader.Value!;
					var isStat = stringId.IsValidStatId();
					var isFunction = stringId.IsValidFunctionId();
					if (!isStat && !isFunction)
					{
						//TODO discard until next property
						errors.Add(reader, "invalid variable id");
						continue;
					}
					context.StatId = new StatId(stringId);

					reader.Read();

					if (isFunction)
					{
						if (reader.TokenType != JsonToken.StartObject)
						{
							//TODO continue on error
							errors.Add(reader, "expected function object");
							return errors;
						}
						//TODO add function
					}
					else
					{
						if (reader.TokenType.IsValue())
						{
							var expr = (string) reader.Value!;
							parser.Parse(out var stat, context, stringId, expr);
							_statService.Add(stat!);
						}
						else if (reader.TokenType == JsonToken.StartObject)
						{
							var stat = new Stat(context.StatId, Expression.Default);
							while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
							{
								var expressionName = (string) reader.Value!;
								reader.Read();

								if (reader.TokenType.IsValue())
								{
									var rawExpression = (string) reader.Value!;
									parser.Parse(out var expression, rawExpression, context);
									stat.AddExpression(expression!, expressionName); //TODO AddOrUpdate
								}
								else if (reader.TokenType == JsonToken.StartObject)
								{
									var props = DeserializeFlatObject(reader);
									var position = -1;

									//TODO case insensitive
									if (!props.ContainsKey("Expression"))
									{
										//TODO fix line number / char pos
										//TODO better msg
										errors.Add(reader, $"expression {expressionName} has no value");
										return errors;
									}
									if (props.ContainsKey("Position"))
									{
										var isInt = int.TryParse(props["Position"], out position);
										if (!isInt)
										{
											//TODO fix line number / char pos
											//TODO better msg
											errors.Add(reader, $"expected position to be an integer but found {props["Position"]}");
										}
									}

									parser.Parse(out var expression, props["Expression"], context);

									stat.AddExpression(expression!, expressionName, position); //TODO AddOrUpdate
								}
								else
								{
									errors.Add(reader, "expected expression string or expression object");
									return errors; // continue on error
								}
							}

							_statService.Add(stat);
						}
					}

					continue;
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
