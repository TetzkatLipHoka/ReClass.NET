using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using ReClassNET.Controls;
using ReClassNET.Extensions;
using ReClassNET.Forms;
using ReClassNET.Memory;
using ReClassNET.UI;
using ReClassNET.Util;

namespace ReClassNET.Nodes
{
	public class BitFieldNode : BaseContainerNode
	{
		private int size;
		private int bits;
		public BaseNode InnerNode;
		/// <summary>Gets or sets the bit count.</summary>
		/// <value>Possible values: 64, 32, 16, 8</value>
		public int Bits
		{
			get => bits;
			set
			{
				Contract.Ensures(bits > 0);
				Contract.Ensures(size > 0);

				if (value >= 64)
				{
					bits = 64;
				}
				else if (value >= 32)
				{
					bits = 32;
				}
				else if (value >= 16)
				{
					bits = 16;
				}
				else
				{
					bits = 8;
				}

				size = bits / 8;
			}
		}

		public override int MemorySize => size;

		protected override bool ShouldCompensateSizeChanges => false;

		public BitFieldNode()
		{
			Bits = IntPtr.Size * 8;

			LevelsOpen.DefaultValue = true;
		}
		public override void Initialize()
		{
			if (InnerNode == null)
			{
				var boolnode = new BoolNode();
				boolnode.Initialize();
				CopyFromNode(boolnode);
			}
		}

		public override bool CanHandleChildNode(BaseNode node)
		{
			if (node.MemorySize <= InnerNode.MemorySize)
				return bChildNodeChangeAllowed;
			return false;
		}
		public override void GetUserInterfaceInfo(out string name, out Image icon)
		{
			name = "Bitfield";
			icon = Properties.Resources.B16x16_Button_Bits;
		}

		public override void CopyFromNode(BaseNode node)
		{
			base.CopyFromNode(node);
			if (CanChangeInnerNodeTo(node))
				InnerNode = node;
			else
				InnerNode = new BoolNode(); //Default to bool
			Bits = InnerNode.MemorySize * 8;
			for (int i = 0; i < Bits; i++)
			{
				var bitfield = new SingleBitNode();
				bitfield.BitCap = Bits;
				bitfield.bParentIsBoolean = node is BoolNode;
				AddNode(bitfield);
				bitfield.Name = $"_{Offset:X}_bit{bitfield.Offset}b{bitfield.BitStart}";
			}
			
		}
		public override void UpdateOffsets()
		{
			int count = 0;
			foreach (var subnode in Nodes)
			{
				subnode.Offset = count / 8;
				if (subnode is SingleBitNode singlebitnode)
				{
					singlebitnode.BitStart = count;
				}
				count+= subnode.MemorySize;
			}
		}
		public void UpdateChildNodes()
		{
			int count = 0;
			foreach (var subnode in Nodes)
			{
				subnode.Offset = count / 8;
				if (subnode is SingleBitNode singlebitnode)
				{
					singlebitnode.BitStart = count;
					singlebitnode.bParentIsBoolean = InnerNode is BoolNode;
					singlebitnode.BitCap = Bits;
				}
				count+= subnode.MemorySize;
			}
		}
		public override void AddBytes(int newsize)
		{
			if (Nodes.Count < Bits)
			{
				newsize = Math.Min(newsize, Bits - Nodes.Count);
				for (int i = 0; i < newsize; i++)
				{
					var bitfield = new SingleBitNode();
					bitfield.BitCap = Bits;
					bitfield.bParentIsBoolean = InnerNode is BoolNode;
					AddNode(bitfield);
					bitfield.Name = $"_{Offset:X}_bit{bitfield.Offset}b{bitfield.BitStart}";
				}
			}
		}
		public void ResizeInternals()
		{
			if (InnerNode == null)
				throw new InvalidOperationException("Inner Type cannot be null");
			if (Bits != Nodes.Count)
			{
				Bits = InnerNode.MemorySize * 8;
				ClearNodes();
				AddBytes(Bits);
				return;
			}
			Bits = InnerNode.MemorySize * 8;
			if (Nodes.Count > Bits)
			{
				//delete extra bits
				foreach(BaseNode node in Nodes.Reverse())
				{
					if (Nodes.Count == Bits)
						break;
					RemoveNode(node);
				}
			}
			else
				AddBytes(Bits - Nodes.Count);
			UpdateChildNodes();
		}
		/// <summary>
		/// Gets the underlaying node for the bit field.
		/// </summary>
		/// <returns></returns>
		public BaseNode GetUnderlayingNode()
		{
			return InnerNode;
		}

		/// <summary>Converts the memory value to a bit string.</summary>
		/// <param name="memory">The process memory.</param>
		/// <returns>The value converted to a bit string.</returns>
		private string ConvertValueToBitString(MemoryBuffer memory)
		{
			Contract.Requires(memory != null);
			Contract.Ensures(Contract.Result<string>() != null);

			switch(bits)
			{
				case 64:
					return BitString.ToString(memory.ReadInt64(Offset));
				case 32:
					return BitString.ToString(memory.ReadInt32(Offset));
				case 16:
					return BitString.ToString(memory.ReadInt16(Offset));
				default:
					return BitString.ToString(memory.ReadUInt8(Offset));
			}
		}

