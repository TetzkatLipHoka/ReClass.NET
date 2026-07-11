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
	public class PascalCodeGenerator : ICodeGenerator
	{
		private readonly Dictionary<Type, string> nodeTypeToTypeDefinationMap;
		private readonly Dictionary<Type, string> nodeTypeToPointerTypeMap;
		private readonly Dictionary<ClassNode, string> classNameMap = new Dictionary<ClassNode, string>();

		private static readonly HashSet<string> reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"and", "array", "as", "asm", "begin", "case", "class", "const", "constructor", "destructor",
			"dispinterface", "div", "do", "downto", "else", "end", "except", "exports", "file", "finalization",
			"finally", "for", "function", "goto", "if", "implementation", "in", "inherited", "initialization",
			"inline", "interface", "is", "label", "library", "mod", "nil", "not", "object", "of", "or",
			"packed", "procedure", "program", "property", "raise", "record", "repeat", "resourcestring",
			"set", "shl", "shr", "string", "then", "threadvar", "to", "try", "type", "unit", "until",
			"uses", "var", "while", "with", "xor"
		};

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

		public Language Language => Language.Pascal;

		public PascalCodeGenerator()
		{
			nodeTypeToTypeDefinationMap = new Dictionary<Type, string>
			{
				[typeof(BoolNode)] = "Boolean",
				[typeof(DoubleNode)] = "Double",
				[typeof(FloatNode)] = "Single",
				[typeof(FunctionPtrNode)] = "Pointer",
				[typeof(VirtualMethodTableNode)] = "Pointer",
				[typeof(Int8Node)] = "ShortInt",
				[typeof(Int16Node)] = "SmallInt",
				[typeof(Int32Node)] = "Integer",
				[typeof(Int64Node)] = "Int64",
				[typeof(NIntNode)] = "NativeInt",
				[typeof(Matrix3x3Node)] = "array [0..8] of Single",
				[typeof(Matrix3x4Node)] = "array [0..11] of Single",
				[typeof(Matrix4x4Node)] = "array [0..15] of Single",
				[typeof(UInt8Node)] = "Byte",
				[typeof(UInt16Node)] = "Word",
				[typeof(UInt32Node)] = "Cardinal",
				[typeof(UInt64Node)] = "UInt64",
				[typeof(NUIntNode)] = "NativeUInt",
				[typeof(Utf8CharacterNode)] = "AnsiChar",
				[typeof(Utf16CharacterNode)] = "WideChar",
				[typeof(Utf32CharacterNode)] = "UCS4Char",
				[typeof(Vector2Node)] = "array [0..1] of Single",
				[typeof(Vector3Node)] = "array [0..2] of Single",
				[typeof(Vector4Node)] = "array [0..3] of Single"
			};

			nodeTypeToPointerTypeMap = new Dictionary<Type, string>
			{
				[typeof(BoolNode)] = "PBoolean",
				[typeof(DoubleNode)] = "PDouble",
				[typeof(FloatNode)] = "PSingle",
				[typeof(Int8Node)] = "PShortInt",
				[typeof(Int16Node)] = "PSmallInt",
				[typeof(Int32Node)] = "PInteger",
				[typeof(Int64Node)] = "PInt64",
				[typeof(NIntNode)] = "PNativeInt",
				[typeof(UInt8Node)] = "PByte",
				[typeof(UInt16Node)] = "PWord",
				[typeof(UInt32Node)] = "PCardinal",
				[typeof(UInt64Node)] = "PUInt64",
				[typeof(NUIntNode)] = "PNativeUInt",
				[typeof(Utf8CharacterNode)] = "PAnsiChar",
				[typeof(Utf16CharacterNode)] = "PWideChar",
				[typeof(Utf32CharacterNode)] = "PUCS4Char"
			};
		}

		public string GenerateCode(IReadOnlyList<ClassNode> classes, IReadOnlyList<EnumDescription> enums, ILogger logger)
		{
			using var bodySw = new StringWriter();
			using var bodyIw = new IndentedTextWriter(bodySw, "  ");
			bodyIw.Indent = 1;

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
				.Distinct()
				.ToList();

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
			using var iw = new IndentedTextWriter(sw, "  ");

			iw.WriteLine($"// Created with {Constants.ApplicationName} {Constants.ApplicationVersion} by {Constants.Author}");
			iw.WriteLine();
			iw.WriteLine("unit ReClassTypes;");
			iw.WriteLine();
			iw.WriteLine("interface");
			iw.WriteLine();

			foreach (var @enum in enums)
			{
				WriteEnum(iw, @enum);
				iw.WriteLine();
			}

			if (classesToWrite.Count > 0)
			{
				iw.WriteLine("type");
				iw.Indent++;
				foreach (var @class in classesToWrite)
				{
					iw.WriteLine($"{GetClassPointerName(@class)} = ^{GetClassName(@class)};");
				}
				iw.Indent--;
				iw.WriteLine();
				iw.Write(bodySw.ToString());
			}

			iw.WriteLine();
			iw.WriteLine("implementation");
			iw.WriteLine();
			iw.WriteLine("end.");

			return sw.ToString();
		}

		private static void WriteEnum(IndentedTextWriter writer, EnumDescription @enum)
		{
			Contract.Requires(writer != null);
			Contract.Requires(@enum != null);

			string underlyingType;
			switch (@enum.Size)
			{
				case EnumDescription.UnderlyingTypeSize.OneByte:
					underlyingType = "Byte";
					break;
				case EnumDescription.UnderlyingTypeSize.TwoBytes:
					underlyingType = "Word";
					break;
				case EnumDescription.UnderlyingTypeSize.EightBytes:
					underlyingType = "UInt64";
					break;
				default:
					underlyingType = "Cardinal";
					break;
			}

			var enumName = MakeTypeIdentifier(@enum.Name);

			writer.WriteLine("type");
			writer.Indent++;
			writer.WriteLine($"T{enumName} = {underlyingType};");
			writer.WriteLine($"P{enumName} = ^T{enumName};");
			writer.Indent--;
			writer.WriteLine();
			writer.WriteLine("const");
			writer.Indent++;
			foreach (var kv in @enum.Values)
			{
				writer.WriteLine($"{enumName}_{MakeIdentifier(kv.Key)} = T{enumName}({kv.Value});");
			}
			writer.Indent--;
		}

		private void WriteClass(IndentedTextWriter writer, ClassNode @class, ILogger logger)
		{
			Contract.Requires(writer != null);
			Contract.Requires(@class != null);
			Contract.Requires(logger != null);

			writer.Write(GetClassName(@class));
			writer.Write(" = packed record");
			if (!string.IsNullOrEmpty(@class.Comment))
			{
				writer.Write(" // ");
				writer.Write(@class.Comment);
			}
			writer.WriteLine();
			writer.Indent++;

			var nodes = @class.Nodes.WhereNot(n => n is FunctionNode);
			WriteNodes(writer, nodes, logger);

			writer.Indent--;
			writer.WriteLine($"end; // Size: 0x{@class.MemorySize:X04}");
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
					Name = $"Pad_{offset:X04}"
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

			void WriteMember(string typeName)
			{
				writer.Write(GetFieldName(node));
				writer.Write(": ");
				writer.Write(typeName);
				writer.Write("; // 0x");
				writer.Write($"{node.Offset:X04}");
				if (!string.IsNullOrEmpty(node.Comment))
				{
					writer.Write(" ");
					writer.Write(node.Comment);
				}
				writer.WriteLine();
			}

			var aliasType = GetStringPointerAlias(node);
			if (aliasType != null)
			{
				WriteMember(aliasType);
				return;
			}

			node = TransformNode(node);

			var typeName = GetTypeDefinition(node);
			if (typeName != null)
			{
				WriteMember(typeName);
			}
			else if (node is BaseWrapperNode)
			{
				WriteMember(ResolveWrappedType(node));
			}
			else if (node is UnionNode unionNode)
			{
				WriteMember($"array [0..{Math.Max(unionNode.MemorySize, 1) - 1}] of Byte");
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
					return MakeTypeIdentifier(classInstanceNode.InnerNode?.Name ?? "ReClassUnknown");
				case EnumNode enumNode:
					return $"T{MakeTypeIdentifier(enumNode.Enum.Name)}";
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
					var innerNode = pointerNode.InnerNode;
					if (innerNode == null)
					{
						return "Pointer";
					}

					innerNode = TransformNode(innerNode);

					if (innerNode is ClassNode innerClassNode)
					{
						return GetClassPointerName(innerClassNode);
					}
					if (innerNode is ClassInstanceNode classInstanceNode && classInstanceNode.InnerNode is ClassNode instanceClassNode)
					{
						return GetClassPointerName(instanceClassNode);
					}
					if (innerNode is EnumNode enumNode)
					{
						return $"P{MakeTypeIdentifier(enumNode.Enum.Name)}";
					}
					if (nodeTypeToPointerTypeMap.TryGetValue(innerNode.GetType(), out var pointerType))
					{
						return pointerType;
					}

					// Multi level pointers and pointers to unnamed constructs degrade to an untyped pointer.
					return "Pointer";
				}

				if (currentNode is ArrayNode arrayNode)
				{
					var innerType = arrayNode.InnerNode != null ? Resolve(arrayNode.InnerNode) : "Byte";
					return $"array [0..{Math.Max(arrayNode.Count, 1) - 1}] of {innerType}";
				}

				return GetTypeDefinition(currentNode) ?? "Byte";
			}

			return Resolve(node);
		}

		private static string GetStringPointerAlias(BaseNode node)
		{
			switch (node)
			{
				case Utf8TextPtrNode _:
				case DefaultTextPtrNode _:
					return "PAnsiChar";
				case Utf16TextPtrNode _:
					return "PWideChar";
				case Utf32TextPtrNode _:
					return "PUCS4Char";
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
					classNameMap[@class] = MakeTypeIdentifier(@class.Name);
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
				return "TReClassUnknown";
			}

			return "T" + (classNameMap.TryGetValue(@class, out var className) ? className : MakeTypeIdentifier(@class.Name));
		}

		private string GetClassPointerName(ClassNode @class)
		{
			if (@class == null)
			{
				return "Pointer";
			}

			return "P" + (classNameMap.TryGetValue(@class, out var className) ? className : MakeTypeIdentifier(@class.Name));
		}

		private static string GetFieldName(BaseNode node)
		{
			var name = MakeIdentifier(node.Name);
			if (name.Length == 0)
			{
				name = $"Field_{node.Offset:X04}";
			}
			if (reservedWords.Contains(name))
			{
				name = "&" + name;
			}
			return name;
		}

		/// <summary>Sanitizes a name to a valid Pascal identifier (no reserved word handling).</summary>
		private static string MakeIdentifier(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return string.Empty;
			}

			var sb = new StringBuilder(name.Length);
			foreach (var c in name)
			{
				sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
			}
			if (char.IsDigit(sb[0]))
			{
				sb.Insert(0, '_');
			}
			return sb.ToString();
		}

		/// <summary>Sanitizes a name for use behind a T/P type prefix.</summary>
		private static string MakeTypeIdentifier(string name)
		{
			var identifier = MakeIdentifier(name);
			return identifier.Length == 0 ? "ReClassUnknown" : identifier;
		}
	}
}
