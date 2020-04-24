using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPG.Engine.Services;

namespace RPG.Engine.Parser
{
	public class NumberNode : ValueNode
	{
		private readonly double _value;

		public NumberNode(StatService statService, string token) 
			: base(statService, NodeType.Number, -1)
		{
			_value = double.Parse(token, NumberStyles.Float, null);
		}

		public NumberNode(StatService statService, double value) 
			: base(statService, NodeType.Number, -1)
		{
			_value = value;
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> token, ParsingContext context)
			=> Enumerable.Empty<string>();

		public override double GetValue() => _value;

		public override string ToString() => _value.ToString(CultureInfo.InvariantCulture);

		public override Node Clone() => new NumberNode(StatService, _value);
	}
}