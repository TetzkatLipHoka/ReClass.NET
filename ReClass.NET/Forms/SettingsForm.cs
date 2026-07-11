using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DarkModeForms;
using ReClassNET.Controls;
using ReClassNET.Extensions;
using ReClassNET.Native;
using ReClassNET.Project;
using ReClassNET.UI;
using ReClassNET.Util;

namespace ReClassNET.Forms
{
	public partial class SettingsForm : IconForm
	{
		private readonly Settings settings;
		private readonly CppTypeMapping typeMapping;
		private static readonly string[] colorPresetDefaultNames = { "Default Light", "Default Dark", "Default Black" };
		private List<ColorPreset> presets;

		public TabControl SettingsTabControl => settingsTabControl;

		public SettingsForm(Settings settings, CppTypeMapping typeMapping)
		{
			Contract.Requires(settings != null);
			Contract.Requires(typeMapping != null);

			this.settings = settings;
			this.typeMapping = typeMapping;		

			InitializeComponent();

			var imageList = new ImageList();
			imageList.Images.Add(Properties.Resources.B16x16_Gear);
			imageList.Images.Add(Properties.Resources.B16x16_Color_Wheel);
			imageList.Images.Add(Properties.Resources.B16x16_Settings_Edit);
			imageList.Images.Add(Properties.Resources.B16x16_Gear);

			settingsTabControl.ImageList = imageList;
			generalSettingsTabPage.ImageIndex = 0;
			colorsSettingTabPage.ImageIndex = 1;
			typeDefinitionsSettingsTabPage.ImageIndex = 2;
			hotkeysSettingsTabPage.ImageIndex = 3;

			UpdatePluginComboBox();

			SetGeneralBindings();
			SetColorBindings();
			SetTypeDefinitionBindings();
			SetHotkeyBindings();

			var encodings = new[]
			{
				Encoding.UTF8,
				Encoding.Unicode,
				Encoding.UTF32,
				Encoding.Default,
				Encoding.ASCII
			};
			rawDataEncodingComboBox.DataSource = encodings;
			rawDataEncodingComboBox.DisplayMember = nameof(Encoding.EncodingName);
			rawDataEncodingComboBox.SelectedItem = encodings.FirstOrDefault(e => e.CodePage == settings.RawDataEncoding.CodePage);
			rawDataEncodingComboBox.SelectedIndexChanged += (s, e) => settings.RawDataEncoding = (Encoding)rawDataEncodingComboBox.SelectedItem;

			if (NativeMethods.IsUnix())
			{
				fileAssociationGroupBox.Enabled = false;
				runAsAdminCheckBox.Enabled = false;
			}
			else
			{
				NativeMethodsWindows.SetButtonShield(createAssociationButton, true);
				NativeMethodsWindows.SetButtonShield(removeAssociationButton, true);
			}
			LoadPresets();
			hotkeysFlowLayoutPanel.Width = hotkeysSettingsTabPage.Width - 30;

			// Wire up events
			savePresetButton.Click += savePresetButton_Click;
			loadPresetButton.Click += loadPresetButton_Click;
			deletePresetButton.Click += deletePresetButton_Click;
			presetComboBox.SelectedIndexChanged += presetComboBox_SelectedIndexChanged;
		}

		public void UpdatePluginComboBox()
		{
			defaultPluginComboBox.Items.Clear();
			var providers = Program.CoreFunctions.FunctionProviders.ToArray();
			defaultPluginComboBox.Items.AddRange(providers);
			for (int i = 0; i < providers.Length; i++)
			{
				if (providers[i].Equals(Program.Settings.DefaultPlugin))
				{
					defaultPluginComboBox.SelectedIndex = i;
					break;
				}				
			}
			if (defaultPluginComboBox.SelectedIndex < 0)
				defaultPluginComboBox.SelectedIndex = 0;
		}

