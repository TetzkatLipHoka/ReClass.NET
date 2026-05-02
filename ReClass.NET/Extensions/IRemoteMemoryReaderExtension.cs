using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using ReClassNET.Memory;
using ReClassNET.MemoryScanner;

namespace ReClassNET.Extensions
{
	public static class IRemoteMemoryReaderExtension
	{
		private static readonly Dictionary<Encoding, BytePattern> nullTerminatorCache = new Dictionary<Encoding, BytePattern>();

		public static sbyte ReadRemoteInt8(this IRemoteMemoryReader reader, IntPtr address)
		{
			var data = reader.ReadRemoteMemory(address, sizeof(sbyte));

			return (sbyte)data[0];
		}

		public static byte ReadRemoteUInt8(this IRemoteMemoryReader reader, IntPtr address)
		{
			var data = reader.ReadRemoteMemory(address, sizeof(byte));

			return data[0];
		}

		public static short ReadRemoteInt16(this IRemoteMemoryReader reader, IntPtr address)
		{
			var data = reader.ReadRemoteMemory(address, sizeof(short));

			return reader.BitConverter.ToInt16(data, 0);
		}

		public static ushort ReadRemoteUInt16(this IRemoteMemoryReader reader, IntPtr address)
		{
			var data = reader.ReadRemoteMemory(address, sizeof(ushort));

			return reader.BitConverter.ToUInt16(data, 0);
		}

		public static int ReadRemoteInt32(this IRemoteMemoryReader reader, IntPtr address)
		{
			var data = reader.ReadRemoteMemory(address, sizeof(int));

			return reader.BitConverter.ToInt32(data, 0);
		}

		public static uint ReadRemoteUInt32(this IRemoteMemoryReader reader, IntPtr address)
		{
			var data = reader.ReadRemoteMemory(address, sizeof(uint));

			return reader.BitConverter.ToUInt32(data, 0);
		}

		public static long ReadRemoteInt64(this IRemoteMemoryReader reader, IntPtr address)
		{
			var data = reader.ReadRemoteMemory(address, sizeof(long));

			return reader.BitConverter.ToInt64(data, 0);
		}

		public static ulong ReadRemoteUInt64(this IRemoteMemoryReader reader, IntPtr address)
		{
			var data = reader.ReadRemoteMemory(address, sizeof(ulong));

			return reader.BitConverter.ToUInt64(data, 0);
		}

		public static float ReadRemoteFloat(this IRemoteMemoryReader reader, IntPtr address)
		{
			var data = reader.ReadRemoteMemory(address, sizeof(float));

			return reader.BitConverter.ToSingle(data, 0);
		}

		public static double ReadRemoteDouble(this IRemoteMemoryReader reader, IntPtr address)
		{
			var data = reader.ReadRemoteMemory(address, sizeof(double));

			return reader.BitConverter.ToDouble(data, 0);
		}

		public static IntPtr ReadRemoteIntPtr(this IRemoteMemoryReader reader, IntPtr address)
		{
#if RECLASSNET64
			return (IntPtr)reader.ReadRemoteInt64(address);
#else
			return (IntPtr)reader.ReadRemoteInt32(address);
#endif
		}

		public static string ReadRemoteString(this IRemoteMemoryReader reader, IntPtr address, Encoding encoding, int length)
		{
			Contract.Requires(encoding != null);
			Contract.Requires(length >= 0);
			Contract.Ensures(Contract.Result<string>() != null);

			var data = reader.ReadRemoteMemory(address, encoding.GetMaxByteCount(length));

			try
			{
				var chars = encoding.GetChars(data);
				var sb = new StringBuilder();
				var count = 0;
				foreach (var c in chars)
				{
					if (c == '\0' || count >= length)
					{
						break;
					}
					if (c.IsPrintable())
					{
						sb.Append(c);
					}
					else
					{
						sb.Append('.');
					}
					count++;
				}
				return sb.ToString();
			}
			catch
			{
				return string.Empty;
			}
		}

		public static string ReadRemoteStringUntilFirstNullCharacter(this IRemoteMemoryReader reader, IntPtr address, Encoding encoding, int length)
		{
			Contract.Requires(encoding != null);
			Contract.Requires(length >= 0);
			Contract.Ensures(Contract.Result<string>() != null);

			var data = reader.ReadRemoteMemory(address, encoding.GetMaxByteCount(length));

			if (!nullTerminatorCache.TryGetValue(encoding, out var pattern))
			{
				pattern = BytePattern.From(encoding.GetBytes("\0"));

				nullTerminatorCache.Add(encoding, pattern);
			}

			var index = PatternScanner.FindPattern(pattern, data);
			if (index == -1)
			{
				index = data.Length;
			}

			try
			{
				var str = encoding.GetString(data, 0, Math.Min(index, data.Length));
				if (str.Length > length)
				{
					str = str.Substring(0, length);
				}
				return str;
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}
