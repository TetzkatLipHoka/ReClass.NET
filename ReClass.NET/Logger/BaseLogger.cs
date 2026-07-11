using System;
using System.Diagnostics.Contracts;
using System.Diagnostics;
namespace ReClassNET.Logger
{
	public abstract class BaseLogger : ILogger
	{
		private readonly object sync = new object();

		public event NewLogEntryEventHandler NewLogEntry;

		public void Log(Exception ex)
		{
			Log(LogLevel.Error, ex.ToString(), ex);
		}

		public void Log(LogLevel level, string message)
		{
			if (level == LogLevel.Error)
			{
				var caller = new StackTrace().GetFrame(1).GetMethod();
				message = $"{message} | {caller.DeclaringType.FullName}.{caller.Name}";
			}
			Log(level, message, null);
		}

		private void Log(LogLevel level, string message, Exception ex)
		{
			Contract.Requires(message != null);

			lock (sync)
			{
				NewLogEntry?.Invoke(level, message, ex);
			}
		}
	}
}
