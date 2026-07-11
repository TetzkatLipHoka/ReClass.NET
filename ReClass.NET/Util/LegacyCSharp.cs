using System.Collections.Generic;
using ColorCode;

namespace ReClassNET.Util;

public class LegacyCSharp : ILanguage
{
	public static LegacyCSharp Instance { get; } = new();

	public string Id => "c#";

	public string Name => "C#";

	public string CssClassName => "csharp";

	public string FirstLinePattern => null;

	public IList<LanguageRule> Rules => new List<LanguageRule>
	{
		new LanguageRule("/\\*([^*]|[\\r\\n]|(\\*+([^*/]|[\\r\\n])))*\\*+/", new Dictionary<int, string> { { 0, "Comment" } }),
		new LanguageRule("(///)(?:\\s*?(<[/a-zA-Z0-9\\s\"=]+>))*([^\\r\\n]*)", new Dictionary<int, string>
		{
			{ 1, "XML Doc Tag" },
			{ 2, "XML Doc Tag" },
			{ 3, "XML Doc Comment" }
		}),
		new LanguageRule("(//.*?)(?=[\\r\\n]|\\z)", new Dictionary<int, string> { { 1, "Comment" } }),
		new LanguageRule("'[^\\n]*?(?<!\\\\)'", new Dictionary<int, string> { { 0, "String" } }),
		new LanguageRule("(?s)@\"(?:\"\"|.)*?\"(?!\")", new Dictionary<int, string> { { 0, "String (C# @ Verbatim)" } }),
		new LanguageRule("(?s)(\"[^\\n]*?(?<!\\\\)\")", new Dictionary<int, string> { { 0, "String" } }),
		new LanguageRule("\\[(assembly|module|type|return|param|method|field|property|event):[^\\]\"]*(\"[^\\n]*?(?<!\\\\)\")?[^\\]]*\\]", new Dictionary<int, string>
		{
			{ 1, "Keyword" },
			{ 2, "String" }
		}),
		new LanguageRule("^\\s*(\\#define|\\#elif|\\#else|\\#endif|\\#endregion|\\#error|\\#if|\\#line|\\#pragma|\\#region|\\#undef|\\#warning).*?$", new Dictionary<int, string> { { 1, "Preprocessor Keyword" } }),
		new LanguageRule("\\b(abstract|as|ascending|base|bool|break|by|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|descending|do|double|dynamic|else|enum|equals|event|explicit|extern|false|finally|fixed|float|for|foreach|from|get|goto|group|if|implicit|in|int|into|interface|internal|is|join|let|lock|long|namespace|new|null|object|on|operator|orderby|out|override|params|partial|private|protected|public|readonly|ref|return|sbyte|sealed|select|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|where|while|yield)\\b", new Dictionary<int, string> { { 1, "Keyword" } })
	};

	public bool HasAlias(string lang)
	{
		string text = lang.ToLower();
		if (text == "cs" || text == "c#")
		{
			return true;
		}

		return false;
	}

	public override string ToString()
	{
		return Name;
	}
}
