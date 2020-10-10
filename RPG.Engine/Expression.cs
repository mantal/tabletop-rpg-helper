using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Parser;

namespace RPG.Engine
{
	public class Expression
	{
		public static Expression Default { get; } = new Expression(new LinkedList<Node>(new [] { new NumberNode(0), }));

		public LinkedList<Node> Nodes { get; }

		public Expression(LinkedList<Node> nodes)
		{
			Nodes = nodes;
		}

		public Expression(Expression expression)
		{
			Nodes = new LinkedList<Node>(expression.Nodes.Select(n => n.Clone()));
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
		{
			var s = "";
			for (var node = Nodes.First; node != null; node = node.Next)
			{
				s += node.Value.ToString();
				if (node.Next != null
					&& !(node.Value is UnaryOperatorNode))
					s += ' ';
			}

			return s;
		}
	}

	public class NamedExpression : Expression
	{
		public string Name { get; }

		public NamedExpression(string name, Expression expression) 
			: base(expression.Nodes)
		{
			Name = name;
		}

		public NamedExpression(string name, LinkedList<Node> nodes) 
			: base(nodes)
		{
			Name = name;
		}

		public NamedExpression(NamedExpression expression)
			: base(expression)
		{
			Name = expression.Name;
		}

		public override string ToString() => $"[{Name}, {base.ToString()}]";
	}
}