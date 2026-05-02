using System.Drawing;
using System.Text;
using ReClassNET.Controls;

namespace ReClassNET.Nodes
{
	public class DefaultTextNode : BaseTextNode
	{
		public override Encoding Encoding => Encoding.Default;

		public override void GetUserInterfaceInfo(out string name, out Image icon)
		{
			name = "System Text";
			icon = Properties.Resources.B16x16_Button_Text;
		}

		public override Size Draw(DrawContext context, int x, int y)
		{
			return DrawText(context, x, y, "Text");
		}
	}
}
