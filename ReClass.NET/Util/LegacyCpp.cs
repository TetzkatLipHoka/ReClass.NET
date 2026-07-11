using System.Collections.Generic;
using ColorCode;

namespace ReClassNET.Util;

public class LegacyCpp : ILanguage
{
	public static LegacyCpp Instance { get; } = new();

	public string Id => "cpp";

	public string Name => "C++";

	public string CssClassName => "cplusplus";

	public string FirstLinePattern => null;

	public IList<LanguageRule> Rules => new List<LanguageRule>
	{
		new LanguageRule("/\\*([^*]|[\\r\\n]|(\\*+([^*/]|[\\r\\n])))*\\*+/", new Dictionary<int, string> { { 0, "Comment" } }),
		new LanguageRule("(//.*?)(?=[\\r\\n]|\\z)", new Dictionary<int, string> { { 1, "Comment" } }),
		new LanguageRule("(?s)(\"[^\\n]*?(?<!\\\\)\")", new Dictionary<int, string> { { 0, "String" } }),
		new LanguageRule("\\b(abstract|array|auto|bool|break|case|catch|char|char16_t|char32_t|ref class|class|const|const_cast|continue|default|delegate|delete|deprecated|dllexport|dllimport|do|double|dynamic_cast|each|else|enum|event|explicit|export|extern|false|float|for|friend|friend_as|gcnew|generic|goto|if|in|initonly|inline|int|int8_t|int16_t|int32_t|int64_t|interface|literal|long|mutable|naked|namespace|new|noinline|noreturn|nothrow|novtable|nullptr|operator|private|property|protected|public|register|reinterpret_cast|return|safecast|sealed|selectany|short|signed|sizeof|static|static_cast|ref struct|struct|switch|template|this|thread|throw|true|try|typedef|typeid|typename|uint8_t|uint16_t|uint32_t|uint64_t|union|unsigned|using|uuid|value|virtual|void|volatile|wchar_t|while)\\b", new Dictionary<int, string> { { 0, "Keyword" } })
	};

	public bool HasAlias(string lang)
	{
		string text = lang.ToLower();
		if (text == "c++" || text == "c")
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
