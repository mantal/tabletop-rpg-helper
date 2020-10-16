using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RPG.Engine.Utils
{
    public class BookParser
    {
		private readonly TextReader _reader;
		private int _linePosition = 0;
		private int _lineNumber = 1;

		public BookParser(TextReader reader)
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
					Read();
					break;
				}
			}

			return nodes;
		}

		private Node ParseProperty()
		{
			var property = "";

			var line = _lineNumber;
			var position = _linePosition;

			var parenthesisCount = 0;
			var bracketCount = 0;
			for (int c;
				 (c = _reader.Peek()) != -1;)
			{
				if (c == '}' && bracketCount == 0)
					break;

				Read();

				if (c == '(')
					parenthesisCount++;
				else if (c == ')')
					parenthesisCount--;
				else if (c == '{')
					bracketCount++;
				else if (c == '}')
					bracketCount--;
				else if (parenthesisCount == 0
						 && bracketCount == 0
						 && c == '\n')
					break;
				property += (char) c;
			}

			var node = new Node(property)
			{
				LineNumber = line,
				LinePosition = position,
			};

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
			var line = _lineNumber;
			var position = _linePosition;

			for (int c;
				 (c = _reader.Peek()) != -1
				 && (char.IsLetterOrDigit((char) c)
						|| (char)c == '.'
						|| (char)c == '_'
						|| (char)c == '-'
						|| (char)c == '#'
						|| (char)c == '$'
					);) 
				id += (char) Read();

			if (id == "")
				throw BuildException("Missing identifier");

			SkipWhitespaces();

			var next = _reader.Peek();
			if (next == -1)
				throw BuildException("unexpected end of file"); // TODO better

			NodeType type;
			if (next == ':')
				type = NodeType.PropertyIdentifier;
			else if (next == '{')
			{
				type = NodeType.ObjectIdentifier;
				id = string.IsNullOrEmpty(id) ? "{" : id;
			}
			else
				throw BuildException($"expected property start ':' or object start '{{' but got: '{(char)next}'");
			Read();

			SkipWhitespaces();

			return new Node(id)
			{
				Type = type,
				LineNumber = line,
				LinePosition = position,
			};
		}

		private void SkipWhitespaces()
		{
			for (int c;
				 (c = _reader.Peek()) != -1
				 && char.IsWhiteSpace((char) c);)
				Read();
		}

		private int Read()
		{
			var c = _reader.Read();

			if (c == '\n')
			{
				_linePosition = 0;
				_lineNumber++;
			}
			else
				_linePosition++;

			return c;
		}

		private BookFormatException BuildException(string message) 
			=> new BookFormatException(_lineNumber, _linePosition, message);

		[DebuggerDisplay("({Type}) {Value}")]
		public class Node
		{
			public int LineNumber { get; set; }
			public int LinePosition { get; set; }
			public NodeType Type { get; set; }
			public string Value { get; }
			public IEnumerable<Node> Children { get; set; } = Enumerable.Empty<Node>();

			public Node(string value)
			{
				Value = value;
			}
			
			public override string ToString()
				=> Type switch
				   {
					   NodeType.ObjectIdentifier   => $"{Value} {{\n{string.Join('\n', Children.Select(c => c.ToString()))}\n}}",
					   NodeType.PropertyIdentifier => $"{Value}: {Children.First()}",
					   NodeType.Float              => Value,
					   NodeType.Integer            => Value,
					   NodeType.String             => Value,
					   _                           => throw new ArgumentOutOfRangeException(nameof(Type)),
				   };
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

	public class BookFormatException : Exception
	{
		public int LineNumber { get; }
		public int LinePosition { get; }

		public BookFormatException(int lineNumber, int linePosition, string message)
			: base($"error at line{lineNumber}:{linePosition}: {message}")
		{
			LineNumber = lineNumber;
			LinePosition = linePosition;
		}
	}
}
