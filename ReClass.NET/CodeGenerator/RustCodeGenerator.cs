using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using ReClassNET.Controls;
using ReClassNET.Extensions;
using ReClassNET.Logger;
using ReClassNET.Nodes;
using ReClassNET.Project;

namespace ReClassNET.CodeGenerator
{
	public class RustCodeGenerator : ICodeGenerator
	{
		private readonly Dictionary<Type, string> nodeTypeToTypeDefinationMap;
		private readonly Dictionary<ClassNode, string> classNameMap = new Dictionary<ClassNode, string>();
		private bool usesCVoid;
		private bool usesUtf8StrPtr;
		private bool usesUtf16StrPtr;
		private bool usesUtf32StrPtr;

		#region HelperNodes

		private class Utf8CharacterNode : BaseNode
		{
			public override int MemorySize => throw new NotImplementedException();
			public override void GetUserInterfaceInfo(out string name, out Image icon) => throw new NotImplementedException();
			public override Size Draw(DrawContext context, int x, int y) => throw new NotImplementedException();
			public override int CalculateDrawnHeight(DrawContext context) => throw new NotImplementedException();
		}

		private class Utf16CharacterNode : BaseNode
		{
			public override int MemorySize => throw new NotImplementedException();
			public override void GetUserInterfaceInfo(out string name, out Image icon) => throw new NotImplementedException();
			public override Size Draw(DrawContext context, int x, int y) => throw new NotImplementedException();
			public override int CalculateDrawnHeight(DrawContext context) => throw new NotImplementedException();
		}

		private class Utf32CharacterNode : BaseNode
		{
			public override int MemorySize => throw new NotImplementedException();
			public override void GetUserInterfaceInfo(out string name, out Image icon) => throw new NotImplementedException();
			public override Size Draw(DrawContext context, int x, int y) => throw new NotImplementedException();
			public override int CalculateDrawnHeight(DrawContext context) => throw new NotImplementedException();
		}

		#endregion

		public Language Language => Language.Rust;

		public RustCodeGenerator()
		{
			nodeTypeToTypeDefinationMap = new Dictionary<Type, string>
			{
				[typeof(BoolNode)] = "bool",
				[typeof(DoubleNode)] = "f64",
				[typeof(FloatNode)] = "f32",
				[typeof(FunctionPtrNode)] = "*mut c_void",
				[typeof(Int8Node)] = "i8",
				[typeof(Int16Node)] = "i16",
				[typeof(Int32Node)] = "i32",
				[typeof(Int64Node)] = "i64",
				[typeof(NIntNode)] = "isize",
				[typeof(Matrix3x3Node)] = "[f32; 9]",
				[typeof(Matrix3x4Node)] = "[f32; 12]",
				[typeof(Matrix4x4Node)] = "[f32; 16]",
				[typeof(UInt8Node)] = "u8",
				[typeof(UInt16Node)] = "u16",
				[typeof(UInt32Node)] = "u32",
				[typeof(UInt64Node)] = "u64",
				[typeof(NUIntNode)] = "usize",
				[typeof(Utf8CharacterNode)] = "i8",
				[typeof(Utf16CharacterNode)] = "u16",
				[typeof(Utf32CharacterNode)] = "u32",
				[typeof(Vector2Node)] = "[f32; 2]",
				[typeof(Vector3Node)] = "[f32; 3]",
				[typeof(Vector4Node)] = "[f32; 4]"
			};
		}

