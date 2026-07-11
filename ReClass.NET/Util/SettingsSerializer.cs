using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ReClassNET.Util
{
	internal sealed class SettingsSerializer
	{
		private const string XmlRootElement = "Settings";
		private const string XmlGeneralElement = "General";
		private const string XmlDisplayElement = "Display";
		private const string XmlColorsElement = "Colors";
		private const string XmlCustomDataElement = "CustomData";
		private const string XmlHotkeysElement = "NodeHotkeys";

		#region Read Settings

		public static Settings Load()
		{
			MigrateSettingsIfNeeded(); //due to path changes from local to roaming, added this auto migration

			EnsureSettingsDirectoryAvailable();

			var settings = new Settings();

			try
			{
				var path = Path.Combine(PathUtil.SettingsFolderPath, Constants.SettingsFile);

				using var sr = new StreamReader(path);

				var document = XDocument.Load(sr);
				var root = document.Root;

				var general = root?.Element(XmlGeneralElement);
				if (general != null)
				{
					XElementSerializer.TryRead(general, nameof(settings.LastProcess), e => settings.LastProcess = XElementSerializer.ToString(e));
					XElementSerializer.TryRead(general, nameof(settings.StayOnTop), e => settings.StayOnTop = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(general, nameof(settings.RunAsAdmin), e => settings.RunAsAdmin = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(general, nameof(settings.RandomizeWindowTitle), e => settings.RandomizeWindowTitle = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(general, nameof(settings.HideWindowIcons), e => settings.HideWindowIcons = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(general, nameof(settings.DarkMode), e => settings.DarkMode = (DarkModeForms.DarkModeCS.DisplayMode)int.Parse(XElementSerializer.ToString(e)));
					XElementSerializer.TryRead(general, nameof(settings.ColorizeIcons), e => settings.ColorizeIcons = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(general, nameof(settings.RoundedPanels), e => settings.RoundedPanels = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(general, nameof(settings.EnhancedCaret), e => settings.EnhancedCaret = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(general, nameof(settings.CppGeneratorShowOffset), e => settings.CppGeneratorShowOffset = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(general, nameof(settings.CppGeneratorShowPadding), e => settings.CppGeneratorShowPadding = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(general, nameof(settings.DefaultPlugin), e => settings.DefaultPlugin = XElementSerializer.ToString(e));
					// Load hotkeys
					XElementSerializer.TryRead(general, "NodeHotkeys", e =>
					{
						foreach (var hotkeyElement in e.Elements("Hotkey"))
						{
							var typeStr = hotkeyElement.Attribute("Type")?.Value;
							var keyStr = hotkeyElement.Attribute("Key")?.Value;

							if (string.IsNullOrEmpty(typeStr) || string.IsNullOrEmpty(keyStr))
								continue;

							var type = Type.GetType(typeStr);
							if (type != null && Enum.TryParse(keyStr, out Keys key))
							{
								settings.SetShortcutKeyForNodeType(type, key);
							}
						}
					});
				}
				var display = root?.Element(XmlDisplayElement);
				if (display != null)
				{
					XElementSerializer.TryRead(display, nameof(settings.ShowNodeAddress), e => settings.ShowNodeAddress = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(display, nameof(settings.ShowNodeOffset), e => settings.ShowNodeOffset = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(display, nameof(settings.ShowNodeText), e => settings.ShowNodeText = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(display, nameof(settings.HighlightChangedValues), e => settings.HighlightChangedValues = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(display, nameof(settings.ShowCommentFloat), e => settings.ShowCommentFloat = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(display, nameof(settings.ShowCommentInteger), e => settings.ShowCommentInteger = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(display, nameof(settings.ShowCommentPointer), e => settings.ShowCommentPointer = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(display, nameof(settings.ShowCommentRtti), e => settings.ShowCommentRtti = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(display, nameof(settings.ShowCommentSymbol), e => settings.ShowCommentSymbol = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(display, nameof(settings.ShowCommentString), e => settings.ShowCommentString = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(display, nameof(settings.ShowCommentPluginInfo), e => settings.ShowCommentPluginInfo = XElementSerializer.ToBool(e));
					XElementSerializer.TryRead(display, nameof(settings.RawDataEncoding), e => {
						try { settings.RawDataEncoding = Encoding.GetEncoding(XElementSerializer.ToInt(e)); } catch { /* ignored */ }
					});
				}
				var colors = root?.Element(XmlColorsElement);
				if (colors != null)
				{
					XElementSerializer.TryRead(colors, nameof(settings.BackgroundColor), e => settings.BackgroundColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.EditedTextColor), e => settings.EditedTextColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.SelectedColor), e => settings.SelectedColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.HiddenColor), e => settings.HiddenColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.OffsetColor), e => settings.OffsetColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.AddressColor), e => settings.AddressColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.HexColor), e => settings.HexColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.TypeColor), e => settings.TypeColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.NameColor), e => settings.NameColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.ValueColor), e => settings.ValueColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.IndexColor), e => settings.IndexColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.CommentColor), e => settings.CommentColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.TextColor), e => settings.TextColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.VTableColor), e => settings.VTableColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.PluginColor), e => settings.PluginColor = XElementSerializer.ToColor(e));
					XElementSerializer.TryRead(colors, nameof(settings.ClassColor), e => settings.ClassColor = XElementSerializer.ToColor(e));
				}
				var customData = root?.Element(XmlCustomDataElement);
				if (customData != null)
				{
					settings.CustomData.Deserialize(customData);
				}
				var hotkeys = root?.Element(XmlHotkeysElement);
				if (hotkeys != null)
				{
					foreach (var hotkeyElement in hotkeys.Elements("Hotkey"))
					{
						var typeStr = hotkeyElement.Attribute("Type")?.Value;
						var keyStr = hotkeyElement.Attribute("Key")?.Value;

						if (string.IsNullOrEmpty(typeStr) || string.IsNullOrEmpty(keyStr))
							continue;

						var type = Type.GetType(typeStr);
						if (type != null)
						{
							if (keyStr == "None")
							{
								settings.SetShortcutKeyForNodeType(type, Keys.None);
							}
							else if (Enum.TryParse(keyStr, true, out Keys key))
							{
								settings.SetShortcutKeyForNodeType(type, key);
							}
						}
					}
				}
			}
			catch
			{
				// ignored
			}

			return settings;
		}

		#endregion

		#region Write Settings

		public static void Save(Settings settings)
		{
			Contract.Requires(settings != null);

			EnsureSettingsDirectoryAvailable();

			var path = Path.Combine(PathUtil.SettingsFolderPath, Constants.SettingsFile);

			using var sw = new StreamWriter(path);

			var document = new XDocument(
				new XComment($"{Constants.ApplicationName} {Constants.ApplicationVersion} by {Constants.Author}"),
				new XComment($"Website: {Constants.HomepageUrl}"),
				new XElement(
					XmlRootElement,
					new XElement(
						XmlGeneralElement,
						XElementSerializer.ToXml(nameof(settings.LastProcess), settings.LastProcess),
						XElementSerializer.ToXml(nameof(settings.StayOnTop), settings.StayOnTop),
						XElementSerializer.ToXml(nameof(settings.RunAsAdmin), settings.RunAsAdmin),
						XElementSerializer.ToXml(nameof(settings.RandomizeWindowTitle), settings.RandomizeWindowTitle),
            XElementSerializer.ToXml(nameof(settings.HideWindowIcons), settings.HideWindowIcons),
						XElementSerializer.ToXml(nameof(settings.DarkMode), (int)settings.DarkMode),
						XElementSerializer.ToXml(nameof(settings.ColorizeIcons), settings.ColorizeIcons),
						XElementSerializer.ToXml(nameof(settings.RoundedPanels), settings.RoundedPanels),
						XElementSerializer.ToXml(nameof(settings.EnhancedCaret), settings.EnhancedCaret),
						XElementSerializer.ToXml(nameof(settings.CppGeneratorShowOffset), settings.CppGeneratorShowOffset),
						XElementSerializer.ToXml(nameof(settings.CppGeneratorShowPadding), settings.CppGeneratorShowPadding),
						XElementSerializer.ToXml(nameof(settings.DefaultPlugin), settings.DefaultPlugin)
					),
					new XElement(
						XmlDisplayElement,
						XElementSerializer.ToXml(nameof(settings.ShowNodeAddress), settings.ShowNodeAddress),
						XElementSerializer.ToXml(nameof(settings.ShowNodeOffset), settings.ShowNodeOffset),
						XElementSerializer.ToXml(nameof(settings.ShowNodeText), settings.ShowNodeText),
						XElementSerializer.ToXml(nameof(settings.HighlightChangedValues), settings.HighlightChangedValues),
						XElementSerializer.ToXml(nameof(settings.ShowCommentFloat), settings.ShowCommentFloat),
						XElementSerializer.ToXml(nameof(settings.ShowCommentInteger), settings.ShowCommentInteger),
						XElementSerializer.ToXml(nameof(settings.ShowCommentPointer), settings.ShowCommentPointer),
						XElementSerializer.ToXml(nameof(settings.ShowCommentRtti), settings.ShowCommentRtti),
						XElementSerializer.ToXml(nameof(settings.ShowCommentSymbol), settings.ShowCommentSymbol),
						XElementSerializer.ToXml(nameof(settings.ShowCommentString), settings.ShowCommentString),
						XElementSerializer.ToXml(nameof(settings.ShowCommentPluginInfo), settings.ShowCommentPluginInfo),
						XElementSerializer.ToXml(nameof(settings.RawDataEncoding), settings.RawDataEncoding.CodePage)
					),
					new XElement(
						XmlColorsElement,
						XElementSerializer.ToXml(nameof(settings.BackgroundColor), settings.BackgroundColor),
						XElementSerializer.ToXml(nameof(settings.EditedTextColor), settings.EditedTextColor),
						XElementSerializer.ToXml(nameof(settings.SelectedColor), settings.SelectedColor),
						XElementSerializer.ToXml(nameof(settings.HiddenColor), settings.HiddenColor),
						XElementSerializer.ToXml(nameof(settings.OffsetColor), settings.OffsetColor),
						XElementSerializer.ToXml(nameof(settings.AddressColor), settings.AddressColor),
						XElementSerializer.ToXml(nameof(settings.HexColor), settings.HexColor),
						XElementSerializer.ToXml(nameof(settings.TypeColor), settings.TypeColor),
						XElementSerializer.ToXml(nameof(settings.NameColor), settings.NameColor),
						XElementSerializer.ToXml(nameof(settings.ValueColor), settings.ValueColor),
						XElementSerializer.ToXml(nameof(settings.IndexColor), settings.IndexColor),
						XElementSerializer.ToXml(nameof(settings.CommentColor), settings.CommentColor),
						XElementSerializer.ToXml(nameof(settings.TextColor), settings.TextColor),
						XElementSerializer.ToXml(nameof(settings.VTableColor), settings.VTableColor),
						XElementSerializer.ToXml(nameof(settings.PluginColor), settings.PluginColor),
						XElementSerializer.ToXml(nameof(settings.ClassColor), settings.ClassColor)
					),
					// Save hotkeys
					settings.CustomData.Serialize(XmlCustomDataElement),
					new XElement(XmlHotkeysElement,
						settings._nodeShortcuts.Select(kvp =>
							new XElement("Hotkey",
								new XAttribute("Type", kvp.Key.AssemblyQualifiedName),
								new XAttribute("Key", kvp.Value.ToString())
							)
						)
					)
				)
			);

			document.Save(sw);
		}

		#endregion

		private static void MigrateSettingsIfNeeded()
		{
			try
			{
				// Get the old (Local) and new (Roaming) paths
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string oldPath = Path.Combine(localAppData, Constants.ApplicationName);

				// If old directory doesn't exist, no migration needed
				if (!Directory.Exists(oldPath)) return;

				// Ensure new directory exists
				EnsureSettingsDirectoryAvailable();

				// Check for files to migrate
				foreach (var filePath in Directory.EnumerateFileSystemEntries(oldPath))
				{
					string fileName = Path.GetFileName(filePath);
					string path = Path.Combine(oldPath, fileName);
					if (File.Exists(path))
					{
						string newPath = Path.Combine(PathUtil.SettingsFolderPath, fileName);
						if (!File.Exists(newPath))
						{
							File.Copy(path, newPath, false);
							try
							{
								File.Delete(path);
							}
							catch
							{
								// Ignore deletion errors
							}
						}
					}
				}

				// Optionally delete old files after successful migration
				try
				{
					if (Directory.Exists(oldPath) && !Directory.EnumerateFileSystemEntries(oldPath).Any())
						Directory.Delete(oldPath);
				}
				catch
				{
					// Ignore deletion errors
				}
			}
			catch
			{
				// Ignore migration errors - worst case, user starts with fresh settings
			}
		}

		private static void EnsureSettingsDirectoryAvailable()
		{
			try
			{
				if (Directory.Exists(PathUtil.SettingsFolderPath) == false)
				{
					Directory.CreateDirectory(PathUtil.SettingsFolderPath);
				}
			}
			catch
			{
				// ignored
			}
		}
	}
}
