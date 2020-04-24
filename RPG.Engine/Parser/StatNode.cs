using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class StatNode : ValueNode
	{
		public readonly StatId Id;

		public StatNode(StatService statService, string id)
			: base(statService, NodeType.Variable, 0)
		{
			Id = new StatId(id);
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			if (!context.StatService.Exists(Id))
				return new[] { $"Undeclared stat id: {Id}" };
			return Enumerable.Empty<string>();
		}

		public override double GetValue() => StatService.GetValue(Id);

		public override string ToString() => Id.ToString();

		public override Node Clone() => new StatNode(StatService, Id.Id);
	}
}