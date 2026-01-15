using System.Drawing;
using ReClassNET.Controls;
using ReClassNET.Memory;
using ReClassNET.UI;

namespace ReClassNET.Nodes
{
	public class Hex8Node : BaseHexNode
	{
		public override int MemorySize => 1;

		public override void GetUserInterfaceInfo(out string name, out Image icon)
		{
			name = "Hex8";
			icon = Properties.Resources.B16x16_Button_Hex_8;
		}

		public override string GetToolTipText(HotSpot spot)
		{
      /*
			var b = spot.Memory.ReadUInt8(Offset);
			return $"Int8: {(int)b}\nUInt8: 0x{b:X02}";
      */      
			var value = ReadFromBuffer(spot.Memory, Offset);
			return $"Int8: {value.SByteValue}\nUInt8: 0x{value.ByteValue:X02}";
		}

		public override Size Draw(DrawContext context, int x, int y)
		{
			return Draw(context, x, y, context.Settings.ShowNodeText ? context.Memory.ReadString(context.Settings.RawDataEncoding, Offset, 1) + "        " : null, 1);
		}

		public override void Update(HotSpot spot)
		{
			Update(spot, 1);
		}
    
		protected override int AddComment(DrawContext context, int x, int y)
		{
			x = base.AddComment(context, x, y);

			var value = ReadFromBuffer(context.Memory, Offset);

			if (context.Settings.ShowCommentInteger)
			{
				if (value.ByteValue == 0)
				{
					x = AddText(context, x, y, context.Settings.ValueColor, HotSpot.ReadOnlyId, "0") + context.Font.Width;
				}
				else
				{
					x = AddText(context, x, y, context.Settings.ValueColor, HotSpot.ReadOnlyId, value.SByteValue.ToString()) + context.Font.Width;
					x = AddText(context, x, y, context.Settings.ValueColor, HotSpot.ReadOnlyId, $"0x{value.ByteValue:X}") + context.Font.Width;
				}
			}

			return x;
		}

		private static UInt8Data ReadFromBuffer(MemoryBuffer memory, int offset) => new UInt8Data
		{
			ByteValue = memory.ReadUInt8(offset)
		};
	}
}
