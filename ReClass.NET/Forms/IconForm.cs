using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using DarkModeForms;

namespace ReClassNET.Forms
{
	public class IconForm : DarkModeForm
	{
		private Icon icon;

		new public Icon Icon
		{
			get => icon;
			set
			{
				icon = value;
				if (IsHandleCreated)
				{
					SetIcon();
				}
			}
		}

		protected override void OnHandleCreated(System.EventArgs e)
		{
			base.OnHandleCreated(e);

			SetIcon();
		}

		private void SetIcon()
		{
			if (Icon != null)
			{
				const int WM_SETICON = 0x80;
				SendMessage(Handle, WM_SETICON, (IntPtr)0, Icon.Handle);
				SendMessage(Handle, WM_SETICON, (IntPtr)1, Icon.Handle);
			}
		}
   
		public IconForm()
		{
			if (!DesignMode && Program.Settings != null)
			{
				if (Program.Settings.HideWindowIcons)
				{
					ShowIcon = false;
				}
				else
				{
					ShowIcon = true;
				}
			}
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
	}
}