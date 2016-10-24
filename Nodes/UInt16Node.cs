﻿using System.Diagnostics.Contracts;
using ReClassNET.UI;

namespace ReClassNET.Nodes
{
	class UInt16Node : BaseNumericNode
	{
		/// <summary>Size of the node in bytes.</summary>
		public override int MemorySize => 2;

		/// <summary>Draws this node.</summary>
		/// <param name="view">The view information.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <returns>The height the node occupies.</returns>
		public override int Draw(ViewInfo view, int x, int y)
		{
			Contract.Requires(view != null);

			return DrawNumeric(view, x, y, Icons.Unsigned, "UInt16", view.Memory.ReadObject<ushort>(Offset).ToString());
		}

		/// <summary>Updates the node from the given spot. Sets the value of the node.</summary>
		/// <param name="spot">The spot.</param>
		public override void Update(HotSpot spot)
		{
			Contract.Requires(spot != null);

			base.Update(spot);

			if (spot.Id == 0)
			{
				ushort val;
				if (ushort.TryParse(spot.Text, out val))
				{
					spot.Memory.Process.WriteRemoteMemory(spot.Address, val);
				}
			}
		}
	}
}
