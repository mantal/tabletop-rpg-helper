using System;
using System.Collections.Generic;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class FunctionNode : ValueNode
	{
		//todo quand impl: update une fonction ne peut pas changer son numbre d'args
		public FunctionNode(StatService statService, string text) 
			: base(statService, NodeType.Function)
		{ }

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
		{
			throw new NotImplementedException();
		}

		public override LinkedListNode<Node> Apply(LinkedListNode<Node> node)
		{
			throw new NotImplementedException();
		}

		public override double GetValue() { throw new NotImplementedException(); }

		public override string ToString() => "non_func";
	}
}