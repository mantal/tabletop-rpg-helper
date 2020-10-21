using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class VariableNode : ValueNode
	{
		public readonly VariableId Id;
		private readonly StatId? _parentId;
		private readonly StatService _statService;

		public VariableNode(StatService statService, string id, StatId? parentId)
			: base(id, NodeType.Stat, 1)
		{
			_statService = statService;
			Id = new VariableId(id, parentId);
			_parentId = parentId;
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> node)
		{
			var errors = base.IsValid(node);

			if (Id.StatId != _parentId && !_statService.Exists(Id))
				return errors.Append($"Undeclared variable id: {Id}");
			return errors;
		}

		public override double GetValue() => _statService.GetValue(Id);

		public override string ToString() => Id.ToString();
		
		//Prepend '.' because VariableId strip it from it's property Id but expected it when building
		public override Node Clone() => new VariableNode(_statService, "." + Id.Id, Id.StatId);
		public Node Clone(StatId? statId) => new VariableNode(_statService, "." + Id.Id, statId ?? Id.StatId);
	}
}