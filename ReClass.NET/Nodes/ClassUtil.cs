using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using ReClassNET.AddressParser;
using ReClassNET.Memory;
using ReClassNET.Util;

namespace ReClassNET.Nodes
{
	public class ClassUtil
	{
		/// <summary>
		/// Tests if the class to check can be inserted into the parent class without creating a cycle.
		/// </summary>
		/// <param name="parent">The class into which </param>
		/// <param name="classToCheck">The class which should get inserted.</param>
		/// <param name="classes">An enumeration of all available classes.</param>
		/// <returns>True if a cycle is detected, false otherwise.</returns>
		public static bool IsCyclicIfClassIsAccessibleFromParent(ClassNode parent, ClassNode classToCheck, IEnumerable<ClassNode> classes)
		{
			Contract.Requires(parent != null);
			Contract.Requires(classToCheck != null);
			Contract.Requires(classes != null);
			Contract.Requires(Contract.ForAll(classes, c => c != null));

			var graph = new DirectedGraph<ClassNode>();
			graph.AddVertices(classes);

			graph.AddEdge(parent, classToCheck);

			foreach (var c in graph.Vertices)
			{
				foreach (var node in c.Nodes)
				{
					if (!node.IsWrapped)
						continue;

					if (node is not BaseWrapperNode wrapperNode)
						continue;

					// BaseWrapperNode
					if (wrapperNode.ShouldPerformCycleCheckForInnerNode() && wrapperNode.ResolveMostInnerNode() is ClassNode classNode)
					{
						graph.AddEdge(c, classNode);
					}
				}
			}

			return graph.ContainsCycle();
		}

		/// <summary>
		/// Resolves the address of a class node by evaluating its address formula.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="node"></param>
		/// <returns></returns>
		public static IntPtr ResolveClassAddress(IProcessReader reader, ClassNode node)
		{
			if (string.IsNullOrEmpty(node.AddressFormula))
				return IntPtr.Zero;

			var expr = Parser.Parse(node.AddressFormula);
			return new DynamicCompiler().Execute(expr, reader);
		}
	}
}