		public string GenerateCode(IReadOnlyList<ClassNode> classes, IReadOnlyList<EnumDescription> enums, ILogger logger)
		{
			usesCVoid = false;
			usesUtf8StrPtr = false;
			usesUtf16StrPtr = false;
			usesUtf32StrPtr = false;
			using var bodySw = new StringWriter();
			using var bodyIw = new IndentedTextWriter(bodySw, "\t");

			using (var en = enums.GetEnumerator())
			{
				if (en.MoveNext())
				{
					WriteEnum(bodyIw, en.Current);

					while (en.MoveNext())
					{
						bodyIw.WriteLine();
						WriteEnum(bodyIw, en.Current);
					}

					bodyIw.WriteLine();
				}
			}

			var alreadySeen = new HashSet<ClassNode>();

			IEnumerable<ClassNode> GetReversedClassHierarchy(ClassNode node)
			{
				Contract.Requires(node != null);
				Contract.Ensures(Contract.Result<IEnumerable<ClassNode>>() != null);

				if (!alreadySeen.Add(node))
				{
					return Enumerable.Empty<ClassNode>();
				}

				var classNodes = node.Nodes
					.OfType<BaseContainerNode>()
					.SelectMany(c => c.Nodes)
					.Concat(node.Nodes)
					.OfType<BaseWrapperNode>()
					.Where(w => !w.IsNodePresentInChain<PointerNode>())
					.Select(w => w.ResolveMostInnerNode() as ClassNode)
					.Where(n => n != null);

				return classNodes
					.SelectMany(GetReversedClassHierarchy)
					.Append(node);
			}

			var classesToWrite = classes
				.Where(c => c.Nodes.None(n => n is FunctionNode))
				.SelectMany(GetReversedClassHierarchy)
				.Distinct();

			BuildClassNameMap(classes);

			using (var en = classesToWrite.GetEnumerator())
			{
				if (en.MoveNext())
				{
					WriteClass(bodyIw, en.Current, logger);

					while (en.MoveNext())
					{
						bodyIw.WriteLine();
						WriteClass(bodyIw, en.Current, logger);
					}
				}
			}

			using var sw = new StringWriter();
			using var iw = new IndentedTextWriter(sw, "\t");

			iw.WriteLine($"// Created with {Constants.ApplicationName} {Constants.ApplicationVersion} by {Constants.Author}");
			iw.WriteLine();
			iw.WriteLine("#![allow(non_snake_case)]");
			iw.WriteLine("#![allow(non_camel_case_types)]");
			iw.WriteLine("#![allow(dead_code)]");
			iw.WriteLine();
			if (usesCVoid)
			{
				iw.WriteLine("use std::ffi::c_void;");
				iw.WriteLine();
			}
			if (usesUtf8StrPtr)
			{
				iw.WriteLine("pub type Utf8StrPtr = *mut i8;");
			}
			if (usesUtf16StrPtr)
			{
				iw.WriteLine("pub type Utf16StrPtr = *mut u16;");
			}
			if (usesUtf32StrPtr)
			{
				iw.WriteLine("pub type Utf32StrPtr = *mut u32;");
			}
			if (usesUtf8StrPtr || usesUtf16StrPtr || usesUtf32StrPtr)
			{
				iw.WriteLine();
			}
			iw.Write(bodySw.ToString());

			return sw.ToString();
		}

		private void WriteEnum(IndentedTextWriter writer, EnumDescription @enum)
		{
			Contract.Requires(writer != null);
			Contract.Requires(@enum != null);

			writer.Write("#[repr(");
			switch (@enum.Size)
			{
				case EnumDescription.UnderlyingTypeSize.OneByte:
					writer.Write("i8");
					break;
				case EnumDescription.UnderlyingTypeSize.TwoBytes:
					writer.Write("i16");
					break;
				case EnumDescription.UnderlyingTypeSize.FourBytes:
					writer.Write("i32");
					break;
				case EnumDescription.UnderlyingTypeSize.EightBytes:
					writer.Write("i64");
					break;
			}
			writer.WriteLine(")]");
			writer.WriteLine($"pub enum {@enum.Name}");
			writer.WriteLine("{");
			writer.Indent++;
			for (var j = 0; j < @enum.Values.Count; ++j)
			{
				var kv = @enum.Values[j];
				writer.Write(kv.Key);
				writer.Write(" = ");
				writer.Write(kv.Value);
				writer.WriteLine(",");
			}
			writer.Indent--;
			writer.WriteLine("}");
		}

		private void WriteClass(IndentedTextWriter writer, ClassNode @class, ILogger logger)
		{
			Contract.Requires(writer != null);
			Contract.Requires(@class != null);
			Contract.Requires(logger != null);

			writer.WriteLine("#[repr(C, packed)]");
			writer.Write("pub struct ");
			writer.Write(GetClassName(@class));
			if (!string.IsNullOrEmpty(@class.Comment))
			{
				writer.Write(" // ");
				writer.Write(@class.Comment);
			}
			writer.WriteLine();
			writer.WriteLine("{");
			writer.Indent++;

			var nodes = @class.Nodes.WhereNot(n => n is FunctionNode || n is VirtualMethodTableNode);
			WriteNodes(writer, nodes, logger);

			writer.Indent--;
			writer.Write("} // Size: 0x");
			writer.WriteLine($"{@class.MemorySize:X04}");
		}

