using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using ReClassNET.Logger;
using ReClassNET.Nodes;
using ReClassNET.Project;
using static ReClassNET.AddressParser.DynamicCompiler;

namespace ReClassNET.DataExchange.ReClass
{
	public partial class ReClassNetFile
	{
		public static List<ClassNode> DeletedReferencedClasses = new List<ClassNode>(); //For classes that are mistakenly deleted
		public void Save(string filePath, ILogger logger)
		{
			using var fs = new FileStream(filePath, FileMode.Create);

			var ext = Path.GetExtension(filePath);
			if(ext == DefaultFileExtension)
			{
				using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

				var dataEntry = archive.CreateEntry(DataFileName);
				using var entryStream = dataEntry.Open();
				Save(entryStream, logger);
			} 
			else if(ext == AlternateFileExtension)
			{
				Save(fs, logger);
			}
		}

		public void Save(Stream output, ILogger logger)
		{
			var document = new XDocument(
				new XComment($"{Constants.ApplicationName} {Constants.ApplicationVersion} by {Constants.Author}"),
				new XComment($"Website: {Constants.HomepageUrl}"),
				new XElement(
					XmlRootElement,
					new XAttribute(XmlVersionAttribute, FileVersion),
					new XAttribute(XmlPlatformAttribute, Constants.Platform),
					project.CustomData.Serialize(XmlCustomDataElement),
					project.TypeMapping.Serialize(XmlTypeMappingElement),
					new XElement(XmlEnumsElement, CreateEnumElements(project.Enums)),
					new XElement(XmlClassesElement, CreateClassElements(project.Classes, logger))
				)
			);
			while (DeletedReferencedClasses.Count > 0)
			{
				foreach (var element in GetDeletedClassesElements(logger).Element(XmlRootElement).Element(XmlClassesElement).Elements())
					document.Element(XmlRootElement).Element(XmlClassesElement).Add(element);
			}
			document.Save(output);
		}
		private static XDocument GetDeletedClassesElements(ILogger logger)
		{
			var newlist = new List<ClassNode>(DeletedReferencedClasses);
			DeletedReferencedClasses.Clear();
			var newdoc = new XDocument(
				new XElement(
					XmlRootElement,
					new XElement(XmlClassesElement, CreateClassElements(newlist, logger))
				));
			return newdoc;
		}
		private static IEnumerable<XElement> CreateEnumElements(IEnumerable<EnumDescription> enums)
		{
			return enums.Select(e => new XElement(
				XmlEnumElement,
				new XAttribute(XmlNameAttribute, e.Name),
				new XAttribute(XmlSizeAttribute, e.Size),
				new XAttribute(XmlFlagsAttribute, e.UseFlagsMode),
				e.Values.Select(kv => new XElement(
					XmlItemElement,
					new XAttribute(XmlNameAttribute, kv.Key),
					new XAttribute(XmlValueAttribute, kv.Value)
				))
			));
		}

		private static IEnumerable<XElement> CreateClassElements(IEnumerable<ClassNode> classes, ILogger logger)
		{
			Contract.Requires(classes != null);
			Contract.Requires(Contract.ForAll(classes, c => c != null));
			Contract.Requires(logger != null);
			Contract.Ensures(Contract.Result<IEnumerable<XElement>>() != null);

			IEnumerable<XElement> elements = classes.Select(c => new XElement(
				XmlClassElement,
				new XAttribute(XmlUuidAttribute, c.Uuid),
				new XAttribute(XmlNameAttribute, c.Name ?? string.Empty),
				new XAttribute(XmlCommentAttribute, c.Comment ?? string.Empty),
				new XAttribute(XmlAddressAttribute, c.AddressFormula ?? string.Empty),
				c.Nodes.Select(n => CreateElementFromNode(n, logger)).Where(e => e != null)
			));
			
			return elements;
		}

