using System.IO;
using System.Linq;
using FluentAssertions;
using RPG.Engine.Utils;
using Xunit;

namespace RPG.Tests
{
    public class BookReaderTests
    {
		[Fact]
		public void ParseProperty()
		{
			var node = new BookParser(new StringReader("expr: value")).Parse();

			var expected = new BookParser.Node
			{
				Type = BookParser.NodeType.ObjectIdentifier,
				Value = "#root",
				Children = new []
				{
					new BookParser.Node
					{
						Type = BookParser.NodeType.PropertyIdentifier,
						Value = "expr",
						Children = new []
						{
							new BookParser.Node
							{
								Type = BookParser.NodeType.String,
								Value = "value",
								Children = Enumerable.Empty<BookParser.Node>(),
							},
						},
					},
				},
			};

			node.Should().BeEquivalentTo(expected);
		}

		[Fact]
		public void ParseProperties()
		{
			var node = new BookParser(new StringReader("expr: value\n expr2: value2")).Parse();

			var expected = new BookParser.Node
			{
				Type = BookParser.NodeType.ObjectIdentifier,
				Value = "#root",
				Children = new[]
				{
					new BookParser.Node
					{
						Type = BookParser.NodeType.PropertyIdentifier,
						Value = "expr",
						Children = new []
						{
							new BookParser.Node
							{
								Type = BookParser.NodeType.String,
								Value = "value",
								Children = Enumerable.Empty<BookParser.Node>(),
							},
						},
					},
					new BookParser.Node
					{
						Type = BookParser.NodeType.PropertyIdentifier,
						Value = "expr2",
						Children = new []
						{
							new BookParser.Node
							{
								Type = BookParser.NodeType.String,
								Value = "value2",
								Children = Enumerable.Empty<BookParser.Node>(),
							},
						},
					},
				},
			};

			node.Should().BeEquivalentTo(expected);
		}

		[Fact]
		public void ParseObject()
		{
			var node = new BookParser(new StringReader("obj { expr: value\n expr2: value2\n }")).Parse();

			var expected = new BookParser.Node
			{
				Type = BookParser.NodeType.ObjectIdentifier,
				Value = "#root",
				Children = new[]
				{
					new BookParser.Node
					{
						Type = BookParser.NodeType.ObjectIdentifier,
						Value = "obj",
						Children = new []
						{
							new BookParser.Node
							{
								Type = BookParser.NodeType.PropertyIdentifier,
								Value = "expr",
								Children = new []
								{
									new BookParser.Node
									{
										Type = BookParser.NodeType.String,
										Value = "value",
										Children = Enumerable.Empty<BookParser.Node>(),
									},
								},
							},
												new BookParser.Node
					{
						Type = BookParser.NodeType.PropertyIdentifier,
						Value = "expr2",
						Children = new []
						{
							new BookParser.Node
							{
								Type = BookParser.NodeType.String,
								Value = "value2",
								Children = Enumerable.Empty<BookParser.Node>(),
							},
						},
					},
						},
					},
				},
			};

			node.Should().BeEquivalentTo(expected);
		}
	}
}