		private void LoadPresets()
		{
			try
			{
				presets = ColorPresetSerializer.LoadPresets();
			}
			catch (Exception)
			{
				presets = new List<ColorPreset>();
			}

			presetComboBox.Items.Clear();
			foreach (var name in colorPresetDefaultNames) 
				presetComboBox.Items.Add(name);
			if (presets != null)  // Add null check
			{
				foreach (var preset in presets)
				{
					presetComboBox.Items.Add(preset.Name);
				}
			}

			presetComboBox.SelectedIndex = 0;
			RefreshColorBindings();
			deletePresetButton.Enabled = false; // Default presets can't be deleted

			// Update buttons state
			UpdatePresetsButtonsState();
		}

		private void UpdatePresetsButtonsState()
		{
			bool hasCustomPresets = presetComboBox.Items.Count > 2;
			deletePresetButton.Enabled = presetComboBox.SelectedIndex > 1;
			savePresetButton.Enabled = true; // Save should always be available
		}

		private void savePresetButton_Click(object sender, EventArgs e)
		{
			using var nameForm = new InputForm("Save Color Preset", "Enter preset name:");
			if (nameForm.ShowDialog() == DialogResult.OK)
			{
				var name = nameForm.Value;
				if (string.IsNullOrEmpty(name)) return;

				var preset = ColorPreset.CreateFrom(settings, name);

				foreach (var s in colorPresetDefaultNames)
				{
					if (name.ToLower().Equals(s.ToLower()))
					{
						MessageBox.Show($"Preset '{name}' is static and can not be altered.", "Static Preset", MessageBoxButtons.OK);
						return;
					}
				}

				var existingIndex = presets.FindIndex(p => p.Name.ToLower().Equals(name.ToLower()));
				if (existingIndex != -1)
				{
					if (MessageBox.Show($"Preset '{name}' already exists. Overwrite?",
							"Confirm Overwrite", MessageBoxButtons.YesNo) != DialogResult.Yes)
					{
						return;
					}
					presets[existingIndex] = preset;
				}
				else
				{
					presets.Add(preset);
					presetComboBox.Items.Add(name);
				}

				ColorPresetSerializer.SavePresets(presets);
			}
		}

