using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class VariableNode : ValueNode
	{
		public readonly VariableId Id;

		public VariableNode(StatService statService, string id, StatId parentId)
			: base(statService, NodeType.Stat, 0)
		{
			Id = new VariableId(id, parentId);
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			if (Id.StatId != context.StatId && !context.StatService.Exists(Id))
				return new[] { $"Undeclared variable id: {Id}" };
			return Enumerable.Empty<string>();
		}

		public override double GetValue() => StatService.GetValue(Id);

		public override string ToString() => Id.ToString();
		
		//Prepend '.' because VariableId strip it from it's property Id but expected it when building
		public override Node Clone() => new VariableNode(StatService, "." + Id.Id, Id.StatId);
		public Node Clone(StatId statId) => new VariableNode(StatService, "." + Id.Id, statId ?? Id.StatId);
	}
}