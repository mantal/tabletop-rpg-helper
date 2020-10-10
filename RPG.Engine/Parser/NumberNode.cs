using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RPG.Engine.Parser
{
	public class NumberNode : ValueNode
	{
		private readonly double _value;

		public NumberNode(string text) 
			: base(text, NodeType.Number, -1)
		{
			_value = double.Parse(text, NumberStyles.Float, null);
		}

		public NumberNode(double value) 
			: base(value.ToString(CultureInfo.InvariantCulture), NodeType.Number, -1)
		{
			_value = value;
		}

		public override IEnumerable<string> IsValid(LinkedListNode<Node> node)
			=> Enumerable.Empty<string>();

		public override double GetValue() => _value;

		public override string ToString() => _value.ToString(CultureInfo.InvariantCulture);

		public override Node Clone() => new NumberNode(_value);
	}
}