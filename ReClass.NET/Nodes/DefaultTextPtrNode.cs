using System.Drawing;
using System.Text;
using ReClassNET.Controls;

namespace ReClassNET.Nodes
{
	public class DefaultTextPtrNode : BaseTextPtrNode
	{
		public override Encoding Encoding => Encoding.Default;

		public override void GetUserInterfaceInfo(out string name, out Image icon)
		{
			name = "System Text Pointer";
			icon = Properties.Resources.B16x16_Button_Text_Pointer;
		}

		public override Size Draw(DrawContext context, int x, int y)
		{
			return DrawText(context, x, y, "TextPtr");
		}
	}
}
