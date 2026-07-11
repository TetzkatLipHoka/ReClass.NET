using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Windows.Forms;
using ReClassNET.CodeGenerator;
using ReClassNET.Core;
using ReClassNET.DataExchange.ReClass;
using ReClassNET.Extensions;
using ReClassNET.Logger;
using ReClassNET.Native;
using ReClassNET.Nodes;
using ReClassNET.UI;

namespace ReClassNET.Plugins
{
	internal sealed class PluginManager
	{
		public static string PluginsPath => Path.Combine(Application.StartupPath, Constants.PluginsFolder);
		private readonly List<PluginInfo> plugins = new List<PluginInfo>();

		private readonly IPluginHost host;

		public IEnumerable<PluginInfo> Plugins => plugins;
		public IPluginHost Host => host;	
		public PluginManager(IPluginHost host)
		{
			Contract.Requires(host != null);

			this.host = host;
		}

		public void LoadAllPlugins(string path, ILogger logger)
		{
			Contract.Requires(path != null);
			Contract.Requires(logger != null);

			try
			{
				if (!Directory.Exists(path))
				{
					return;
				}

				var directory = new DirectoryInfo(path);

				LoadPlugins(directory.GetFiles("*.dll", SearchOption.AllDirectories), logger, true);

				LoadPlugins(directory.GetFiles("*.exe", SearchOption.AllDirectories), logger, true);

				LoadPlugins(directory.GetFiles("*.so", SearchOption.AllDirectories), logger, false);
			}
			catch (Exception ex)
			{
				logger.Log(ex);
			}
		}

		private void LoadPlugins(IEnumerable<FileInfo> files, ILogger logger, bool checkProductName)
		{
			// TODO: How to include plugin infos for unix files as they don't have embedded version info.

			Contract.Requires(files != null);
			Contract.Requires(logger != null);

			foreach (var fi in files)
			{
				LoadPlugin(fi);
			}
		}

		public bool LoadPlugin(FileInfo file, bool checkProductName = true)
		{
			FileVersionInfo fvi;

			try
			{
				fvi = FileVersionInfo.GetVersionInfo(file.FullName);

				if (checkProductName && fvi.ProductName != PluginInfo.PluginName && fvi.ProductName != PluginInfo.PluginNativeName)
				{
					return false;
				}
				var pi = new PluginInfo(file.FullName, fvi);
				if (!pi.IsNative)
				{
					pi.Interface = CreatePluginInstance(pi.FilePath);

					if (!pi.Interface.Initialize(host))
					{
						return false;
					}

					RegisterNodeInfoReaders(pi);
					RegisterCustomNodeTypes(pi);
				}
				else
				{
					pi.NativeHandle = CreateNativePluginInstance(pi.FilePath);
					if (pi.NativeHandle == null)
					{
						throw new Exception($"Failed to load library: { pi.FilePath }");
					}
					Program.CoreFunctions.RegisterFunctions(
						pi.Name,
						new NativeCoreWrapper(pi.NativeHandle)
					);


				}

				plugins.Add(pi);
			}
			catch(Exception e)
			{
				host.Logger.Log(e);
				return false;
			}
			return true;
		}
		public bool LoadPlugin(string path)
		{
			try
			{
				if (!File.Exists(path))
				{
					return false;
				}
				FileInfo file = new FileInfo(path);
				if (file.Exists)
				{
					return LoadPlugin(file);
				}
			}
			catch (Exception ex)
			{
				host.Logger.Log(ex);
			}
			return false;
		}

		public void UnloadAllPlugins()
		{
			foreach (var pi in plugins)
			{
				UnloadPlugin(pi);
			}

			plugins.Clear();
		}

		public void UnloadPlugin(PluginInfo plugin, bool bAlsoRemove = false)
		{
			Contract.Requires(plugin != null);
			if (plugin.Interface != null)
			{
				DeregisterNodeInfoReaders(plugin);
				DeregisterCustomNodeTypes(plugin);
			}
			if (plugin.IsNative)
			{
				Program.CoreFunctions.UnregisterFunctions(plugin.Name);
				NativeMethods.FreeLibrary(plugin.NativeHandle);
			}

			plugin.Dispose();
			if (bAlsoRemove)
			{
				plugins.Remove(plugin);
			}

		}
		private static Plugin CreatePluginInstance(string filePath)
		{
			Contract.Requires(filePath != null);

			var type = Path.GetFileNameWithoutExtension(filePath);
			type = type + "." + type + "Ext";

			var handle = Activator.CreateInstanceFrom(filePath, type);

			if (!(handle.Unwrap() is Plugin plugin))
			{
				throw new FileLoadException();
			}
			return plugin;
		}

		private static IntPtr CreateNativePluginInstance(string filePath)
		{
			Contract.Requires(filePath != null);

			var handle = NativeMethods.LoadLibrary(filePath);
			if (handle.IsNull())
			{
				throw new FileLoadException($"Failed to load native plugin: {Path.GetFileName(filePath)}");
			}
			return handle;
		}

		private static void RegisterNodeInfoReaders(PluginInfo pluginInfo)
		{
			Contract.Requires(pluginInfo != null);

			var nodeInfoReaders = pluginInfo.Interface.GetNodeInfoReaders();

			if (nodeInfoReaders == null || nodeInfoReaders.Count == 0)
			{
				return;
			}

			pluginInfo.NodeInfoReaders = nodeInfoReaders;

			BaseNode.NodeInfoReader.AddRange(nodeInfoReaders);
		}

		private static void DeregisterNodeInfoReaders(PluginInfo pluginInfo)
		{
			Contract.Requires(pluginInfo != null);

			if (pluginInfo.NodeInfoReaders == null)
			{
				return;
			}

			foreach (var reader in pluginInfo.NodeInfoReaders)
			{
				BaseNode.NodeInfoReader.Remove(reader);
			}
		}

		private static void RegisterCustomNodeTypes(PluginInfo pluginInfo)
		{
			Contract.Requires(pluginInfo != null);

			var customNodeTypes = pluginInfo.Interface.GetCustomNodeTypes();

			if (customNodeTypes == null)
			{
				return;
			}

			if (customNodeTypes.NodeTypes == null || customNodeTypes.Serializer == null || customNodeTypes.CodeGenerator == null)
			{
				throw new ArgumentException($"The plugin '{pluginInfo.Name}' did not provide all required custom node type information.");
			}

			foreach (var nodeType in customNodeTypes.NodeTypes)
			{
				if (!nodeType.IsSubclassOf(typeof(BaseNode)))
				{
					throw new ArgumentException($"Type '{nodeType}' is not a valid node.");
				}
			}

			pluginInfo.CustomNodeTypes = customNodeTypes;

			NodeTypesBuilder.AddPluginNodeGroup(pluginInfo.Interface, customNodeTypes.NodeTypes);

			CustomNodeSerializer.Add(customNodeTypes.Serializer);
			CppCodeGenerator.Add(customNodeTypes.CodeGenerator);
		}

		private static void DeregisterCustomNodeTypes(PluginInfo pluginInfo)
		{
			Contract.Requires(pluginInfo != null);

			if (pluginInfo.CustomNodeTypes == null)
			{
				return;
			}

			NodeTypesBuilder.RemovePluginNodeGroup(pluginInfo.Interface);

			CustomNodeSerializer.Remove(pluginInfo.CustomNodeTypes.Serializer);
			CppCodeGenerator.Remove(pluginInfo.CustomNodeTypes.CodeGenerator);
		}
	}
}
