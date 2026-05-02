using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ReClassNET.Extensions
{
	public static class StringExtension
	{
		[Pure]
		[DebuggerStepThrough]
		public static bool IsPrintable(this char c)
		{
			if (c == '\xFFFD') // Unicode REPLACEMENT CHARACTER
			{
				return false;
			}
			if (char.IsControl(c))
			{
				// In many 8-bit encodings characters in the range 0x80 - 0x9F are printable.
				// While they are control characters in Unicode, we allow them here to support these encodings.
				return (c >= 0x80 && c <= 0x9F) || c == '\n' || c == '\r' || c == '\t';
			}
			return true;
		}

		[DebuggerStepThrough]
		public static IEnumerable<char> InterpretAsSingleByteCharacter(this IEnumerable<byte> source)
		{
			return InterpretAsSingleByteCharacter(source, Encoding.Default);
		}

		[DebuggerStepThrough]
		public static IEnumerable<char> InterpretAsSingleByteCharacter(this IEnumerable<byte> source, Encoding encoding)
		{
			Contract.Requires(source != null);
			Contract.Requires(encoding != null);

			return encoding.GetChars(source.ToArray());
		}

		[DebuggerStepThrough]
		public static IEnumerable<char> InterpretAsDoubleByteCharacter(this IEnumerable<byte> source)
		{
			Contract.Requires(source != null);

			return Encoding.Unicode.GetChars(source.ToArray());
		}

		[Pure]
		[DebuggerStepThrough]
		public static bool IsStrictlyPrintable(this char c)
		{
			if (c == '\xFFFD')
			{
				return false;
			}
			if (c == '\n' || c == '\r' || c == '\t')
			{
				return true;
			}
			if (char.IsControl(c))
			{
				return false;
			}

			// We restrict the detection to common scripts and symbols to avoid false positives with random data.
			// Latin, Cyrillic, Greek, etc. are usually in the lower Unicode ranges.
			// Symbols, Box Drawing, Block Elements are in 0x2000 - 0x2BFF.
			// Private Use Area (E000 - F8FF) is common for custom font icons in games.
			return c < 0x3000 || (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0xE000 && c <= 0xF8FF) || char.IsSurrogate(c);
		}

		[DebuggerStepThrough]
		public static bool IsPrintableData(this IEnumerable<char> source)
		{
			Contract.Requires(source != null);

			return CalculatePrintableDataThreshold(source, true) >= 1.0f;
		}

		[DebuggerStepThrough]
		public static bool IsLikelyPrintableData(this IEnumerable<char> source)
		{
			Contract.Requires(source != null);

			return CalculatePrintableDataThreshold(source, true) >= 0.75f;
		}

		[DebuggerStepThrough]
		public static float CalculatePrintableDataThreshold(this IEnumerable<char> source, bool strictly = false)
		{
			var doCountValid = true;
			var countValid = 0;
			var countAll = 0;

			foreach (var c in source)
			{
				if (c == 0)
				{
					break;
				}

				countAll++;

				if (doCountValid)
				{
					if (strictly ? c.IsStrictlyPrintable() : c.IsPrintable())
					{
						countValid++;
					}
					else
					{
						doCountValid = false;
					}
				}
			}

			if (countAll == 0)
			{
				return 0.0f;
			}

			return countValid / (float)countAll;
		}

		[Pure]
		[DebuggerStepThrough]
		public static string LimitLength(this string s, int length)
		{
			Contract.Requires(s != null);
			Contract.Ensures(Contract.Result<string>() != null);

			if (s.Length <= length)
			{
				return s;
			}
			return s.Substring(0, length);
		}

		private static readonly Regex hexadecimalValueRegex = new Regex("^(0x|h)?([0-9A-F]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		public static bool TryGetHexString(this string s, out string value)
		{
			Contract.Requires(s != null);

			var match = hexadecimalValueRegex.Match(s);
			value = match.Success ? match.Groups[2].Value : null;

			return match.Success;
		}
	}
}
