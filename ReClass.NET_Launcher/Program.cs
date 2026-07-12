using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ReClassNET;
using ReClassNET.DataExchange.ReClass;
using ReClassNET.Native;
using ReClassNET.Util;

namespace ReClassNET_Launcher
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			var commandLineArgs = new CommandLineArgs(args);

			// Register the files with the launcher.
			if (commandLineArgs[Constants.CommandLineOptions.FileExtRegister] != null)
			{
				NativeMethods.RegisterExtension(ReClassNetFile.DefaultFileExtension, ReClassNetFile.FileExtensionId, PathUtil.ExecutablePath, Constants.ApplicationName);
				NativeMethods.RegisterExtension(ReClassNetFile.AlternateFileExtension, ReClassNetFile.AlternateFileExtensionId, PathUtil.ExecutablePath, Constants.ApplicationName);

				return;
			}
			if (commandLineArgs[Constants.CommandLineOptions.FileExtUnregister] != null)
			{
				NativeMethods.UnregisterExtension(ReClassNetFile.DefaultFileExtension, ReClassNetFile.FileExtensionId);
				NativeMethods.UnregisterExtension(ReClassNetFile.AlternateFileExtension, ReClassNetFile.AlternateFileExtensionId);

				return;
			}

			// Use the OS bitness, not the launcher process bitness, so an x86 launcher still
			// starts the x64 build on a 64-bit OS.
			var is64Bit = Environment.Is64BitOperatingSystem;

			// If there is a file in the commandline, read the platform.
			if (commandLineArgs.FileName != null)
			{
				try
				{
					is64Bit = ReClassNetFile.ReadPlatform(commandLineArgs.FileName) == "x64";
				}
				catch (Exception)
				{
					
				}
			}

			// And finally start the real ReClass.NET.
			var applicationPath = Path.Combine(PathUtil.ExecutableFolderPath, is64Bit ? "x64" : "x86", Constants.ApplicationExecutableName);

			try
			{
				// The launcher is already elevated (see app.manifest), so start the
				// (requireAdministrator) application directly via CreateProcess instead of
				// ShellExecute (which fails to elevate when UAC is disabled). Set the working
				// directory so the app's native dependencies (NativeCore.dll, libclang, ...) resolve.
				var processStartInfo = new ProcessStartInfo
				{
					FileName = applicationPath,
					WorkingDirectory = Path.GetDirectoryName(applicationPath),
					UseShellExecute = false,
					WindowStyle = ProcessWindowStyle.Normal
				};
				var arguments = GetCommandLineWithoutExecutablePath();
				if (arguments != null)
				{
					processStartInfo.Arguments = arguments;
				}

				Process.Start(processStartInfo);
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Could not start '{applicationPath}'.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					Constants.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>Gets command line without the executable path.</summary>
		/// <returns>If empty <c>null</c> else the command line parameters.</returns>
		private static string GetCommandLineWithoutExecutablePath()
		{
			var commandLine = Environment.CommandLine;

			if (string.IsNullOrEmpty(commandLine))
			{
				return null;
			}

			var arguments = string.Empty;
			int argIndex;

			if (commandLine[0] == '"')
			{
				var secondDoublequoteIndex = -1;
				for (var i = 1; i < commandLine.Length; ++i)
				{
					if (commandLine[i] == '\\')
					{
						++i;
						continue;
					}
					if (commandLine[i] == '"')
					{
						secondDoublequoteIndex = i + 1;
						break;
					}
				}
				argIndex = secondDoublequoteIndex;
			}
			else
			{
				argIndex = commandLine.IndexOf(" ", StringComparison.Ordinal);
			}
			// Guard against argIndex pointing at (or past) the end of the string, which happens
			// when the launcher is started without arguments (command line is just the quoted
			// executable path). Substring(argIndex + 1) would otherwise throw.
			if (argIndex != -1 && argIndex + 1 <= commandLine.Length)
			{
				arguments = commandLine.Substring(argIndex + 1);
			}

			return arguments == string.Empty ? null : arguments;
		}
	}
}
