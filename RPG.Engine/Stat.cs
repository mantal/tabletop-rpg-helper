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
		//TODO exposer un IEnumerable + garder IList en private pour rester immutable
		public IList<NamedExpression> Expressions { get; }

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
		private readonly VariableId _lastValueId;

		public Stat(StatId id, Expression expression)
			: this(id, new List<NamedExpression> { new NamedExpression("0", expression.Nodes) })
		{ }

		public Stat(StatId id, List<NamedExpression> expressions)
		{
			Id = id;
			Expressions = expressions;
			_lastValueId = new VariableId(":value", Id);
			Variables[_lastValueId] = 0;
			foreach (var node in Expressions.SelectMany(e => e.Nodes))
			{
				if (node is VariableNode variableNode
					&& variableNode.Id.StatId == id)
					AddOrUpdateVariable(variableNode.Id, 0);
			}
		}

		public Stat(Stat stat, StatId? id = null)
		{
			Id = id ?? new StatId(stat.Id);
			_lastValueId = new VariableId(":" + stat._lastValueId.Id, Id);
			Variables = stat.Variables
							.Select(var => (Id: new VariableId(":" + var.Key.Id, Id), Value: var.Value))
							.ToDictionary(v => v.Id, v => v.Value);
			Expressions = stat.Expressions.Select(e 
													  => new NamedExpression(e.Name,
																			 new LinkedList<Node>(e.Nodes.Select(node =>
																					 {
																						 if (node is VariableNode variableNode
																							 && variableNode.Id.StatId == stat.Id)
																							 return variableNode.Clone(Id);
																						 return node;
																					 })
																				 )))
							  .ToList();
		}

		public double Resolve()
		{
			Variables[_lastValueId] = 0;
			foreach (var expression in Expressions)
			{
				var result = expression.Resolve();
				if (UseLastValue(expression))
					Variables[_lastValueId] = result;
				else
					Variables[_lastValueId] = Variables[_lastValueId] + result;
			}

			return Variables[_lastValueId];
		}

		public bool Exists(string expressionName) => Expressions.Any(e => e.Name == expressionName);

		public IEnumerable<string> AddOrUpdateExpression(Expression expression, string name, int? position = null)
		{
			if (Exists(name))
				return UpdateExpression(expression, name);
			return AddExpression(expression, name, position ?? -1);
		}

		public IEnumerable<string> AddExpression(Expression expression, string name, int position = -1)
		{
			var exists = Expressions.Any(e => e.Name == name);
			if (exists)
				return new[] { $"Expression {Id}:{name} already exists" };

			position = position >= Expressions.Count ? Expressions.Count - 1 : position;
			position = position >= 0 ? position : Expressions.Count + position + 1;
			position = position <= 0 ? 0 : position;

			Expressions.Insert(position, new NamedExpression(name, expression.Nodes));

			AddVariables(expression);

			return Enumerable.Empty<string>();
		}

		public IEnumerable<string> UpdateExpression(Expression expression, string name)
		{
			var previousExpression = Expressions.FirstOrDefault(e => e.Name == name);
			if (previousExpression == null)
				return new[] { $"Expression {Id}:{name} doesn't exists" };

			var index = Expressions.IndexOf(previousExpression);
			Expressions[index] = new NamedExpression(name, expression.Nodes);

			AddVariables(expression);

			//TODO remove unused variables

			return Enumerable.Empty<string>();
		}

		public IEnumerable<string> RemoveExpression(string name)
		{
			var expression = Expressions.FirstOrDefault(e => e.Name == name);
			if (expression == null)
				return new[] { $"Expression doesn't {Id}:{name} exists" };

			Expressions.Remove(expression);
			
			//TODO remove unused variables

			return Enumerable.Empty<string>();
		}

		public double GetVariable(VariableId id)
		{
			if (id.StatId != Id)
				throw new ArgumentOutOfRangeException(nameof(id), id, "");

			var value = TryGetVariable(id);
			if (value == null)
				throw new ArgumentOutOfRangeException(nameof(id), id, $"No variable with id {id} were found in {Id}. Registered variables are: {{{Variables.Keys.Join()}}}");
			return (double) value;
		}

		public double? TryGetVariable(VariableId id)
		{
			if (!Variables.ContainsKey(id))
				return null;
			return Variables[id];
		}

		public void AddOrUpdateVariable(VariableId id, double value) => Variables[id] = value;
		
		//TODO
		public override string ToString() => Expressions.Select(e => e.ToString()).Join();

		private void AddVariables(Expression expression)
		{
			foreach (var node in expression.Nodes)
			{
				if (node is VariableNode variableNode
					&& variableNode.Id.StatId == Id
					&& !Variables.ContainsKey(variableNode.Id))
					AddOrUpdateVariable(variableNode.Id, 0);

			}
		}

		private bool UseLastValue(Expression expression) 
			=> expression.Nodes.Any(n => n is VariableNode varNode && varNode.Id == _lastValueId);
	}
}