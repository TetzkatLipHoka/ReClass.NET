using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ReClassNET.DataExchange.ReClass.Legacy;
using ReClassNET.Nodes;
using ReClassNET.Util;

namespace ReClassNET
{
	public class Settings
	{	
		// Application Settings

		public string LastProcess { get; set; } = string.Empty;

		public bool StayOnTop { get; set; } = false;

		public bool RunAsAdmin { get; set; } = false;

		public bool RandomizeWindowTitle { get; set; } = false;

		public bool HideWindowIcons { get; set; } = false;

		public DarkModeForms.DarkModeCS.DisplayMode DarkMode { get; set; } = DarkModeForms.DarkModeCS.DisplayMode.SystemDefault;

		public bool ColorizeIcons { get; set; } = false;

		public bool RoundedPanels { get; set; } = false;

		public bool EnhancedCaret { get; set; } = false;

		public bool CppGeneratorShowOffset { get; set; } = true;

		public bool CppGeneratorShowPadding { get; set; } = true;

		public string DefaultPlugin { get; set; } = "Default";

		// Node Drawing Settings

		public bool ShowNodeAddress { get; set; } = true;

		public bool ShowNodeOffset { get; set; } = true;

		public bool ShowNodeText { get; set; } = true;

		public bool HighlightChangedValues { get; set; } = true;

		public Encoding RawDataEncoding { get; set; } = Encoding.GetEncoding(1252); /* Windows-1252 */

		// Comment Drawing Settings

		public bool ShowCommentFloat { get; set; } = true;

		public bool ShowCommentInteger { get; set; } = true;

		public bool ShowCommentPointer { get; set; } = true;

		public bool ShowCommentRtti { get; set; } = true;

		public bool ShowCommentSymbol { get; set; } = true;

		public bool ShowCommentString { get; set; } = true;

		public bool ShowCommentPluginInfo { get; set; } = true;

		// Colors

		public Color BackgroundColor { get; set; } = Color.FromArgb(255, 255, 255);

		public Color EditedTextColor { get; set; } = Color.FromArgb(0, 0, 0);

		public Color SelectedColor { get; set; } = Color.FromArgb(240, 240, 240);

		public Color HiddenColor { get; set; } = Color.FromArgb(240, 240, 240);

		public Color OffsetColor { get; set; } = Color.FromArgb(255, 0, 0);

		public Color AddressColor { get; set; } = Color.FromArgb(0, 200, 0);

		public Color HexColor { get; set; } = Color.FromArgb(0, 0, 0);

		public Color TypeColor { get; set; } = Color.FromArgb(0, 0, 255);

		public Color NameColor { get; set; } = Color.FromArgb(32, 32, 128);

		public Color ValueColor { get; set; } = Color.FromArgb(255, 128, 0);

		public Color IndexColor { get; set; } = Color.FromArgb(32, 200, 200);

		public Color CommentColor { get; set; } = Color.FromArgb(0, 200, 0);

		public Color TextColor { get; set; } = Color.FromArgb(0, 0, 255);

		public Color VTableColor { get; set; } = Color.FromArgb(0, 255, 0);

		public Color PluginColor { get; set; } = Color.FromArgb(255, 0, 255);

		public Color ClassColor { get; set; } = Color.FromArgb(32, 32, 128); 

		public CustomDataMap CustomData { get; } = new CustomDataMap();

		// HotKeys
		internal Dictionary<Type, Keys> _nodeShortcuts = new Dictionary<Type, Keys>
		{
			{ typeof(Hex8Node), Keys.Control | Keys.Shift | Keys.B },
			{ typeof(Hex16Node), 0 },
			{ typeof(Hex32Node), 0 },
			{ typeof(Hex64Node), Keys.Control | Keys.Shift | Keys.D6 },

			{ typeof(NIntNode), 0 },
			{ typeof(Int8Node), 0 },
			{ typeof(Int16Node), 0 },
			{ typeof(Int32Node), Keys.Control | Keys.Shift | Keys.I },
			{ typeof(Int64Node), 0 },

			{ typeof(NUIntNode), 0 },
			{ typeof(UInt8Node), 0 },
			{ typeof(UInt16Node), 0 },
			{ typeof(UInt32Node), 0 },
			{ typeof(UInt64Node), 0 },

			{ typeof(BoolNode), Keys.Control | Keys.Shift | Keys.O },
			{ typeof(SingleBitNode), 0 },
			{ typeof(BitFieldNode), 0 },
			{ typeof(EnumNode), Keys.Control | Keys.Shift | Keys.E },

			{ typeof(FloatNode), Keys.Control | Keys.Shift | Keys.F },
			{ typeof(DoubleNode), 0 },

			{ typeof(Vector2Node), Keys.Control | Keys.Shift | Keys.D2 },
			{ typeof(Vector3Node), Keys.Control | Keys.Shift | Keys.D3 },
			{ typeof(Vector4Node), Keys.Control | Keys.Shift | Keys.D4 },
			{ typeof(Matrix3x3Node), 0 },
			{ typeof(Matrix3x4Node), 0 },
			{ typeof(Matrix4x4Node), 0 },

			{ typeof(Utf8TextNode), 0 },
			{ typeof(Utf8TextPtrNode), 0 },
			{ typeof(Utf16TextNode), 0 },
			{ typeof(Utf16TextPtrNode), 0 },
			{ typeof(Utf32TextNode), 0 },
			{ typeof(Utf32TextPtrNode), 0 },

			{ typeof(PointerNode), Keys.Control | Keys.Shift | Keys.P },
			{ typeof(ArrayNode), 0 },
			{ typeof(UnionNode), 0 },
			{ typeof(ClassNode), 0 },
			{ typeof(ClassInstanceNode), Keys.Control | Keys.Shift | Keys.C },
			{ typeof(ClassInstanceArrayNode), 0 },
			{ typeof(VirtualMethodTableNode), Keys.Control | Keys.Shift | Keys.V },
			{ typeof(VirtualMethodNode), 0 },
			{ typeof(FunctionNode), 0 },
			{ typeof(FunctionPtrNode), 0 }
		};

		public Keys GetShortcutKeyForNodeType(Type nodeType)
		{
			return !_nodeShortcuts.TryGetValue(nodeType, out var shortcutKeys) ? Keys.None : shortcutKeys;
		}

		public void SetShortcutKeyForNodeType(Type nodeType, Keys shortCut)
		{
			if (_nodeShortcuts.ContainsKey(nodeType))
			{
				_nodeShortcuts[nodeType] = shortCut;
			}
		}

		public bool IsHotkeyInUse(Keys hotkey, Type excludeType = null)
		{
			if (hotkey == Keys.None)
				return false;

			foreach (var kvp in _nodeShortcuts)
			{
				if (kvp.Key != excludeType && kvp.Value == hotkey)
				{
					return true;
				}
			}
			return false;
		}

		public string GetHotkeyOwner(Keys hotkey)
		{
			foreach (var kvp in _nodeShortcuts)
			{
				if (kvp.Value == hotkey)
				{
					return kvp.Key.Name;
				}
			}
			return null;
		}

		public Settings Clone() => MemberwiseClone() as Settings;
	}
}
