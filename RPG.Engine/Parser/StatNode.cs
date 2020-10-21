using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class StatNode : ValueNode
	{
		public readonly StatId Id;
		private readonly StatService _statService;

		public StatNode(StatService statService, string id)
			: base(id, NodeType.Variable, 1)
		{
			_statService = statService;
			Id = new StatId(id);
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> node)
		{
			var errors = base.IsValid(node);

			if (!_statService.Exists(Id))
				return errors.Append($"Undeclared stat id: {Id}");
			return errors;
		}

		public override double GetValue() => _statService.GetValue(Id);

		public override string ToString() => Id.ToString();

		public override Node Clone() => new StatNode(_statService, Id.Id);
	}
}