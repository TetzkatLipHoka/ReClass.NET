﻿using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using ReClassNET.UI;

namespace ReClassNET.Nodes
{
	class Vector3Node : BaseMatrixNode
	{
		[StructLayout(LayoutKind.Explicit)]
		struct Vector3Data
		{
			[FieldOffset(0)]
			public float X;
			[FieldOffset(4)]
			public float Y;
			[FieldOffset(8)]
			public float Z;
		}

		/// <summary>Size of the node in bytes.</summary>
		public override int MemorySize => 3 * 4;

		/// <summary>Draws this node.</summary>
		/// <param name="view">The view information.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <returns>The height the node occupies.</returns>
		public override int Draw(ViewInfo view, int x2, int y2)
		{
			Contract.Requires(view != null);

			return DrawVectorType(view, x2, y2, "Vector3", (ref int x, ref int y) =>
			{
				var value = view.Memory.ReadObject<Vector3Data>(Offset);

				x = AddText(view, x, y, Program.Settings.NameColor, HotSpot.NoneId, "(");
				x = AddText(view, x, y, Program.Settings.ValueColor, 0, $"{value.X:0.000}");
				x = AddText(view, x, y, Program.Settings.NameColor, HotSpot.NoneId, ",");
				x = AddText(view, x, y, Program.Settings.ValueColor, 1, $"{value.Y:0.000}");
				x = AddText(view, x, y, Program.Settings.NameColor, HotSpot.NoneId, ",");
				x = AddText(view, x, y, Program.Settings.ValueColor, 2, $"{value.Z:0.000}");
				x = AddText(view, x, y, Program.Settings.NameColor, HotSpot.NoneId, ")");
			});
		}

		public override void Update(HotSpot spot)
		{
			Contract.Requires(spot != null);

			base.Update(spot);

			Update(spot, 3);
		}
	}
}
