using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using ColorCode;
using ColorCode.Parsing;
using ColorCode.Styling;
using ReClassNET.CodeGenerator;
using ReClassNET.Extensions;
using ReClassNET.Logger;
using ReClassNET.Nodes;
using ReClassNET.Project;
using ReClassNET.UI;
using ReClassNET.Util;
using ReClassNET.Util.Rtf;

namespace ReClassNET.Forms
{
	public partial class CodeForm : IconForm
	{
		public CodeForm(ICodeGenerator generator, IReadOnlyList<ClassNode> classes, IReadOnlyList<EnumDescription> enums, ILogger logger)
		{
			Contract.Requires(generator != null);
			Contract.Requires(classes != null);
			Contract.Requires(enums != null);

			InitializeComponent();

			codeRichTextBox.SetInnerMargin(5, 5, 5, 5);

			var code = generator.GenerateCode(classes, enums, logger);

			RtfFormatter formatter = new();
			// C# uses the C# grammar; C/C++/Rust/Pascal all fall back to the C++ grammar.
			codeRichTextBox.Rtf =
				formatter.GetFormattedOutput(code, (generator.Language is Language.CSharp ? LegacyCSharp.Instance : LegacyCpp.Instance));
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			GlobalWindowManager.AddWindow(this);
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			base.OnFormClosed(e);

			GlobalWindowManager.RemoveWindow(this);
		}
	}

	internal sealed class RtfFormatter(StyleDictionary Styles = null, ILanguageParser languageParser = null) : CodeColorizerBase(Styles, languageParser)
	{
		private readonly RtfBuilder builder = new RtfBuilder(RtfFont.Consolas, 20);

		private static Color ParseFromHex(string color)
		{
			/* Trim leading hashtag. */
			ReadOnlySpan<char> hex = color.AsSpan(1);

			byte a = 255;
			/* If `Length` is 8, assume we were given a color in ARGB format. */
			if (hex.Length is 8)
			{
				a = byte.Parse(hex[0..2], NumberStyles.HexNumber);
				hex = hex[2..];
			}

			byte r = byte.Parse(hex[0..2], NumberStyles.HexNumber);
			byte g = byte.Parse(hex[2..4], NumberStyles.HexNumber);
			byte b = byte.Parse(hex[4..6], NumberStyles.HexNumber);

			return Color.FromArgb(a, r, g, b);
		}

		protected override void Write(string parsedSourceCode, IList<Scope> scopes)
		{
			if (scopes.Any())
			{
				builder.SetForeColor(ParseFromHex(Styles[scopes.First().Name].Foreground)).Append(parsedSourceCode);
			}
			else
			{
				builder.Append(parsedSourceCode);
			}
		}

		public string GetFormattedOutput(string sourceCode, ILanguage language)
		{
			languageParser.Parse(sourceCode, language, (string parsedSourceCode, IList<Scope> captures) => Write(parsedSourceCode, captures));
			/* `Write` outputs to our `RtfBuilder`, so we can execute `ToString` to retrieve the formatted output and provide it back to the caller. */
			return builder.ToString();
		}
	}
}
