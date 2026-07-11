using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using ReClassNET.CodeGenerator;
using ReClassNET.Forms;
using ReClassNET.Logger;
using ReClassNET.Nodes;
using ReClassNET.Project;

namespace ReClassNET.Mcp
{
	/// <summary>
	/// Embedded Model Context Protocol server. Speaks JSON-RPC 2.0 over a minimal HTTP/1.1
	/// transport implemented directly on a <see cref="TcpListener"/> so it works without an
	/// external bridge process and without requiring an HTTP.sys URL reservation.
	/// </summary>
	public class McpServer : IDisposable
	{
		private const string ProtocolVersion = "2024-11-05";

		private readonly ILogger logger;
		private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

		private TcpListener listener;
		private Thread acceptThread;
		private volatile bool running;

		public int Port { get; private set; }
		public bool IsRunning => running;

		public McpServer(ILogger logger)
		{
			Contract.Requires(logger != null);

			this.logger = logger;
		}

		public void Start(int port)
		{
			if (running)
			{
				return;
			}

			listener = new TcpListener(IPAddress.Loopback, port);
			listener.Start();
			Port = ((IPEndPoint)listener.LocalEndpoint).Port;
			running = true;

			acceptThread = new Thread(AcceptLoop)
			{
				IsBackground = true,
				Name = "McpServerAccept"
			};
			acceptThread.Start();

			logger.Log(LogLevel.Information, $"MCP server listening on http://127.0.0.1:{Port}/");
		}

		public void Stop()
		{
			if (!running)
			{
				return;
			}

			running = false;

			try
			{
				listener?.Stop();
			}
			catch
			{
				// ignored
			}

			listener = null;

			logger.Log(LogLevel.Information, "MCP server stopped.");
		}

		public void Dispose()
		{
			Stop();
		}

		private void AcceptLoop()
		{
			while (running)
			{
				TcpClient client;
				try
				{
					client = listener.AcceptTcpClient();
				}
				catch
				{
					// listener stopped or faulted
					break;
				}

				ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
			}
		}

		private void HandleClient(TcpClient client)
		{
			try
			{
				using (client)
				using (var stream = client.GetStream())
				{
					// Guard against a client that connects but never finishes sending: bound the
					// blocking reads so a stalled peer can't pin a thread-pool thread forever.
					client.ReceiveTimeout = 15000;
					client.SendTimeout = 15000;

					if (!TryReadHttpRequest(stream, out var method, out var path, out var body))
					{
						return;
					}

					// Basic CORS/health handling.
					if (method == "GET")
					{
						WriteHttpResponse(stream, 200, "application/json", "{\"status\":\"ok\",\"server\":\"ReClass.NET MCP\"}");
						return;
					}

					if (method == "OPTIONS")
					{
						WriteHttpResponse(stream, 204, null, string.Empty);
						return;
					}

					if (method != "POST")
					{
						WriteHttpResponse(stream, 405, "text/plain", "Method Not Allowed");
						return;
					}

					var responseJson = HandleRpcPayload(body);
					if (responseJson == null)
					{
						// Notification(s) only -> no content.
						WriteHttpResponse(stream, 202, null, string.Empty);
						return;
					}

					WriteHttpResponse(stream, 200, "application/json", responseJson);
				}
			}
			catch (Exception ex)
			{
				logger.Log(LogLevel.Error, $"MCP client handler failed: {ex.Message}");
			}
		}

		#region HTTP transport

