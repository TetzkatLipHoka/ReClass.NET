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
	/// <summary>
	/// A C code generator for custom nodes.
	/// </summary>
	public abstract class CustomCCodeGenerator
	{
		/// <summary>
		/// Returns <c>true</c> if the code generator can handle the given node.
		/// </summary>
		/// <param name="node">The node to check.</param>
		/// <returns>True if the code generator can handle the given node, false otherwise.</returns>
		public abstract bool CanHandle(BaseNode node);

		/// <summary>
		/// Outputs the C++ code for the node to the <see cref="TextWriter"/> instance.
		/// </summary>
		/// <param name="writer">The writer to output to.</param>
		/// <param name="node">The node to output.</param>
		/// <param name="defaultWriteNodeFunc">The default implementation of <see cref="CppCodeGenerator.WriteNode"/>.</param>
		/// <param name="logger">The logger.</param>
		/// <returns>True if the code generator has processed the node, false otherwise. If this method returns false, the default implementation is used.</returns>
		public virtual bool WriteNode(IndentedTextWriter writer, BaseNode node, WriteNodeFunc defaultWriteNodeFunc, ILogger logger)
		{
			return false;
		}

		/// <summary>
		/// Transforms the given node if necessary.
		/// </summary>
		/// <param name="node">The node to transform.</param>
		/// <returns>The transformed node.</returns>
		public virtual BaseNode TransformNode(BaseNode node)
		{
			return node;
		}

		/// <summary>
		/// Gets the type definition for the node. If the node is not a simple node <c>null</c> is returned.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="defaultGetTypeDefinitionFunc">The default implementation of <see cref="CppCodeGenerator.GetTypeDefinition"/>.</param>
		/// <param name="defaultResolveWrappedTypeFunc">The default implementation of <see cref="CppCodeGenerator.ResolveWrappedType"/>.</param>
		/// <param name="logger">The logger.</param>
		/// <returns>The type definition for the node or null if no simple type is available.</returns>
		public virtual string GetTypeDefinition(BaseNode node, GetTypeDefinitionFunc defaultGetTypeDefinitionFunc, ResolveWrappedTypeFunc defaultResolveWrappedTypeFunc, ILogger logger)
		{
			return null;
		}
	}

	public class CCodeGenerator : ICodeGenerator
	{
		#region Custom Code Generators

		private static readonly ISet<CustomCppCodeGenerator> customGenerators = new HashSet<CustomCppCodeGenerator>();

		public static void Add(CustomCppCodeGenerator generator)
		{
			customGenerators.Add(generator);
		}

		public static void Remove(CustomCppCodeGenerator generator)
		{
			customGenerators.Remove(generator);
		}

		private static CustomCppCodeGenerator GetCustomCodeGeneratorForNode(BaseNode node)
		{
			return customGenerators.FirstOrDefault(g => g.CanHandle(node));
		}

		#endregion

		private readonly Dictionary<Type, string> nodeTypeToTypeDefinationMap;

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

		public Language Language => Language.C;

		public CCodeGenerator()
		{
			nodeTypeToTypeDefinationMap = new Dictionary<Type, string>
			{
				[typeof(BoolNode)] = "bool",          // C99's bool type
				[typeof(DoubleNode)] = "double",
				[typeof(FloatNode)] = "float",
				[typeof(FunctionPtrNode)] = "void*",
				[typeof(Int8Node)] = "int8_t",
				[typeof(Int16Node)] = "int16_t",
				[typeof(Int32Node)] = "int32_t",
				[typeof(Int64Node)] = "int64_t",
				[typeof(NIntNode)] = "intptr_t",       // Platform-specific signed integer type
				[typeof(Matrix3x3Node)] = "float[9]",  // 3x3 array of floats
				[typeof(Matrix3x4Node)] = "float[12]", // 3x4 array of floats
				[typeof(Matrix4x4Node)] = "float[16]", // 4x4 array of floats
				[typeof(UInt8Node)] = "uint8_t",
				[typeof(UInt16Node)] = "uint16_t",
				[typeof(UInt32Node)] = "uint32_t",
				[typeof(UInt64Node)] = "uint64_t",
				[typeof(NUIntNode)] = "size_t",
				[typeof(Utf8CharacterNode)] = "char",
				[typeof(Utf16CharacterNode)] = "char16_t",
				[typeof(Utf32CharacterNode)] = "char32_t",
				[typeof(Vector2Node)] = "float[2]",    // 2D vector as float array
				[typeof(Vector3Node)] = "float[3]",    // 3D vector as float array
				[typeof(Vector4Node)] = "float[4]"     // 4D vector as float array
			};
		}

		public string GenerateCode(IReadOnlyList<ClassNode> classes, IReadOnlyList<EnumDescription> enums, ILogger logger)
		{
			using var sw = new StringWriter();
			using var iw = new IndentedTextWriter(sw, "\t");

			iw.WriteLine($"// Created with {Constants.ApplicationName} {Constants.ApplicationVersion} by {Constants.Author}");
			iw.WriteLine();

			using (var en = enums.GetEnumerator())
			{
				if (en.MoveNext())
				{
					WriteEnum(iw, en.Current);

					while (en.MoveNext())
					{
						iw.WriteLine();

						WriteEnum(iw, en.Current);
					}

					iw.WriteLine();
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
					.Where(w => !w.IsNodePresentInChain<PointerNode>()) // Pointers are forward declared
					.Select(w => w.ResolveMostInnerNode() as ClassNode)
					.Where(n => n != null);

				return classNodes
					.SelectMany(GetReversedClassHierarchy)
					.Append(node);
			}

			var classesToWrite = classes
				.Where(c => c.Nodes.None(n => n is FunctionNode)) // Skip class which contains FunctionNodes because these are not data classes.
				.SelectMany(GetReversedClassHierarchy) // Order the classes by their use hierarchy.
				.Distinct();

			using (var en = classesToWrite.GetEnumerator())
			{
				if (en.MoveNext())
				{
					WriteClass(iw, en.Current, classes, logger);

					while (en.MoveNext())
					{
						iw.WriteLine();

						WriteClass(iw, en.Current, classes, logger);
					}
				}
			}

			return sw.ToString();
		}

		/// <summary>
		/// Outputs the C++ code for the given enum to the <see cref="TextWriter"/> instance.
		/// </summary>
		/// <param name="writer">The writer to output to.</param>
		/// <param name="enum">The enum to output.</param>
		private void WriteEnum(IndentedTextWriter writer, EnumDescription @enum)
		{
			Contract.Requires(writer != null);
			Contract.Requires(@enum != null);

			writer.WriteLine($"enum {@enum.Name}");
			writer.WriteLine("{");
			writer.Indent++;
			for (var j = 0; j < @enum.Values.Count; ++j)
			{
				var kv = @enum.Values[j];

				writer.Write(kv.Key);
				writer.Write(" = ");
				writer.Write(kv.Value);
				if (j < @enum.Values.Count - 1)
				{
					writer.Write(",");
				}
				writer.WriteLine();
			}
			writer.Indent--;
			writer.WriteLine("};");
		}

		/// <summary>
		/// Outputs the C++ code for the given class to the <see cref="TextWriter"/> instance.
		/// </summary>
		/// <param name="writer">The writer to output to.</param>
		/// <param name="class">The class to output.</param>
		/// <param name="classes">The list of all available classes.</param>
		/// <param name="logger">The logger.</param>
		private void WriteClass(IndentedTextWriter writer, ClassNode @class, IEnumerable<ClassNode> classes, ILogger logger)
		{
			Contract.Requires(writer != null);
			Contract.Requires(@class != null);
			Contract.Requires(classes != null);

			writer.Write("struct __attribute__((packed)) ");
			writer.Write(@class.Name);

			var skipFirstMember = false;

			if (!string.IsNullOrEmpty(@class.Comment))
			{
				writer.Write(" // ");
				writer.Write(@class.Comment);
			}

			writer.WriteLine();

			writer.WriteLine("{");
			writer.Indent++;

			var nodes = @class.Nodes
				.Skip(skipFirstMember ? 1 : 0)
				.WhereNot(n => n is FunctionNode);
			WriteNodes(writer, nodes, logger);

			var vTableNodes = @class.Nodes.OfType<VirtualMethodTableNode>().ToList();
			if (vTableNodes.Any())
			{
				writer.WriteLine();

				var virtualMethodNodes = vTableNodes
					.SelectMany(vt => vt.Nodes)
					.OfType<VirtualMethodNode>();
				foreach (var node in virtualMethodNodes)
				{
					writer.Write("virtual void ");
					writer.Write(node.MethodName);
					writer.WriteLine("();");
				}
			}

			var functionNodes = classes
				.SelectMany(c2 => c2.Nodes)
				.OfType<FunctionNode>()
				.Where(f => f.BelongsToClass == @class)
				.ToList();
			if (functionNodes.Any())
			{
				writer.WriteLine();

				foreach (var node in functionNodes)
				{
					writer.Write(node.Signature);
					writer.WriteLine("{ }");
				}
			}

			writer.Indent--;
			writer.Write("}; //Size: 0x");
			writer.WriteLine($"{@class.MemorySize:X04}");
		}

		/// <summary>
		/// Outputs the C++ code for the given nodes to the <see cref="TextWriter"/> instance.
		/// </summary>
		/// <param name="writer">The writer to output to.</param>
		/// <param name="nodes">The nodes to output.</param>
		/// <param name="logger">The logger.</param>
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

			foreach (var member in nodes.WhereNot(m => m is VirtualMethodTableNode))
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

		/// <summary>
		/// Outputs the C++ code for the given node to the <see cref="TextWriter"/> instance.
		/// </summary>
		/// <param name="writer">The writer to output to.</param>
		/// <param name="node">The node to output.</param>
		/// <param name="logger">The logger.</param>
		private void WriteNode(IndentedTextWriter writer, BaseNode node, ILogger logger)
		{
			Contract.Requires(writer != null);
			Contract.Requires(node != null);

			var custom = GetCustomCodeGeneratorForNode(node);
			if (custom != null)
			{
				if (custom.WriteNode(writer, node, WriteNode, logger))
				{
					return;
				}
			}

			node = TransformNode(node);

			var simpleType = GetTypeDefinition(node, logger);
			if (simpleType != null)
			{
				//$"{type} {node.Name}; //0x{node.Offset.ToInt32():X04} {node.Comment}".Trim();
				writer.Write(simpleType);
				writer.Write(" ");
				writer.Write(node.Name);
				writer.Write("; //0x");
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
				writer.Write(ResolveWrappedType(node, false, logger));
				writer.Write("; //0x");
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
				writer.Write("union //0x");
				writer.Write($"{node.Offset:X04}");
				if (!string.IsNullOrEmpty(node.Comment))
				{
					writer.Write(" ");
					writer.Write(node.Comment);
				}
				writer.WriteLine();
				writer.WriteLine("{");
				writer.Indent++;

				WriteNodes(writer, unionNode.Nodes, logger);

				writer.Indent--;

				writer.WriteLine($"}} {node.Name};");
			}
			else
			{
				logger.Log(LogLevel.Error, $"Skipping node with unhandled type: {node.GetType()}");
			}
		}

		/// <summary>
		/// Transforms the given node into some other node if necessary.
		/// </summary>
		/// <param name="node">The node to transform.</param>
		/// <returns>The transformed node.</returns>
		private static BaseNode TransformNode(BaseNode node)
		{
			var custom = GetCustomCodeGeneratorForNode(node);
			if (custom != null)
			{
				return custom.TransformNode(node);
			}

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

		/// <summary>
		/// Gets the type definition for the given node. If the node is not a simple node <c>null</c> is returned.
		/// </summary>
		/// <param name="node">The target node.</param>
		/// <param name="logger">The logger.</param>
		/// <returns>The type definition for the node or null if no simple type is available.</returns>
		private string GetTypeDefinition(BaseNode node, ILogger logger)
		{
			Contract.Requires(node != null);

			var custom = GetCustomCodeGeneratorForNode(node);
			if (custom != null)
			{
				return custom.GetTypeDefinition(node, GetTypeDefinition, ResolveWrappedType, logger);
			}

			if (nodeTypeToTypeDefinationMap.TryGetValue(node.GetType(), out var type))
			{
				return type;
			}

			switch (node)
			{
				case ClassInstanceNode classInstanceNode:
					return $"struct {classInstanceNode.InnerNode.Name}";
				case EnumNode enumNode:
					return $"enum {enumNode.Enum.Name}";
			}

			return null;
		}

		/// <summary>
		/// Resolves the type of a <see cref="BaseWrapperNode"/> node (<see cref="PointerNode"/> and <see cref="ArrayNode"/>).
		/// </summary>
		/// <param name="node">The node to resolve.</param>
		/// <param name="isAnonymousExpression">Specify if the expression should be anonymous.</param>
		/// <param name="logger">The logger.</param>
		/// <returns>The resolved type of the node.</returns>
		private string ResolveWrappedType(BaseNode node, bool isAnonymousExpression, ILogger logger)
		{
			Contract.Requires(node != null);

			var sb = new StringBuilder();
			if (!isAnonymousExpression)
			{
				sb.Append(node.Name);
			}

			BaseNode lastWrapperNode = null;
			var currentNode = node;

			while (true)
			{
				currentNode = TransformNode(currentNode);

				if (currentNode is PointerNode pointerNode)
				{
					sb.Prepend('*');

					if (pointerNode.InnerNode == null) // void*
					{
						if (!isAnonymousExpression)
						{
							sb.Prepend(' ');
						}
						sb.Prepend("void");
						break;
					}

					lastWrapperNode = pointerNode;
					currentNode = pointerNode.InnerNode;
				}
				else if (currentNode is ArrayNode arrayNode)
				{
					if (lastWrapperNode is PointerNode)
					{
						sb.Prepend('(');
						sb.Append(')');
					}

					sb.Append($"[{arrayNode.Count}]");

					lastWrapperNode = arrayNode;
					currentNode = arrayNode.InnerNode;
				}
				else
				{
					var simpleType = GetTypeDefinition(currentNode, logger);

					if (!isAnonymousExpression)
					{
						sb.Prepend(' ');
					}

					sb.Prepend(simpleType);
					break;
				}
			}

			return sb.ToString().Trim();
		}
	}
}