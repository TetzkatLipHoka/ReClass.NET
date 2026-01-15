using System;
using NFluent;
using ReClassNET.AddressParser;
using Xunit;

namespace ReClass.NET_Tests.AddressParser
{
	class TestClassList : IProjectContext
	{
		public IReadOnlyList<ReClassNET.Nodes.ClassNode> Classes { get; } = new List<ReClassNET.Nodes.ClassNode>{};
	}
  
	public class ParserTest
	{
		[Theory]
		[InlineData("-")]
		[InlineData("+")]
		[InlineData("*")]
		[InlineData("/")]
		[InlineData(",")]
		[InlineData("(")]
		[InlineData(")")]
		[InlineData("[")]
		[InlineData("]")]
		[InlineData("{")]
		[InlineData("}")]
		[InlineData("1-")]
		[InlineData("1(")]
		[InlineData("1)")]
		[InlineData("1[")]
		[InlineData("1]")]
		[InlineData("(1")]
		[InlineData(")1")]
		[InlineData("[1")]
		[InlineData("]1")]
		[InlineData("{1")]
		[InlineData("}1")]
		[InlineData("1+(")]
		[InlineData("1+)")]
		[InlineData("1 + ()")]
		[InlineData("(1 + 2")]
		[InlineData("1 + 2)")]
		[InlineData("[1 + 2)")]
		[InlineData("(1 + 2]")]
		[InlineData("[1,")]
		[InlineData("[1,]")]
		[InlineData("[1,2]")]
		[InlineData("1,")]
		[InlineData("1,2")]
		public void InvalidExpressionTests(string expression)
		{
			Check.ThatCode(() => Parser.Parse(expression)).Throws<ParseException>();
		}

		[Theory]
		[InlineData("1", typeof(ConstantExpression))]
		[InlineData("1 + 2", typeof(AddExpression))]
		[InlineData("1 - 2", typeof(SubtractExpression))]
		[InlineData("1 * 2", typeof(MultiplyExpression))]
		[InlineData("1 / 2", typeof(DivideExpression))]
		[InlineData("1 + 2 * 3", typeof(AddExpression))]
		[InlineData("(1 + 2) * 3", typeof(MultiplyExpression))]
		[InlineData("1 + (2 * 3)", typeof(AddExpression))]
		[InlineData("(1 + (2 * 3))", typeof(AddExpression))]
		[InlineData("[1]", typeof(ReadMemoryExpression))]
		[InlineData("[1,4]", typeof(ReadMemoryExpression))]
		[InlineData("[1,8]", typeof(ReadMemoryExpression))] 
		[InlineData("<test.exe>", typeof(ModuleExpression))] // Module
		[InlineData("[<test.exe>]", typeof(ReadMemoryExpression))] // Read memory at address of module
		[InlineData("{ClassName}", typeof(TypeExpression))] // Address of another class
		public void ValidExpressionTests(string expression, Type type)
		{
			DynamicCompiler.projectContext = new TestClassList();
			Check.That(Parser.Parse(expression)).IsInstanceOfType(type);
		}

		[Fact]
		public void ReadMemoryDefaultByteCountCheck()
		{
			var expression = (ReadMemoryExpression)Parser.Parse("[1]");

			Check.That(expression.ByteCount).IsEqualTo(IntPtr.Size);
		}
	}
}
