using System.Drawing;
using ReClassNET.Controls;
using ReClassNET.Memory;
using ReClassNET.UI;

namespace ReClassNET.Nodes
{
	public class Hex16Node : BaseHexNode
	{
		public override int MemorySize => 2;

		public override void GetUserInterfaceInfo(out string name, out Image icon)
		{
			name = "Hex16";
			icon = Properties.Resources.B16x16_Button_Hex_16;
		}

		public override string GetToolTipText(HotSpot spot)
		{
			//var value = new UInt16Data { ShortValue = spot.Memory.ReadInt16(Offset) };
			var value = ReadFromBuffer(spot.Memory, Offset);

			return $"Int16: {value.ShortValue}\nUInt16: 0x{value.UShortValue:X04}";
		}

		public override Size Draw(DrawContext context, int x, int y)
		{
			return Draw(context, x, y, context.Settings.ShowNodeText ? context.Memory.ReadString(context.Settings.RawDataEncoding, Offset, 2) + "       " : null, 2);
		}

		public override void Update(HotSpot spot)
		{
			Update(spot, 2);
		}
    
	protected override int AddComment(DrawContext context, int x, int y)
		{
			x = base.AddComment(context, x, y);

			var value = ReadFromBuffer(context.Memory, Offset);

			if (context.Settings.ShowCommentInteger)
			{ 
				if (value.ShortValue == 0)
				{
					x = AddText(context, x, y, context.Settings.ValueColor, HotSpot.ReadOnlyId, "0") + context.Font.Width;
				}
				else
				{
					x = AddText(context, x, y, context.Settings.ValueColor, HotSpot.ReadOnlyId, value.ShortValue.ToString()) + context.Font.Width;
					x = AddText(context, x, y, context.Settings.ValueColor, HotSpot.ReadOnlyId, $"0x{value.UShortValue:X}") + context.Font.Width;
				}
			}

			return x;
		}

		private static UInt16Data ReadFromBuffer(MemoryBuffer memory, int offset) => new UInt16Data
		{
			ShortValue = memory.ReadInt16(offset)
		};
	}
}
