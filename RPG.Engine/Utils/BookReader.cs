using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RPG.Engine.Utils
{
    public class BookReader
    {
		private readonly TextReader _reader;
		private int linePos = 0;
		private int lineNumber = 1;

		public BookReader(TextReader reader)
		{
			_reader = reader;
		}

		public Node Parse()
		{
			return new Node("#root")
			{
				Type = NodeType.ObjectIdentifier,
				Children = ParseObjectChildren(),
			};
		}

		private IEnumerable<Node> ParseObjectChildren()
		{
			var nodes = new List<Node>();

			while (_reader.Peek() != -1)
			{
				var id = ParseIdentifier();
				SkipWhitespaces();
				if (id.Type == NodeType.ObjectIdentifier)
					id.Children = ParseObjectChildren();
				else
					id.Children = new [] { ParseProperty() };

				nodes.Add(id);

				SkipWhitespaces();
				if ((char)_reader.Peek() == '}')
				{
					_reader.Read();
					break;
				}
			}

			return nodes;
		}

		private Node ParseProperty()
		{
			var node = new Node(ReadLine());

			if (int.TryParse(node.Value, NumberStyles.Integer, null, out _))
				node.Type = NodeType.Integer;
			else if (float.TryParse(node.Value, NumberStyles.Float, null, out _))
				node.Type = NodeType.Float;
			else
				node.Type = NodeType.String;

			return node;
		}

		private Node ParseIdentifier()
		{
			var id = "";

			for (int c;
				 (c = _reader.Peek()) != -1
				 && (char.IsLetterOrDigit((char) c)
						|| (char)c == '.'
						|| (char)c == '_'
						|| (char)c == '-'
						|| (char)c == '#'
						);) 
				id += (char) _reader.Read();

			if (id == "")
				throw new Exception("Missing identifier");

			SkipWhitespaces();

			var next = _reader.Peek();
			if (next == -1)
				throw new Exception("unexpected end of file"); // TODO better

			NodeType type;
			if (next == ':')
				type = NodeType.PropertyIdentifier;
			else if (next == '{')
			{
				type = NodeType.ObjectIdentifier;
				id = string.IsNullOrEmpty(id) ? "{" : id;
			}
			else
				throw new Exception($"expected property start ':' or object start '{{' but got: '{(char)next}'");
			_reader.Read();

			SkipWhitespaces();

			return new Node
			{
				Type = type,
				Value = id,
			};
		}

		private void SkipWhitespaces()
		{
			for (int c;
				 (c = _reader.Peek()) != -1
				 && char.IsWhiteSpace((char) c);)
				_reader.Read();
		}

		private string ReadLine()
		{
			//TODO keep track of position
			var line = "";
			
			for (int c; (c = _reader.Read()) != -1 && c != '\n';)
				line += (char)c;

			return line;
		}

		[DebuggerDisplay("({Type}) {Value}")]
		public class Node
		{
			public NodeType Type { get; set; }
			public string Value { get; set; }
			public IEnumerable<Node> Children { get; set; } = Enumerable.Empty<Node>();

			public Node(string value)
			{
				Value = value;
			}

			public Node() {}
		}

		public enum NodeType
		{
			PropertyIdentifier,
			ObjectIdentifier,
			String,
			Integer,
			Float,
		}
	}
}