		private void WriteNodes(IndentedTextWriter writer, IEnumerable<BaseNode> nodes, ILogger logger)
		{
			Contract.Requires(writer != null);
			Contract.Requires(nodes != null);

			var fill = 0;
			var fillStart = 0;

			static BaseNode CreatePaddingMember(int offset, int count)
			{
				var node = new ArrayNode
				{
					Offset = offset,
					Count = count,
					Name = $"pad_{offset:X04}"
				};

				node.ChangeInnerNode(new Utf8CharacterNode());
				return node;
			}

			foreach (var member in nodes)
			{
				if (member is BaseHexNode)
				{
					if (fill == 0)
					{
						fillStart = member.Offset;
					}
					fill += member.MemorySize;
					continue;
				}

				if (fill != 0)
				{
					WriteNode(writer, CreatePaddingMember(fillStart, fill), logger);
					fill = 0;
				}

				WriteNode(writer, member, logger);
			}

			if (fill != 0)
			{
				WriteNode(writer, CreatePaddingMember(fillStart, fill), logger);
			}
		}

		private void WriteNode(IndentedTextWriter writer, BaseNode node, ILogger logger)
		{
			Contract.Requires(writer != null);
			Contract.Requires(node != null);

			var aliasType = GetStringPointerAlias(node);
			if (aliasType != null)
			{
				if (!node.Name.StartsWith("pad_", StringComparison.Ordinal))
				{
					writer.Write("pub ");
				}
				writer.Write(node.Name);
				writer.Write(": ");
				writer.Write(aliasType);
				writer.Write(", // 0x");
				writer.Write($"{node.Offset:X04}");
				if (!string.IsNullOrEmpty(node.Comment))
				{
					writer.Write(" ");
					writer.Write(node.Comment);
				}
				writer.WriteLine();
				return;
			}

			node = TransformNode(node);

			var typeName = GetTypeDefinition(node);
			if (typeName != null)
			{
				if (typeName.Contains("c_void"))
				{
					usesCVoid = true;
				}

				if (!node.Name.StartsWith("pad_", StringComparison.Ordinal))
				{
					writer.Write("pub ");
				}
				writer.Write(node.Name);
				writer.Write(": ");
				writer.Write(typeName);
				writer.Write(", // 0x");
				writer.Write($"{node.Offset:X04}");
				if (!string.IsNullOrEmpty(node.Comment))
				{
					writer.Write(" ");
					writer.Write(node.Comment);
				}
				writer.WriteLine();
			}
			else if (node is BaseWrapperNode)
			{
				var wrappedType = ResolveWrappedType(node);
				if (wrappedType.Contains("c_void"))
				{
					usesCVoid = true;
				}

				if (!node.Name.StartsWith("pad_", StringComparison.Ordinal))
				{
					writer.Write("pub ");
				}
				writer.Write(node.Name);
				writer.Write(": ");
				writer.Write(wrappedType);
				writer.Write(", // 0x");
				writer.Write($"{node.Offset:X04}");
				if (!string.IsNullOrEmpty(node.Comment))
				{
					writer.Write(" ");
					writer.Write(node.Comment);
				}
				writer.WriteLine();
			}
			else if (node is UnionNode unionNode)
			{
				// Rust can't inline anonymous unions inside a struct field declaration.
				if (!unionNode.Name.StartsWith("pad_", StringComparison.Ordinal))
				{
					writer.Write("pub ");
				}
				writer.Write(unionNode.Name);
				writer.Write(": [u8; ");
				writer.Write(unionNode.MemorySize);
				writer.Write("], // 0x");
				writer.Write($"{unionNode.Offset:X04}");
				if (!string.IsNullOrEmpty(unionNode.Comment))
				{
					writer.Write(" ");
					writer.Write(unionNode.Comment);
				}
				writer.WriteLine();
			}
			else
			{
				logger.Log(LogLevel.Error, $"Skipping node with unhandled type: {node.GetType()}");
			}
		}

