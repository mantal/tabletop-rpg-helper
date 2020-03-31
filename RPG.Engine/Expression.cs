using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Parser;

namespace RPG.Engine
{
    public class Expression
	{
		public LinkedList<Node> Nodes { get; }

		public Expression(LinkedList<Node> nodes)
		{
			Nodes = nodes;
		}

		public double Resolve()
		{
			var nodes = new LinkedList<Node>(Nodes);

			var priority = Node.MaxPriority;
			while (nodes.Count > 1)
			{
				var node = nodes.First;
				while (node != null && node.Value.Priority < priority)
					node = node.Next;
				if (node?.Next == null)
				{
					priority--;
					if (priority < Node.MinPriority)
						break;
					continue;
				}

				node.Value.Apply(node);
			}

			nodes.First.Value.Apply(nodes.First);

			return ((ValueNode)nodes.First.Value).GetValue();
		}

		public override string ToString()
			=> string.Join(' ', Nodes.Select(e => e.ToString()));
	}
}
