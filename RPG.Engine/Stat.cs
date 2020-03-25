using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Modifiers;
using RPG.Engine.Parser;
using RPG.Engine.Utils;

namespace RPG.Engine
{
	[DebuggerDisplay("{Id} = {ToString()}")]
	public class Stat
	{
		public readonly StatId Id;
		public LinkedList<Node> Expression { get; }

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

		public Stat(StatId id, LinkedList<Node> expression)
		{
			Id = id;
			Expression = expression;
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
		
		public override string ToString() 
			=> string.Join(' ', Expression.Select(e => e.ToString()));
	}
}