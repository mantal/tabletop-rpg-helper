using System.IO;
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

			node.ToString().Should().BeEquivalentTo("#root {\nexpr: value\n}");
		}

		[Fact]
		public void ParseProperties()
		{
			var node = new BookParser(new StringReader("expr: value\n expr2: value2")).Parse();

			node.ToString().Should().BeEquivalentTo("#root {\nexpr: value\nexpr2: value2\n}");
		}

		[Fact]
		public void ParseObject()
		{
			var node = new BookParser(new StringReader("obj { expr: value\n expr2: value2\n }")).Parse();

			node.ToString().Should().BeEquivalentTo("#root {\nobj {\nexpr: value\nexpr2: value2\n}\n}");
		}
	}
}