		private static bool TryReadHttpRequest(NetworkStream stream, out string method, out string path, out string body)
		{
			method = null;
			path = null;
			body = null;

			var headerBytes = new List<byte>(1024);
			var window = new byte[4];
			var windowLen = 0;
			var single = new byte[1];

			// Read until end of headers (\r\n\r\n).
			while (true)
			{
				var read = stream.Read(single, 0, 1);
				if (read == 0)
				{
					return false;
				}

				headerBytes.Add(single[0]);

				if (windowLen < 4)
				{
					window[windowLen++] = single[0];
				}
				else
				{
					window[0] = window[1];
					window[1] = window[2];
					window[2] = window[3];
					window[3] = single[0];
				}

				if (windowLen == 4 && window[0] == '\r' && window[1] == '\n' && window[2] == '\r' && window[3] == '\n')
				{
					break;
				}

				if (headerBytes.Count > 64 * 1024)
				{
					return false;
				}
			}

			var headerText = Encoding.ASCII.GetString(headerBytes.ToArray());
			var lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
			if (lines.Length == 0)
			{
				return false;
			}

			var requestLine = lines[0].Split(' ');
			if (requestLine.Length < 2)
			{
				return false;
			}

			method = requestLine[0].ToUpperInvariant();
			path = requestLine[1];

			var contentLength = 0;
			var expectsContinue = false;
			foreach (var line in lines.Skip(1))
			{
				var idx = line.IndexOf(':');
				if (idx <= 0)
				{
					continue;
				}

				var key = line.Substring(0, idx).Trim();
				var value = line.Substring(idx + 1).Trim();
				if (string.Equals(key, "Content-Length", StringComparison.OrdinalIgnoreCase))
				{
					int.TryParse(value, out contentLength);
				}
				else if (string.Equals(key, "Expect", StringComparison.OrdinalIgnoreCase)
					&& value.IndexOf("100-continue", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					expectsContinue = true;
				}
			}

			// Clients that send "Expect: 100-continue" withhold the request body until the
			// server acknowledges. Without this the read below would deadlock.
			if (expectsContinue)
			{
				var cont = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
				stream.Write(cont, 0, cont.Length);
				stream.Flush();
			}

			if (contentLength > 16 * 1024 * 1024)
			{
				// Reject an absurd Content-Length instead of allocating it.
				return false;
			}

			if (contentLength > 0)
			{
				var buffer = new byte[contentLength];
				var offset = 0;
				while (offset < contentLength)
				{
					var read = stream.Read(buffer, offset, contentLength - offset);
					if (read == 0)
					{
						break;
					}
					offset += read;
				}
				body = Encoding.UTF8.GetString(buffer, 0, offset);
			}
			else
			{
				body = string.Empty;
			}

			return true;
		}

		private static void WriteHttpResponse(NetworkStream stream, int statusCode, string contentType, string content)
		{
			var payload = content != null ? Encoding.UTF8.GetBytes(content) : Array.Empty<byte>();

			var sb = new StringBuilder();
			sb.Append($"HTTP/1.1 {statusCode} {GetStatusText(statusCode)}\r\n");
			if (contentType != null)
			{
				sb.Append($"Content-Type: {contentType}; charset=utf-8\r\n");
			}
			sb.Append($"Content-Length: {payload.Length}\r\n");
			sb.Append("Access-Control-Allow-Origin: *\r\n");
			sb.Append("Access-Control-Allow-Headers: Content-Type\r\n");
			sb.Append("Access-Control-Allow-Methods: POST, GET, OPTIONS\r\n");
			sb.Append("Connection: close\r\n");
			sb.Append("\r\n");

			var headerBytes = Encoding.ASCII.GetBytes(sb.ToString());
			stream.Write(headerBytes, 0, headerBytes.Length);
			if (payload.Length > 0)
			{
				stream.Write(payload, 0, payload.Length);
			}
			stream.Flush();
		}

		private static string GetStatusText(int code)
		{
			switch (code)
			{
				case 200: return "OK";
				case 202: return "Accepted";
				case 204: return "No Content";
				case 405: return "Method Not Allowed";
				default: return "OK";
			}
		}

		#endregion

		#region JSON-RPC dispatch

		private string HandleRpcPayload(string body)
		{
			object parsed;
			try
			{
				parsed = serializer.DeserializeObject(body);
			}
			catch (Exception ex)
			{
				return serializer.Serialize(MakeError(null, -32700, $"Parse error: {ex.Message}"));
			}

			// Batch request.
			if (parsed is object[] batch)
			{
				var responses = new List<object>();
				foreach (var item in batch)
				{
					var response = HandleSingleRpc(item as Dictionary<string, object>);
					if (response != null)
					{
						responses.Add(response);
					}
				}
				return responses.Count > 0 ? serializer.Serialize(responses) : null;
			}

			var single = HandleSingleRpc(parsed as Dictionary<string, object>);
			return single != null ? serializer.Serialize(single) : null;
		}

		private Dictionary<string, object> HandleSingleRpc(Dictionary<string, object> request)
		{
			if (request == null)
			{
				return MakeError(null, -32600, "Invalid Request");
			}

			request.TryGetValue("id", out var id);
			var hasId = request.ContainsKey("id");
			var method = request.TryGetValue("method", out var m) ? m as string : null;
			var @params = request.TryGetValue("params", out var p) ? p as Dictionary<string, object> : null;

			if (string.IsNullOrEmpty(method))
			{
				return hasId ? MakeError(id, -32600, "Invalid Request") : null;
			}

			try
			{
				switch (method)
				{
					case "initialize":
						return MakeResult(id, BuildInitializeResult());
					case "notifications/initialized":
					case "notifications/cancelled":
						return null; // notification, no response
					case "ping":
						return MakeResult(id, new Dictionary<string, object>());
					case "tools/list":
						return MakeResult(id, new Dictionary<string, object> { ["tools"] = BuildToolList() });
					case "tools/call":
						return MakeResult(id, CallTool(@params));
					default:
						return hasId ? MakeError(id, -32601, $"Method not found: {method}") : null;
				}
			}
			catch (McpToolException ex)
			{
				// Tool-level error surfaced as a successful result with isError=true.
				return MakeResult(id, TextToolResult(ex.Message, true));
			}
			catch (Exception ex)
			{
				return MakeError(id, -32603, $"Internal error: {ex.Message}");
			}
		}

		private Dictionary<string, object> BuildInitializeResult()
		{
			return new Dictionary<string, object>
			{
				["protocolVersion"] = ProtocolVersion,
				["capabilities"] = new Dictionary<string, object>
				{
					["tools"] = new Dictionary<string, object>()
				},
				["serverInfo"] = new Dictionary<string, object>
				{
					["name"] = "ReClass.NET",
					["version"] = Constants.ApplicationVersion
				}
			};
		}

		#endregion

		#region Tools

		private List<object> BuildToolList()
		{
			object StringSchema(string desc) => new Dictionary<string, object> { ["type"] = "string", ["description"] = desc };
			object IntSchema(string desc) => new Dictionary<string, object> { ["type"] = "integer", ["description"] = desc };

			Dictionary<string, object> Tool(string name, string description, Dictionary<string, object> properties, params string[] required)
			{
				var schema = new Dictionary<string, object>
				{
					["type"] = "object",
					["properties"] = properties
				};
				if (required.Length > 0)
				{
					schema["required"] = required;
				}
				return new Dictionary<string, object>
				{
					["name"] = name,
					["description"] = description,
					["inputSchema"] = schema
				};
			}

			return new List<object>
			{
				Tool("processInfo", "Get information about the process ReClass.NET is currently attached to.",
					new Dictionary<string, object>()),
				Tool("projectState", "Return the current project's classes, enums and their member nodes (structure, offsets, sizes and comments).",
					new Dictionary<string, object>()),
				Tool("treeSearch", "Search class member nodes by a substring of the node name or its type.",
					new Dictionary<string, object> { ["query"] = StringSchema("Substring matched against node name and type name.") },
					"query"),
				Tool("hexRead", "Read raw bytes from the attached process. Returns an uppercase hex string.",
					new Dictionary<string, object>
					{
						["address"] = StringSchema("Address as a ReClass address formula (e.g. '0x1234', 'module.dll+0x10')."),
						["size"] = IntSchema("Number of bytes to read (1-4096).")
					},
					"address", "size"),
				Tool("hexWrite", "Write raw bytes to the attached process. Requires an active attachment.",
					new Dictionary<string, object>
					{
						["address"] = StringSchema("Address as a ReClass address formula."),
						["data"] = StringSchema("Bytes to write as a hex string, e.g. 'DEADBEEF'.")
					},
					"address", "data"),
				Tool("statusSet", "Show a text message in the ReClass.NET status bar.",
					new Dictionary<string, object> { ["text"] = StringSchema("Message to display.") },
					"text"),
				Tool("generateCode", "Generate source code for the current project in the given language.",
					new Dictionary<string, object>
					{
						["language"] = new Dictionary<string, object>
						{
							["type"] = "string",
							["description"] = "One of: c, cpp, csharp, rust, pascal.",
							["enum"] = new[] { "c", "cpp", "csharp", "rust", "pascal" }
						}
					},
					"language")
			};
		}

		private Dictionary<string, object> CallTool(Dictionary<string, object> @params)
		{
			var name = @params != null && @params.TryGetValue("name", out var n) ? n as string : null;
			var arguments = @params != null && @params.TryGetValue("arguments", out var a) ? a as Dictionary<string, object> : new Dictionary<string, object>();
			arguments = arguments ?? new Dictionary<string, object>();

			if (string.IsNullOrEmpty(name))
			{
				throw new McpToolException("Missing tool name.");
			}

			switch (name)
			{
				case "processInfo":
					return TextToolResult(ToolProcessInfo());
				case "projectState":
					return TextToolResult(ToolProjectState());
				case "treeSearch":
					return TextToolResult(ToolTreeSearch(GetString(arguments, "query")));
				case "hexRead":
					return TextToolResult(ToolHexRead(GetString(arguments, "address"), GetInt(arguments, "size")));
				case "hexWrite":
					return TextToolResult(ToolHexWrite(GetString(arguments, "address"), GetString(arguments, "data")));
				case "statusSet":
					return TextToolResult(ToolStatusSet(GetString(arguments, "text")));
				case "generateCode":
					return TextToolResult(ToolGenerateCode(GetString(arguments, "language")));
				default:
					throw new McpToolException($"Unknown tool: {name}");
			}
		}

		private string ToolProcessInfo()
		{
			var process = Program.RemoteProcess;
			var result = new Dictionary<string, object>
			{
				["isValid"] = process.IsValid
			};
			if (process.UnderlayingProcess != null)
			{
				result["name"] = process.UnderlayingProcess.Name;
				result["id"] = process.UnderlayingProcess.Id.ToString();
				result["path"] = process.UnderlayingProcess.Path;
			}
			return serializer.Serialize(result);
		}

		private string ToolProjectState()
		{
			return RunOnUiThread(() =>
			{
				var project = MainForm.CurrentProject;
				if (project == null)
				{
					throw new McpToolException("No project is currently open.");
				}

				var classes = project.Classes.Select(c => new Dictionary<string, object>
				{
					["name"] = c.Name,
					["uuid"] = c.Uuid.ToString(),
					["comment"] = c.Comment,
					["addressFormula"] = c.AddressFormula,
					["size"] = c.MemorySize,
					["nodes"] = c.Nodes.Select(NodeToDictionary).ToList()
				}).ToList();

				var enums = project.Enums.Select(e => new Dictionary<string, object>
				{
					["name"] = e.Name,
					["size"] = (int)e.Size,
					["values"] = e.Values.Select(kv => new Dictionary<string, object>
					{
						["name"] = kv.Key,
						["value"] = kv.Value
					}).ToList()
				}).ToList();

				return serializer.Serialize(new Dictionary<string, object>
				{
					["classes"] = classes,
					["enums"] = enums
				});
			});
		}

		private string ToolTreeSearch(string query)
		{
			if (string.IsNullOrEmpty(query))
			{
				throw new McpToolException("Parameter 'query' is required.");
			}

			return RunOnUiThread(() =>
			{
				var project = MainForm.CurrentProject;
				if (project == null)
				{
					throw new McpToolException("No project is currently open.");
				}

				var matches = new List<object>();
				foreach (var @class in project.Classes)
				{
					foreach (var node in @class.Nodes)
					{
						var typeName = node.GetType().Name;
						if ((node.Name?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
							|| typeName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							var entry = NodeToDictionary(node);
							entry["class"] = @class.Name;
							matches.Add(entry);
						}
					}
				}

				return serializer.Serialize(new Dictionary<string, object> { ["matches"] = matches });
			});
		}

		private string ToolHexRead(string address, int size)
		{
			if (size <= 0 || size > 4096)
			{
				throw new McpToolException("Parameter 'size' must be between 1 and 4096.");
			}

			var process = Program.RemoteProcess;
			if (!process.IsValid)
			{
				throw new McpToolException("Not attached to a valid process.");
			}

			var resolved = ResolveAddress(address);
			var data = process.ReadRemoteMemory(resolved, size);

			var hex = new StringBuilder(data.Length * 2);
			foreach (var b in data)
			{
				hex.Append(b.ToString("X2", CultureInfo.InvariantCulture));
			}

			return serializer.Serialize(new Dictionary<string, object>
			{
				["address"] = $"0x{resolved.ToInt64():X}",
				["size"] = size,
				["data"] = hex.ToString()
			});
		}

		private string ToolHexWrite(string address, string data)
		{
			var bytes = ParseHex(data);
			if (bytes.Length == 0)
			{
				throw new McpToolException("Parameter 'data' must contain at least one byte.");
			}

			var process = Program.RemoteProcess;
			if (!process.IsValid)
			{
				throw new McpToolException("Not attached to a valid process.");
			}

			var resolved = ResolveAddress(address);
			var success = process.WriteRemoteMemory(resolved, bytes);

			return serializer.Serialize(new Dictionary<string, object>
			{
				["address"] = $"0x{resolved.ToInt64():X}",
				["written"] = bytes.Length,
				["success"] = success
			});
		}

		private string ToolStatusSet(string text)
		{
			if (text == null)
			{
				throw new McpToolException("Parameter 'text' is required.");
			}

			return RunOnUiThread(() =>
			{
				Program.MainForm.SetStatusMessage(text);
				return serializer.Serialize(new Dictionary<string, object> { ["ok"] = true });
			});
		}

		private string ToolGenerateCode(string language)
		{
			return RunOnUiThread(() =>
			{
				var project = MainForm.CurrentProject;
				if (project == null)
				{
					throw new McpToolException("No project is currently open.");
				}

				// Created on the UI thread and after the null-check so cpp's TypeMapping access is safe.
				var generator = CreateGenerator(language, project);

				var code = generator.GenerateCode(project.Classes, project.Enums, Program.Logger);
				return code;
			});
		}

		private ICodeGenerator CreateGenerator(string language, ReClassNetProject project)
		{
			switch ((language ?? string.Empty).ToLowerInvariant())
			{
				case "c":
					return new CCodeGenerator();
				case "cpp":
				case "c++":
					return new CppCodeGenerator(project.TypeMapping, Program.Settings.CppGeneratorShowOffset, Program.Settings.CppGeneratorShowPadding);
				case "csharp":
				case "c#":
				case "cs":
					return new CSharpCodeGenerator();
				case "rust":
					return new RustCodeGenerator();
				case "pascal":
				case "delphi":
					return new PascalCodeGenerator();
				default:
					throw new McpToolException($"Unknown language: {language}. Use one of c, cpp, csharp, rust, pascal.");
			}
		}

		private static Dictionary<string, object> NodeToDictionary(BaseNode node)
		{
			return new Dictionary<string, object>
			{
				["name"] = node.Name,
				["type"] = node.GetType().Name,
				["offset"] = node.Offset,
				["size"] = node.MemorySize,
				["comment"] = node.Comment
			};
		}

		private static IntPtr ResolveAddress(string address)
		{
			if (string.IsNullOrWhiteSpace(address))
			{
				throw new McpToolException("Parameter 'address' is required.");
			}

			var resolved = Program.RemoteProcess.ParseAddress(address);
			if (resolved == IntPtr.Zero)
			{
				throw new McpToolException($"Could not resolve address '{address}'.");
			}
			return resolved;
		}

		private static byte[] ParseHex(string hex)
		{
			if (string.IsNullOrEmpty(hex))
			{
				return Array.Empty<byte>();
			}

			var cleaned = hex.Replace(" ", string.Empty).Replace("0x", string.Empty).Replace("0X", string.Empty);
			if (cleaned.Length % 2 != 0)
			{
				throw new McpToolException("Hex string must have an even number of digits.");
			}

			var result = new byte[cleaned.Length / 2];
			for (var i = 0; i < result.Length; i++)
			{
				if (!byte.TryParse(cleaned.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result[i]))
				{
					throw new McpToolException($"Invalid hex byte at position {i * 2}.");
				}
			}
			return result;
		}

		#endregion

		#region Helpers

		private static string GetString(Dictionary<string, object> args, string key)
		{
			return args != null && args.TryGetValue(key, out var value) ? value?.ToString() : null;
		}

		private static int GetInt(Dictionary<string, object> args, string key)
		{
			if (args == null || !args.TryGetValue(key, out var value) || value == null)
			{
				return 0;
			}

			if (value is int i)
			{
				return i;
			}

			return int.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
		}

		private static T RunOnUiThread<T>(Func<T> action)
		{
			var mainForm = Program.MainForm;
			if (mainForm == null || !mainForm.IsHandleCreated)
			{
				return action();
			}

			if (!mainForm.InvokeRequired)
			{
				return action();
			}

			// Marshal to the UI thread and unwrap tool exceptions so they are reported correctly.
			Exception captured = null;
			var result = default(T);
			mainForm.Invoke((MethodInvoker)(() =>
			{
				try
				{
					result = action();
				}
				catch (Exception ex)
				{
					captured = ex;
				}
			}));

			if (captured != null)
			{
				if (captured is McpToolException toolEx)
				{
					throw toolEx;
				}
				throw new Exception(captured.Message, captured);
			}

			return result;
		}

		private Dictionary<string, object> TextToolResult(string text, bool isError = false)
		{
			var result = new Dictionary<string, object>
			{
				["content"] = new List<object>
				{
					new Dictionary<string, object>
					{
						["type"] = "text",
						["text"] = text ?? string.Empty
					}
				}
			};
			if (isError)
			{
				result["isError"] = true;
			}
			return result;
		}

		private static Dictionary<string, object> MakeResult(object id, object result)
		{
			return new Dictionary<string, object>
			{
				["jsonrpc"] = "2.0",
				["id"] = id,
				["result"] = result
			};
		}

		private static Dictionary<string, object> MakeError(object id, int code, string message)
		{
			return new Dictionary<string, object>
			{
				["jsonrpc"] = "2.0",
				["id"] = id,
				["error"] = new Dictionary<string, object>
				{
					["code"] = code,
					["message"] = message
				}
			};
		}

		#endregion

		private class McpToolException : Exception
		{
			public McpToolException(string message) : base(message)
			{
			}
		}
	}
}