		public override Size Draw(DrawContext context, int x, int y)
		{
			const int BitsPerBlock = 4;

			if (IsHidden && !IsWrapped)
			{
				return DrawHidden(context, x, y);
			}

			var origX = x;
			var origY = y;

			AddSelection(context, x, y, context.Font.Height);

			x = AddOpenCloseIcon(context, x, y);
			x = AddIconPadding(context, x);
			//x = AddIconPadding(context, x);
			var subx = x;

			x = AddAddressOffset(context, x, y);


			x = AddText(context, x, y, context.Settings.TypeColor, HotSpot.NoneId, "Bits") + context.Font.Width;
			if (!IsWrapped)
			{
				x = AddText(context, x, y, context.Settings.NameColor, HotSpot.NameId, Name) + context.Font.Width;
			}


			var tx = subx - 3;

			for (var i = 0; i < bits; ++i)
			{
				var rect = new Rectangle(x + (i + i / BitsPerBlock) * context.Font.Width, y, context.Font.Width, context.Font.Height);
				AddHotSpot(context, rect, string.Empty, i, HotSpotType.Edit);
			}

			var value = ConvertValueToBitString(context.Memory);

			x = AddText(context, x, y, context.Settings.ValueColor, HotSpot.NoneId, value) + context.Font.Width;
			x = AddIcon(context, x + 2, y, context.IconProvider.Change, 1, HotSpotType.Click);

			x += context.Font.Width;

			x = AddComment(context, x, y);

			DrawInvalidMemoryIndicatorIcon(context, y);
			AddContextDropDownIcon(context, y);
			AddDeleteIcon(context, y);

			if (LevelsOpen[context.Level])
			{
				y += context.Font.Height;
				var size = new Size(subx - origX, y - origY);

				var childOffset = tx - origX;

				var innerContext = context.Clone();
				innerContext.Level++;
				innerContext.Address = context.Address + Offset;
				innerContext.Memory = context.Memory.Clone();
				innerContext.Memory.Offset += Offset;
				int doneBits = 0;
				foreach (var node in Nodes)
				{
					if (node is SingleBitNode sbn)
					{
						sbn.BitStart = doneBits;
						doneBits += sbn.BitCount;
						if (doneBits > Bits)
							continue;
					}
					Size AggregateNodeSizes(Size baseSize, Size newSize)
					{
						return new Size(Math.Max(baseSize.Width, newSize.Width), baseSize.Height + newSize.Height);
					}

					Size ExtendWidth(Size baseSize, int width)
					{
						return new Size(baseSize.Width + width, baseSize.Height);
					}

					// Draw the node if it is in the visible area.
					if (context.ClientArea.Contains(tx, y))
					{
						var innerSize = node.Draw(innerContext, tx, y);

						size = AggregateNodeSizes(size, ExtendWidth(innerSize, childOffset));

						y += innerSize.Height;
					}
					else
					{
						// Otherwise calculate the height...
						var calculatedHeight = node.CalculateDrawnHeight(innerContext);

						// and check if the node area overlaps with the visible area...
						if (new Rectangle(tx, y, 9999999, calculatedHeight).IntersectsWith(context.ClientArea))
						{
							// then draw the node...
							var innerSize = node.Draw(innerContext, tx, y);

							size = AggregateNodeSizes(size, ExtendWidth(innerSize, childOffset));

							y += innerSize.Height;
						}
						else
						{
							// or skip drawing and just use the calculated height.
							size = AggregateNodeSizes(size, new Size(0, calculatedHeight));

							y += calculatedHeight;
						}
					}
				}
				y -= 8;
			}

			return new Size(x - origX, y - origY + context.Font.Height);
		}

		public override int CalculateDrawnHeight(DrawContext context)
		{
			if (IsHidden && !IsWrapped)
			{
				return HiddenHeight;
			}

			var height = context.Font.Height;
			if (LevelsOpen[context.Level])
			{
				var nv = context.Clone();
				height += context.Font.Height + 8;
				height += Nodes.Sum(n => n.CalculateDrawnHeight(nv));
			}
			return height;
		}
		private bool CanChangeInnerNodeTo(BaseNode node)
		{
			switch (node)
			{
				case FloatNode:
				case DoubleNode:
					return false;
				case BaseNumericNode:
				case BaseHexNode:
					return true;
			}

			return false;
		}
		private bool CanChangeInnerNodeTo(Type nodetype)
		{
			if (nodetype == typeof(FloatNode) || nodetype == typeof(DoubleNode) || nodetype == typeof(SingleBitNode))
				return false;
			else if (nodetype.IsSubclassOf(typeof(BaseNumericNode)) || nodetype.IsSubclassOf(typeof(BaseHexNode)))
				return true;

			return false;
		}
		private void ChangeInnerNode(BaseNode node)
		{
			if (!CanChangeInnerNodeTo(node))
			{
				throw new InvalidOperationException($"Can't change inner node to '{node?.GetType().ToString() ?? "null"}'");
			}

			if (InnerNode != node)
			{
				InnerNode = node;

				if (node != null)
				{
					node.ParentNode = this;
				}
				GetParentContainer()?.ChildHasChanged(this);
			}
		}
		public override void Update(HotSpot spot)
		{
			base.Update(spot);
			if (spot.Id == 1)
			{
				var items = NodeTypesBuilder.CreateToolStripMenuItems(t =>
				{
					var node = BaseNode.CreateInstanceFromType(t);
					if (CanChangeInnerNodeTo(node))
					{
						ChangeInnerNode(node);
						ResizeInternals();
					}
				}, false);
				var menu = new ContextMenuStrip();
				List<ToolStripItem> list = new List<ToolStripItem>();
				foreach (var item in items)
				{
					if ((item is TypeToolStripMenuItem typeitem && CanChangeInnerNodeTo(typeitem.Value)))
					{
						list.Add(item);
					}
				}
				menu.Items.AddRange(list.ToArray());
				
				menu.Show(MainForm.MousePosition);
			}

		}
	}
}
