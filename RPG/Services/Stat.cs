using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RPG.Services
{
	[DebuggerDisplay("{Id} = {ToString()}")]
	public class Stat
	{
		public readonly StatId Id;
		public IEnumerable<Modifier> Modifiers { get; set; } = new List<Modifier>();

		[JsonConverter(typeof(StringEnumConverter))]
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

		public readonly IDictionary<StatId, double> InnerStats = new Dictionary<StatId, double>();

		public Stat(StatId id)
		{
			Id = id;
		}

		public double GetInner(StatId innerId)
		{
			var value = TryGetInner(innerId);
			if (value == null)
				throw new ArgumentOutOfRangeException(nameof(innerId), innerId, $"No inner stat with id {innerId} were found in {Id}. Registered inner stats are: {{{InnerStats.Keys.Aggregate("", (s, id) => s + id)}}}");
			return (double) value;
		}

		public double? TryGetInner(StatId innerId)
		{
			if (!InnerStats.ContainsKey(innerId))
				return null;
			return InnerStats[innerId];
		}

		public void AddOrUpdateInner(StatId innerId, double value) => InnerStats[innerId] = value;

		public static IEnumerable<string> FromString(out Stat? stat, string id, string? rawModifiers = null)
		{
			var errors = new List<string>();
			
			stat = new Stat(id);
			if (string.IsNullOrWhiteSpace(rawModifiers))
				return errors;

			rawModifiers = rawModifiers.Replace("+", " + ", StringComparison.InvariantCultureIgnoreCase)
									   .Replace("-", " - ", StringComparison.InvariantCultureIgnoreCase)
									   .Replace("*", " * ", StringComparison.InvariantCultureIgnoreCase)
									   .Replace("/", " / ", StringComparison.InvariantCultureIgnoreCase)
									   .Trim()
				; // Cleanup

			// Add implicit +
			if (ModifierType.FromString(rawModifiers[0].ToString()) == null)
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
				var isId = operand.IsValidStatId();
				var isNumber = double.TryParse(operand, NumberStyles.Float, null, out var modValue);
				if (!isId && !isNumber)
				{
					errors.Add($"Expected a stat id or a number after {type} but found {operand}");
					return errors;
				}

				if (isId)
				{
					var refId = new StatId(operand.ExpandStatId(stat.Id));
					if (refId.Id == stat.Id && refId.InnerId != null)
						stat.InnerStats[refId.InnerId] = 0;

					stat.Modifiers = stat.Modifiers.Append(new StatModifier(type, refId));
				}
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				else if (isNumber)
					stat.Modifiers = stat.Modifiers.Append(new StaticModifier(type, modValue));
			}

			return errors;
		}

		public override string ToString() => Modifiers.Aggregate("", (res, m) => res + " " + m);
	}
}