		private void loadPresetButton_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog openFileDialog = new OpenFileDialog())
			{
				openFileDialog.Filter = "Color Preset files (*.xml)|*.xml|All files (*.*)|*.*";
				openFileDialog.FilterIndex = 1;

				if (openFileDialog.ShowDialog() == DialogResult.OK)
				{
					try
					{
						// Load the preset file
						var loadedPresets = ColorPresetSerializer.LoadPresetsFromFile(openFileDialog.FileName);
						bool presetsAdded = false;
						int addedCount = 0;
						int replacedCount = 0;
						int skippedCount = 0;

						// Add the loaded presets to existing presets, checking for duplicates
						foreach (var preset in loadedPresets)
						{
							// Check if a preset with this name already exists
							int existingIndex = presets.FindIndex(p => p.Name.Equals(preset.Name, StringComparison.OrdinalIgnoreCase));
							if (existingIndex != -1)
							{
								var result = MessageBox.Show(
									$"A preset named '{preset.Name}' already exists. Do you want to replace it?",
									"Preset Already Exists",
									MessageBoxButtons.YesNoCancel,
									MessageBoxIcon.Question);

								if (result == DialogResult.Cancel)
									return;
								else if (result == DialogResult.Yes)
								{
									presets[existingIndex] = preset;
									presetsAdded = true;
									replacedCount++;
									// Update the combo box item
									int comboIndex = existingIndex + 2; // Adjust for default presets
									presetComboBox.Items[comboIndex] = preset.Name;
								}
								else
								{
									skippedCount++;
								}
							}
							else
							{
								// Add new preset
								presets.Add(preset);
								presetComboBox.Items.Add(preset.Name);
								presetsAdded = true;
								addedCount++;
							}
						}

						if (presetsAdded)
						{
							// Save the updated presets list
							ColorPresetSerializer.SavePresets(presets);

							// Select the first imported preset if it was added
							if (loadedPresets.Count > 0)
							{
								int indexToSelect = presetComboBox.Items.IndexOf(loadedPresets[0].Name);
								if (indexToSelect != -1)
									presetComboBox.SelectedIndex = indexToSelect;
							}

							// Show summary message
							var summary = new System.Text.StringBuilder();
							summary.AppendLine("Import complete:");
							if (addedCount > 0)
								summary.AppendLine($"- {addedCount} new preset{(addedCount != 1 ? "s" : "")} added");
							if (replacedCount > 0)
								summary.AppendLine($"- {replacedCount} preset{(replacedCount != 1 ? "s" : "")} replaced");
							if (skippedCount > 0)
								summary.AppendLine($"- {skippedCount} preset{(skippedCount != 1 ? "s" : "")} skipped");
							summary.AppendLine("\nAll changes have been auto-saved.");

							MessageBox.Show(summary.ToString(), "Import Complete",
								MessageBoxButtons.OK, MessageBoxIcon.Information);
						}
						else if (skippedCount > 0)
						{
							MessageBox.Show($"All {skippedCount} preset{(skippedCount != 1 ? "s were" : " was")} skipped.\nNo changes were made.",
								"Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show("Error loading preset file: " + ex.Message, "Error",
							MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}


		private void deletePresetButton_Click(object sender, EventArgs e)
		{
			if (presetComboBox.SelectedIndex <= 1) return;

			var presetName = presetComboBox.SelectedItem.ToString();
			if (MessageBox.Show(
				$"Are you sure you want to delete the preset '{presetName}'?",
				"Confirm Delete",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question) == DialogResult.Yes)
			{
				int index = presetComboBox.SelectedIndex;
				presets.RemoveAt(index - 2);
				presetComboBox.Items.RemoveAt(index);

				try
				{
					ColorPresetSerializer.SavePresets(presets);
				}
				catch (Exception ex)
				{
					MessageBox.Show(
						$"Failed to save presets after deletion: {ex.Message}",
						"Save Error",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning
					);
				}

				// Select appropriate index after deletion
				presetComboBox.SelectedIndex = Math.Min(index - 1, presetComboBox.Items.Count - 1);

				// Update buttons state
				UpdatePresetsButtonsState();
			}
		}

		private void ApplyDefaultLightPreset()
		{
			settings.BackgroundColor = Color.FromArgb(255, 255, 255);
			settings.EditedTextColor = DarkMode.OScolors.TextActive;
			settings.SelectedColor = Color.FromArgb(240, 240, 240);
			settings.HiddenColor = Color.FromArgb(240, 240, 240);
			settings.OffsetColor = Color.FromArgb(255, 0, 0);
			settings.AddressColor = Color.FromArgb(0, 200, 0);
			settings.HexColor = Color.FromArgb(0, 0, 0);
			settings.TypeColor = Color.FromArgb(0, 0, 255);
			settings.NameColor = Color.FromArgb(32, 32, 128);
			settings.ValueColor = Color.FromArgb(255, 128, 0);
			settings.IndexColor = Color.FromArgb(32, 200, 200);
			settings.CommentColor = Color.FromArgb(0, 200, 0);
			settings.TextColor = Color.FromArgb(0, 0, 255);
			settings.VTableColor = Color.FromArgb(0, 255, 0);
			settings.PluginColor = Color.FromArgb(255, 0, 255);
			settings.ClassColor = Color.FromArgb(32, 32, 128);
		}

		private void ApplyDefaultDarkPreset(bool black = false)
		{
			if (black)
			{
				settings.BackgroundColor = Color.FromArgb(0, 0, 0);
				settings.SelectedColor = Color.FromArgb(43, 43, 43);
				settings.HiddenColor = Color.FromArgb(43, 43, 43);
			}
			else
			{
				settings.BackgroundColor = Color.FromArgb(43, 43, 43);
				settings.SelectedColor = Color.FromArgb(0, 0, 0);
				settings.HiddenColor = Color.FromArgb(0, 0, 0);
			}

			settings.EditedTextColor = DarkMode.OScolors.TextActive;
			settings.OffsetColor = Color.FromArgb(255, 0, 0);
			settings.AddressColor = Color.FromArgb(0, 200, 0);
			settings.HexColor = Color.FromArgb(255, 255, 255);
			settings.TypeColor = Color.FromArgb(5, 245, 255);
			settings.NameColor = Color.FromArgb(240, 240, 240);
			settings.ValueColor = Color.FromArgb(255, 128, 0);
			settings.IndexColor = Color.FromArgb(32, 200, 200);
			settings.CommentColor = Color.FromArgb(0, 200, 0);
			settings.TextColor = Color.FromArgb(128, 128, 128);
			settings.VTableColor = Color.FromArgb(0, 255, 0);
			settings.PluginColor = Color.FromArgb(255, 0, 255);
			settings.ClassColor = Color.FromArgb(240, 240, 240);
		}

		private void RefreshColorBindings()
		{
			foreach (Control control in nodeColorGroupBox.Controls)
			{
				if (control is ColorBox colorBox)
				{
					foreach (Binding binding in colorBox.DataBindings)
					{
						binding.ReadValue();
					}
				}
			}
			backgroundColorBox.DataBindings[nameof(ColorBox.Color)].ReadValue();
			editedTextColorColorBox.DataBindings[nameof(ColorBox.Color)].ReadValue();
		}

		private void presetComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (presetComboBox.SelectedIndex < 0) return;

			if (presetComboBox.SelectedIndex < colorPresetDefaultNames.Count())
			{
				switch (presetComboBox.SelectedIndex)
				{
					case 0:
						ApplyDefaultLightPreset();
						break;
					case 1:
						ApplyDefaultDarkPreset(false/*black*/);
						break;
					case 2:
						ApplyDefaultDarkPreset(true/*black*/);
						break;
				}
				deletePresetButton.Enabled = false;
			}
			else
			{
				var preset = presets[presetComboBox.SelectedIndex - colorPresetDefaultNames.Count()];
				preset.ApplyTo(settings);
				deletePresetButton.Enabled = true;
			}

			RefreshColorBindings();
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

		private void createAssociationButton_Click(object sender, EventArgs e)
		{
			WinUtil.RunElevated(PathUtil.LauncherExecutablePath, $"-{Constants.CommandLineOptions.FileExtRegister}");
		}

		private void removeAssociationButton_Click(object sender, EventArgs e)
		{
			WinUtil.RunElevated(PathUtil.LauncherExecutablePath, $"-{Constants.CommandLineOptions.FileExtUnregister}");
		}

		private static void SetBinding(IBindableComponent control, string propertyName, object dataSource, string dataMember)
		{
			Contract.Requires(control != null);
			Contract.Requires(propertyName != null);
			Contract.Requires(dataSource != null);
			Contract.Requires(dataMember != null);

			control.DataBindings.Add(propertyName, dataSource, dataMember, true, DataSourceUpdateMode.OnPropertyChanged);
		}

		new private void UpdateDarkMode()
		{
			foreach (Form form in Application.OpenForms)
			{
				if (form is DarkModeForm darkModeForm)
				{
					darkModeForm.UpdateDarkMode();
				}
			}
		}

		private void SetGeneralBindings()
		{
			SetBinding(stayOnTopCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.StayOnTop));
			stayOnTopCheckBox.CheckedChanged += (_, _2) => GlobalWindowManager.Windows.ForEach(w => w.TopMost = stayOnTopCheckBox.Checked);

			SetBinding(showNodeAddressCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.ShowNodeAddress));
			SetBinding(showNodeOffsetCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.ShowNodeOffset));
			SetBinding(showTextCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.ShowNodeText));
			SetBinding(highlightChangedValuesCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.HighlightChangedValues));