		private static BaseNode TransformNode(BaseNode node)
		{
			static BaseNode GetCharacterNodeForEncoding(Encoding encoding)
			{
				if (encoding.IsSameCodePage(Encoding.Unicode))
				{
					return new Utf16CharacterNode();
				}
				if (encoding.IsSameCodePage(Encoding.UTF32))
				{
					return new Utf32CharacterNode();
				}
				return new Utf8CharacterNode();
			}

			switch (node)
			{
				case BaseTextNode textNode:
				{
					var arrayNode = new ArrayNode { Count = textNode.Length };
					arrayNode.CopyFromNode(node);
					arrayNode.ChangeInnerNode(GetCharacterNodeForEncoding(textNode.Encoding));
					return arrayNode;
				}
				case BaseTextPtrNode textPtrNode:
				{
					var pointerNode = new PointerNode();
					pointerNode.CopyFromNode(node);
					pointerNode.ChangeInnerNode(GetCharacterNodeForEncoding(textPtrNode.Encoding));
					return pointerNode;
				}
				case BitFieldNode bitFieldNode:
				{
					var underlayingNode = bitFieldNode.GetUnderlayingNode();
					underlayingNode.CopyFromNode(node);
					return underlayingNode;
				}
				case BaseHexNode hexNode:
				{
					var arrayNode = new ArrayNode { Count = hexNode.MemorySize };
					arrayNode.CopyFromNode(node);
					arrayNode.ChangeInnerNode(new Utf8CharacterNode());
					return arrayNode;
				}
			}

			return node;
		}

		private string GetTypeDefinition(BaseNode node)
		{
			Contract.Requires(node != null);

			if (nodeTypeToTypeDefinationMap.TryGetValue(node.GetType(), out var type))
			{
				return type;
			}

			switch (node)
			{
				case ClassInstanceNode classInstanceNode:
					if (classInstanceNode.InnerNode is ClassNode innerClassNode)
					{
						return GetClassName(innerClassNode);
					}
					return classInstanceNode.InnerNode?.Name ?? "ReClassUnknown";
				case EnumNode enumNode:
					return enumNode.Enum.Name;
			}

			return null;
		}

		private string ResolveWrappedType(BaseNode node)
		{
			Contract.Requires(node != null);

			string Resolve(BaseNode currentNode)
			{
				currentNode = TransformNode(currentNode);

				if (currentNode is PointerNode pointerNode)
				{
					if (pointerNode.InnerNode == null)
					{
						return "*mut c_void";
					}

					var innerType = Resolve(pointerNode.InnerNode);
					return $"*mut {innerType}";
				}

				if (currentNode is ArrayNode arrayNode)
				{
					var innerType = arrayNode.InnerNode != null ? Resolve(arrayNode.InnerNode) : "u8";
					return $"[{innerType}; {arrayNode.Count}]";
				}

				return GetTypeDefinition(currentNode) ?? "u8";
			}

			return Resolve(node);
		}

		private string GetStringPointerAlias(BaseNode node)
		{
			switch (node)
			{
				case Utf8TextPtrNode _:
					usesUtf8StrPtr = true;
					return "Utf8StrPtr";
				case Utf16TextPtrNode _:
					usesUtf16StrPtr = true;
					return "Utf16StrPtr";
				case Utf32TextPtrNode _:
					usesUtf32StrPtr = true;
					return "Utf32StrPtr";
				default:
					return null;
			}
		}

		private void BuildClassNameMap(IReadOnlyList<ClassNode> classes)
		{
			classNameMap.Clear();

			var usedNames = new HashSet<string>(classes.Select(c => c.Name));

			foreach (var @class in classes)
			{
				if (@class.Name != "_")
				{
					classNameMap[@class] = @class.Name;
					continue;
				}

				var i = 1;
				string candidate;
				do
				{
					candidate = $"ReClassUnnamed{i++}";
				} while (usedNames.Contains(candidate));

				usedNames.Add(candidate);
				classNameMap[@class] = candidate;
			}
		}

		private string GetClassName(ClassNode @class)
		{
			if (@class == null)
			{
				return "ReClassUnknown";
			}

			return classNameMap.TryGetValue(@class, out var className) ? className : @class.Name;
		}
	}
}