		private static XElement CreateElementFromNode(BaseNode node, ILogger logger)
		{
			Contract.Requires(node != null);
			Contract.Requires(logger != null);

			XElement CreateElement()
			{
				var converter = CustomNodeSerializer.GetWriteConverter(node);
				if (converter != null)
				{
					return converter.CreateElementFromNode(node, logger, CreateElementFromNode);
				}

				if (!buildInTypeToStringMap.TryGetValue(node.GetType(), out var typeString))
				{
					logger.Log(LogLevel.Error, $"Skipping node with unknown type: {node.Name}");
					logger.Log(LogLevel.Warning, node.GetType().ToString());

					return null;
				}

				return new XElement(
					XmlNodeElement,
					new XAttribute(XmlTypeAttribute, typeString)
				);
			}

			var element = CreateElement();
			if (element == null)
			{
				logger.Log(LogLevel.Error, "Could not create element.");

				return null;
			}

			element.SetAttributeValue(XmlNameAttribute, node.Name ?? string.Empty);
			element.SetAttributeValue(XmlCommentAttribute, node.Comment ?? string.Empty);
			element.SetAttributeValue(XmlHiddenAttribute, node.IsHidden);

			if (node is BaseWrapperNode wrapperNode)
			{
				if (node is BaseClassWrapperNode classWrapperNode)
				{
					element.SetAttributeValue(XmlReferenceAttribute, ((ClassNode)classWrapperNode.InnerNode).Uuid);
					//In case if class was mistakenly deleted from project, add it
					//if (!Program.MainForm.CurrentProject.ContainsClass(((ClassNode)classWrapperNode.InnerNode).Uuid))
					if (!ReClassNET.AddressParser.DynamicCompiler.projectContext.Classes.Any(c => c.Uuid.Equals(((ClassNode)classWrapperNode.InnerNode).Uuid)))
					    DeletedReferencedClasses.Add(classWrapperNode.InnerNode as ClassNode);

				}
				else if (wrapperNode.InnerNode != null)
				{
					element.Add(CreateElementFromNode(wrapperNode.InnerNode, logger));
				}
			}

			switch (node)
			{
				case VirtualMethodTableNode vtableNode:
				{
					element.Add(vtableNode.Nodes.Select(n => new XElement(
						XmlMethodElement,
						new XAttribute(XmlNameAttribute, n.Name ?? string.Empty),
						new XAttribute(XmlCommentAttribute, n.Comment ?? string.Empty),
						new XAttribute(XmlHiddenAttribute, n.IsHidden)
					)));
					break;
				}
				case UnionNode unionNode:
				{
					element.Add(unionNode.Nodes.Select(n => CreateElementFromNode(n, logger)));
					break;
				}
				case BaseWrapperArrayNode arrayNode:
				{
					element.SetAttributeValue(XmlCountAttribute, arrayNode.Count);
					break;
				}
				case BaseTextNode textNode:
				{
					element.SetAttributeValue(XmlLengthAttribute, textNode.Length);
					break;
				}
				case BitFieldNode bitFieldNode:
				{
					element.SetAttributeValue(XmlBitsAttribute, bitFieldNode.Bits);
					if (!buildInTypeToStringMap.TryGetValue(bitFieldNode.InnerNode.GetType(), out var typeString))
					{
						logger.Log(LogLevel.Error, $"Skipping node with unknown type: {node.Name}");
						logger.Log(LogLevel.Warning, node.GetType().ToString());

						return null;
					}
					element.SetAttributeValue(XmlInnerTypeAttribute, typeString);
					foreach(var subnode in bitFieldNode.Nodes)
					{
						element.Add(CreateElementFromNode(subnode, logger));
					}
					break;
				}
				case SingleBitNode singleBitNode:
				{
					element.SetAttributeValue(XmlBitStartAttribute, singleBitNode.BitStart);
					element.SetAttributeValue(XmlBitCountAttribute, singleBitNode.BitCount);
					element.SetAttributeValue(XmlBitCapAttribute, singleBitNode.BitCap);
					break;
				}
				case FunctionNode functionNode:
				{
					var uuid = functionNode.BelongsToClass?.Uuid ?? Guid.Empty;
					element.SetAttributeValue(XmlReferenceAttribute, uuid);
					element.SetAttributeValue(XmlSignatureAttribute, functionNode.Signature);
					break;
				}
				case EnumNode enumNode:
				{
					element.SetAttributeValue(XmlReferenceAttribute, enumNode.Enum.Name);
					break;
				}
				case CustomNode customNode:
				{
					element.SetAttributeValue(XmlSizeAttribute, customNode.RealSize);
					break;
				}
			}

			return element;
		}

		public static void SerializeNodesToStream(Stream output, IEnumerable<BaseNode> nodes, ILogger logger)
		{
			Contract.Requires(output != null);
			Contract.Requires(nodes != null);
			Contract.Requires(Contract.ForAll(nodes, n => n != null));
			Contract.Requires(logger != null);

			using var project = new ReClassNetProject();

			void RecursiveAddClasses(BaseNode node)
			{
				ClassNode classNode = null;
				switch (node)
				{
					case ClassNode c1:
						classNode = c1;
						break;
					case BaseWrapperNode wrapperNode when wrapperNode.ResolveMostInnerNode() is ClassNode c2:
						classNode = c2;
						break;
				}

				if (classNode == null || project.ContainsClass(classNode.Uuid))
				{
					return;
				}

				project.AddClass(classNode);

				foreach (var wrapperNodeChild in classNode.Nodes.OfType<BaseWrapperNode>())
				{
					RecursiveAddClasses(wrapperNodeChild);
				}
			}

			var serialisationClass = new ClassNode(false)
			{
				Name = SerializationClassName
			};

			var needsSerialisationClass = true;

			foreach (var node in nodes)
			{
				RecursiveAddClasses(node);

				if (!(node is ClassNode))
				{
					if (needsSerialisationClass)
					{
						needsSerialisationClass = false;

						project.AddClass(serialisationClass);
					}

					serialisationClass.AddNode(node);
				}
			}

			var file = new ReClassNetFile(project);
			file.Save(output, logger);
		}
	}
}