			SetBinding(showFloatCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.ShowCommentFloat));
			SetBinding(showIntegerCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.ShowCommentInteger));
			SetBinding(showPointerCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.ShowCommentPointer));
			SetBinding(showRttiCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.ShowCommentRtti));
			SetBinding(showSymbolsCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.ShowCommentSymbol));
			SetBinding(showStringCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.ShowCommentString));
			SetBinding(showPluginInfoCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.ShowCommentPluginInfo));
			SetBinding(runAsAdminCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.RunAsAdmin));
			SetBinding(randomizeWindowTitleCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.RandomizeWindowTitle));
			SetBinding(hideWindowIconsCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.HideWindowIcons));
			SetBinding(colorizeIconsCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.ColorizeIcons));
			SetBinding(roundedPanelsCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.RoundedPanels));
			SetBinding(enhancedCaretCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.EnhancedCaret));
			SetBinding(cppGeneratorShowOffsetCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.CppGeneratorShowOffset));
			SetBinding(cppGeneratorShowPaddingCheckBox, nameof(CheckBox.Checked), settings, nameof(Settings.CppGeneratorShowPadding));
			SetBinding(defaultPluginComboBox, nameof(ComboBox.Text), settings, nameof(Settings.DefaultPlugin));

			colorizeIconsCheckBox.CheckedChanged += (_, _2) =>
			{
				foreach (Form form in Application.OpenForms)
				{
					if (form is DarkModeForm darkModeForm)
					{
						darkModeForm.UpdateDarkMode();
					}
				}
			};

			roundedPanelsCheckBox.CheckedChanged += (_, _2) =>
			{
				foreach (Form form in Application.OpenForms)
				{
					if (form is DarkModeForm darkModeForm)
					{
						darkModeForm.UpdateDarkMode();
					}
				}
			};

			hideWindowIconsCheckBox.CheckedChanged += (_, _2) =>
			{
				foreach (Form form in Application.OpenForms)
				{
					form.ShowIcon = !hideWindowIconsCheckBox.Checked;
				}
			};
		}

		private void SetColorBindings()
		{
			SetBinding(backgroundColorBox, nameof(ColorBox.Color), settings, nameof(Settings.BackgroundColor));
			SetBinding(editedTextColorColorBox, nameof(ColorBox.Color), settings, nameof(Settings.EditedTextColor));

			SetBinding(nodeSelectedColorBox, nameof(ColorBox.Color), settings, nameof(Settings.SelectedColor));
			SetBinding(nodeHiddenColorBox, nameof(ColorBox.Color), settings, nameof(Settings.HiddenColor));
			SetBinding(nodeAddressColorBox, nameof(ColorBox.Color), settings, nameof(Settings.AddressColor));
			SetBinding(nodeOffsetColorBox, nameof(ColorBox.Color), settings, nameof(Settings.OffsetColor));
			SetBinding(nodeHexValueColorBox, nameof(ColorBox.Color), settings, nameof(Settings.HexColor));
			SetBinding(nodeTypeColorBox, nameof(ColorBox.Color), settings, nameof(Settings.TypeColor));
			SetBinding(nodeNameColorBox, nameof(ColorBox.Color), settings, nameof(Settings.NameColor));
			SetBinding(nodeValueColorBox, nameof(ColorBox.Color), settings, nameof(Settings.ValueColor));
			SetBinding(nodeIndexColorBox, nameof(ColorBox.Color), settings, nameof(Settings.IndexColor));
			SetBinding(nodeVTableColorBox, nameof(ColorBox.Color), settings, nameof(Settings.VTableColor));
			SetBinding(nodeCommentColorBox, nameof(ColorBox.Color), settings, nameof(Settings.CommentColor));
			SetBinding(nodeTextColorBox, nameof(ColorBox.Color), settings, nameof(Settings.TextColor));
			SetBinding(nodePluginColorBox, nameof(ColorBox.Color), settings, nameof(Settings.PluginColor));
			SetBinding(nodeClassColorBox, nameof(ColorBox.Color), settings, nameof(Settings.ClassColor));

			themeComboBox.SelectedIndexChanged += (s, e) =>
			{
				settings.DarkMode = (DarkModeForms.DarkModeCS.DisplayMode)themeComboBox.SelectedIndex;
				UpdateDarkMode(); // Update the theme immediately
			};
			themeComboBox.SelectedIndex = (int)settings.DarkMode; // Set initial value
		}

		private void SetTypeDefinitionBindings()
		{
			SetBinding(boolTypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeBool));
			SetBinding(int8TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeInt8));
			SetBinding(int16TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeInt16));
			SetBinding(int32TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeInt32));
			SetBinding(int64TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeInt64));
			SetBinding(nintTypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeNInt));
			SetBinding(uint8TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeUInt8));
			SetBinding(uint16TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeUInt16));
			SetBinding(uint32TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeUInt32));
			SetBinding(uint64TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeUInt64));
			SetBinding(nuintTypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeNUInt));
			SetBinding(floatTypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeFloat));
			SetBinding(doubleTypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeDouble));
			SetBinding(vector2TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeVector2));
			SetBinding(vector3TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeVector3));
			SetBinding(vector4TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeVector4));
			SetBinding(matrix3x3TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeMatrix3x3));
			SetBinding(matrix3x4TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeMatrix3x4));
			SetBinding(matrix4x4TypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeMatrix4x4));
			SetBinding(utf8TextTypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeUtf8Text));
			SetBinding(utf16TextTypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeUtf16Text));
			SetBinding(utf32TextTypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeUtf32Text));
			SetBinding(functionPtrTypeTextBox, nameof(TextBox.Text), typeMapping, nameof(CppTypeMapping.TypeFunctionPtr));
		}

		private void SetHotkeyBindings()
		{
			foreach (var kvp in settings._nodeShortcuts)
			{
				var type = kvp.Key;
				var panel = new System.Windows.Forms.Panel
				{
					AutoSize = true,
					Margin = new Padding(3),
					Padding = new Padding(3),
					Width = hotkeysFlowLayoutPanel.Width - 10
				};

				var label = new Label
				{
					Text = type.Name.Replace("Node", ""),
					AutoSize = true,
					Location = new Point(3, 8),
					Width = 120
				};

				var currentKey = new TextBox
				{
					ReadOnly = true,
					Location = new Point(130, 5),
					Width = 200,
					Text = settings.GetShortcutKeyForNodeType(type) == Keys.None ?
						  "None" :
						  settings.GetShortcutKeyForNodeType(type).ToString()
				};

				var setButton = new Button
				{
					Text = "Set",
					Location = new Point(340, 3),
					Width = 75
				};

				var clearButton = new Button
				{
					Text = "Clear",
					Location = new Point(420, 3),
					Width = 75
				};

				setButton.Click += (s, e) =>
				{
					using (var form = new HotkeyInputForm(settings, type))
					{
						if (form.ShowDialog() == DialogResult.OK)
						{
							settings.SetShortcutKeyForNodeType(type, form.HotKey);
							currentKey.Text = form.HotKey == Keys.None ? "None" : form.HotKey.ToString();

							// Rebuild toolbar and menu items
							Program.MainForm.RebuildToolbarButtons();
							Program.MainForm.RebuildTypeChangeDropDownItems();
						}
					}
				};


				clearButton.Click += (s, e) =>
				{
					settings.SetShortcutKeyForNodeType(type, Keys.None);
					currentKey.Text = "None";

					// Rebuild toolbar and menu items
					Program.MainForm.RebuildToolbarButtons();
					Program.MainForm.RebuildTypeChangeDropDownItems();
				};

				panel.Controls.Add(label);
				panel.Controls.Add(currentKey);
				panel.Controls.Add(setButton);
				panel.Controls.Add(clearButton);

				hotkeysFlowLayoutPanel.Controls.Add(panel);
			}
		}

		private void SettingsForm_Load(object sender, EventArgs e)
		{

		}
	}
}
