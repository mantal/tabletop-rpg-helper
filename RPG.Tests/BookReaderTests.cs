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
			var node = new BookReader(new StringReader("expr: value")).Parse();

			var expected = new BookReader.Node
			{
				Type = BookReader.NodeType.ObjectIdentifier,
				Value = "#root",
				Children = new []
				{
					new BookReader.Node
					{
						Type = BookReader.NodeType.PropertyIdentifier,
						Value = "expr",
						Children = new []
						{
							new BookReader.Node
							{
								Type = BookReader.NodeType.String,
								Value = "value",
								Children = Enumerable.Empty<BookReader.Node>(),
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
			var node = new BookReader(new StringReader("expr: value\n expr2: value2")).Parse();

			var expected = new BookReader.Node
			{
				Type = BookReader.NodeType.ObjectIdentifier,
				Value = "#root",
				Children = new[]
				{
					new BookReader.Node
					{
						Type = BookReader.NodeType.PropertyIdentifier,
						Value = "expr",
						Children = new []
						{
							new BookReader.Node
							{
								Type = BookReader.NodeType.String,
								Value = "value",
								Children = Enumerable.Empty<BookReader.Node>(),
							},
						},
					},
					new BookReader.Node
					{
						Type = BookReader.NodeType.PropertyIdentifier,
						Value = "expr2",
						Children = new []
						{
							new BookReader.Node
							{
								Type = BookReader.NodeType.String,
								Value = "value2",
								Children = Enumerable.Empty<BookReader.Node>(),
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
			var node = new BookReader(new StringReader("obj { expr: value\n expr2: value2\n }")).Parse();

			var expected = new BookReader.Node
			{
				Type = BookReader.NodeType.ObjectIdentifier,
				Value = "#root",
				Children = new[]
				{
					new BookReader.Node
					{
						Type = BookReader.NodeType.ObjectIdentifier,
						Value = "obj",
						Children = new []
						{
							new BookReader.Node
							{
								Type = BookReader.NodeType.PropertyIdentifier,
								Value = "expr",
								Children = new []
								{
									new BookReader.Node
									{
										Type = BookReader.NodeType.String,
										Value = "value",
										Children = Enumerable.Empty<BookReader.Node>(),
									},
								},
							},
												new BookReader.Node
					{
						Type = BookReader.NodeType.PropertyIdentifier,
						Value = "expr2",
						Children = new []
						{
							new BookReader.Node
							{
								Type = BookReader.NodeType.String,
								Value = "value2",
								Children = Enumerable.Empty<BookReader.Node>(),
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
