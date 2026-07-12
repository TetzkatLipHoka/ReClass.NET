using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ReClassNET.DataExchange.ReClass;
using ReClassNET.Logger;
using ReClassNET.Plugins;
using ReClassNET.UI;

namespace ReClassNET.Forms
{
	public partial class PluginForm : IconForm
	{
		PluginManager pluginManager { get; set; }
		private class PluginInfoRow
		{
			private readonly PluginInfo plugin;

			public Image Icon
			{
				get
				{
					try
					{
						return plugin.Interface?.Icon ?? Properties.Resources.B16x16_Plugin;
					}
					catch
					{
						// Some plugins store their icon in a BinaryFormatter-serialized resource,
						// which can no longer be deserialized on modern .NET. Fall back to the
						// default plugin icon instead of failing the whole grid.
						return Properties.Resources.B16x16_Plugin;
					}
				}
			}
			public string Name => plugin.Name;
			public string Version => plugin.FileVersion;
			public string Author => plugin.Author;
			public string Description => plugin.Description;
			public PluginInfo Plugin => plugin;

			public PluginInfoRow(PluginInfo plugin)
			{
				Contract.Requires(plugin != null);
				Contract.Ensures(this.plugin != null);

				this.plugin = plugin;
			}
		}

		internal PluginForm(PluginManager pm)
		{
			Contract.Requires(pm != null);
			pluginManager = pm;
			InitializeComponent();
			UpdatePluginsInfo(pm);
		}
		private static void PluginsDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
			// A plugin's bound property (e.g. an icon stored in a legacy BinaryFormatter resource)
			// threw while the grid was reading it. Log it and keep the dialog from popping up.
			Program.Logger.Log(LogLevel.Error, $"Failed to read plugin info (row {e.RowIndex}, column {e.ColumnIndex}).");
			if (e.Exception != null)
			{
				Program.Logger.Log(e.Exception);
			}
			e.ThrowException = false;
		}

		internal void UpdatePluginsInfo(PluginManager pm)
		{
			// Plugins Tab
			pluginsDataGridView.AutoGenerateColumns = false;
			pluginsDataGridView.DataError -= PluginsDataGridView_DataError;
			pluginsDataGridView.DataError += PluginsDataGridView_DataError;
			pluginsDataGridView.DataSource = pm.Plugins.Select(p => new PluginInfoRow(p)).ToList();

			UpdatePluginDescription();

			// Native Methods Tab
			functionsProvidersComboBox.Items.Clear();
			var providers = Program.CoreFunctions.FunctionProviders.ToArray();
			functionsProvidersComboBox.Items.AddRange(providers);
			functionsProvidersComboBox.SelectedIndex = Array.IndexOf(providers, Program.CoreFunctions.CurrentFunctionsProvider);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			GlobalWindowManager.AddWindow(this);
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			base.OnFormClosed(e);

			GlobalWindowManager.RemoveWindow(this);
		}

		#region Event Handler

		private void pluginsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			UpdatePluginDescription();
			UpdateUnloadButtonInfo();
		}

		private void functionsProvidersComboBox_SelectionChangeCommitted(object sender, EventArgs e)
		{
			if (!(functionsProvidersComboBox.SelectedItem is string provider))
			{
				return;
			}

			Program.CoreFunctions.SetActiveFunctionsProvider(provider);
		}

		private void getMoreLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start(Constants.PluginUrl);
		}

		#endregion

		private void UpdatePluginDescription()
		{
			var row = pluginsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			if (row == null)
			{
				descriptionGroupBox.Text = string.Empty;
				descriptionLabel.Text = string.Empty;

				return;
			}

			if (row.DataBoundItem is PluginInfoRow plugin)
			{
				descriptionGroupBox.Text = plugin.Name;
				descriptionLabel.Text = plugin.Description;
			}
		}

		private void UpdateUnloadButtonInfo()
		{
			var row = pluginsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			if (row == null)
			{
				return;
			}

			if (row.DataBoundItem is PluginInfoRow plugin)
			{
				unloadPlugin.Enabled = true;
				unloadPlugin.Tag = plugin;
			}
		}

		private void loadButton_Click(object sender, EventArgs e)
		{
			if (pluginManager == null)
				return;
			pluginManager.Host.Logger.Log(Logger.LogLevel.Information, "Load Plugin clicked");
			using var ofd = new OpenFileDialog
			{
				CheckFileExists = true,
				Filter = $"All Plugin Types |*.dll;*.exe;*.so;"
			};

			if (ofd.ShowDialog() == DialogResult.OK)
			{
				if (pluginManager.LoadPlugin(ofd.FileName))
				{
					UpdatePluginsInfo(pluginManager);
					try
					{
						Program.CoreFunctions.SetActiveFunctionsProvider(Program.Settings.DefaultPlugin); 
					}						
					catch
					{
						Program.CoreFunctions.SetActiveFunctionsProvider("Default");
					}
				}
			}
		}

		private void unloadPlugin_Click(object sender, EventArgs e)
		{
			if (sender is Button button && button.Tag != null)
			{

				if (button.Tag is PluginInfoRow plugin)
				{
					pluginManager.UnloadPlugin(plugin.Plugin, true);
					UpdatePluginsInfo(pluginManager);

				}
				button.Tag = null;
			}
		}
	}
}
