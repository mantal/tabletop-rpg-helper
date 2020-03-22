using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Modifiers;
using RPG.Engine.Utils;

namespace RPG.Engine
{
	[DebuggerDisplay("{Id} = {ToString()}")]
	public class Stat
	{
		public readonly StatId Id;
		public IEnumerable<Modifier> Modifiers { get; set; } = new List<Modifier>();

		public RoundingMethod RoundingMethod
		{
			get => _roundingMethod;
			set
			{
				if (value == RoundingMethod.None) 
					throw new ArgumentOutOfRangeException(nameof(RoundingMethod), value, "Stat must be converted to int");
				_roundingMethod = value;
			}
		}

		private RoundingMethod _roundingMethod = RoundingMethod.Ceiling;

		public readonly IDictionary<VariableId, double> Variables = new Dictionary<VariableId, double>();

		public Stat(StatId id)
		{
			Id = id;
		}

		public double GetVariable(VariableId id)
		{
			if (id.StatId != Id)
				throw new ArgumentOutOfRangeException(nameof(id), id, "");

			var value = TryGetVariable(id);
			if (value == null)
				throw new ArgumentOutOfRangeException(nameof(id), id, $"No variable with id {id} were found in {Id}. Registered inner stats are: {{{Variables.Keys.AsString()}}}");
			return (double) value;
		}

		public double? TryGetVariable(VariableId id)
		{
			if (!Variables.ContainsKey(id))
				return null;
			return Variables[id];
		}

		public void AddOrUpdateVariable(VariableId id, double value) => Variables[id] = value;

		public static IEnumerable<string> FromString(out Stat? stat, string id, string? rawModifiers = null)
		{
			var errors = new List<string>();
			
			stat = new Stat((StatId) id);
			if (string.IsNullOrWhiteSpace(rawModifiers))
				return errors;

			rawModifiers = rawModifiers.Replace("+", " + ", StringComparison.InvariantCultureIgnoreCase)
									   .Replace("-", " - ", StringComparison.InvariantCultureIgnoreCase)
									   .Replace("*", " * ", StringComparison.InvariantCultureIgnoreCase)
									   .Replace("/", " / ", StringComparison.InvariantCultureIgnoreCase)
									   .Trim()
				; // Cleanup

			// Add implicit +
			if (ModifierType.FromString(rawModifiers[0].ToString(CultureInfo.InvariantCulture)) == null)
				rawModifiers = "+ " + rawModifiers;

			var tokens = rawModifiers.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

			for (var i = 0; i < tokens.Length; i++)
			{
				var type = ModifierType.FromString(tokens[i]);
				if (type == null)
				{
					errors.Add($"Unknown operator: {tokens[i]}, expected one of: '+', '-', '*', '/'");
					return errors;
				}
				if (i + 1 >= tokens.Length)
				{
					errors.Add($"Missing identifier after {tokens[i]}");
					return errors;
				}

				i++;
				var operand = tokens[i];

				var operandType = GetModifierType(operand);
				if (operandType == typeof(StatModifier))
				{
					var statId = new StatId(operand);
					stat.Modifiers = stat.Modifiers.Append(new StatModifier(type, statId));
				}
				else if (operandType == typeof(VariableModifier))
				{
					var variableId = new VariableId(operand, stat.Id);
					stat.Variables[variableId] = 0;
					stat.Modifiers = stat.Modifiers.Append(new VariableModifier(type, variableId));
				}
				else if (operandType == typeof(StaticModifier))
				{
					var isNumber = double.TryParse(operand, NumberStyles.Float, null, out var modValue);
					if (!isNumber)
					{
						errors.Add($"Expected a stat id or a number after {type} but found {operand}");
						return errors;
					}

					stat.Modifiers = stat.Modifiers.Append(new StaticModifier(type, modValue));
				}
			}

			return errors;
		}

		public override string ToString()
		{
			var s = Modifiers.Aggregate("", (res, m) => res + " " + m);
			if (s.Length > 1 && s[1] == '+')
				s = s.Substring(2);
			return s.Trim();
		}

		//TODO proper parser
		private static Type? GetModifierType(string s)
		{
			if (s.IsValidStatId()) return typeof(StatModifier);
			if (s.IsValidVariableId()) return typeof(VariableModifier);
			if (double.TryParse(s, NumberStyles.Float, null, out _)) return typeof(StaticModifier);
			return null;
		}
	}
}