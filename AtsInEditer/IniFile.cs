using System.Text;

namespace AtsInEditer
{
    /// <summary>
    /// INIファイルを1行ずつ読み書きするクラスです。
    /// 値だけを書き換えるので、行末の「; コメント」や空白の揃え、他の行はそのまま残ります。
    /// </summary>
    public class IniFile
    {
        private readonly List<string> lines;
        private readonly Encoding encoding;
        private string path;

        private readonly string newLine;        // 改行の種類(\r\n か \n)
        private readonly bool endsWithNewLine;  // 最後の行のあとに改行があったか

        private IniFile(string path, List<string> lines, Encoding encoding, string newLine, bool endsWithNewLine)
        {
            this.path = path;
            this.lines = lines;
            this.encoding = encoding;
            this.newLine = newLine;
            this.endsWithNewLine = endsWithNewLine;
        }

        /// <summary>
        /// 中身が空の新しいINIを用意します(UTF-8・BOMなし、改行は \r\n)。Save() でファイルができます。
        /// </summary>
        public static IniFile CreateNew(string path)
        {
            return new IniFile(path, new List<string>(), new UTF8Encoding(false), "\r\n", true);
        }

        /// <summary>
        /// ファイルを読み込みます。文字コードは自動で見分けます。
        /// UTF-8(BOM付き)→ UTF-8(BOM付き)、UTF-8として正しい → UTF-8(BOMなし)、それ以外 → Shift-JIS
        /// </summary>
        public static IniFile Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Encoding enc;
            string text;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                enc = new UTF8Encoding(true);
                text = enc.GetString(bytes, 3, bytes.Length - 3);
            }
            else
            {
                try
                {
                    // UTF-8として読めない文字があれば例外になる
                    var strictUtf8 = new UTF8Encoding(false, true);
                    text = strictUtf8.GetString(bytes);
                    enc = new UTF8Encoding(false);
                }
                catch (DecoderFallbackException)
                {
                    enc = Encoding.GetEncoding(932); // Shift-JIS
                    text = enc.GetString(bytes);
                }
            }

            string nl = text.Contains("\r\n") || !text.Contains('\n') ? "\r\n" : "\n";
            bool endsNl = text.EndsWith("\n");
            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            // 最後の改行で生まれる空の行は取り除く(保存時に付け直す)
            if (endsNl) lines.RemoveAt(lines.Count - 1);
            return new IniFile(path, lines, enc, nl, endsNl || text.Length == 0);
        }

        /// <summary>
        /// ファイルに書き戻します(読み込んだときと同じ文字コード・改行)。
        /// </summary>
        public void Save()
        {
            string text = string.Join(newLine, lines) + (endsWithNewLine ? newLine : "");
            File.WriteAllText(path, text, encoding);
        }

        /// <summary>
        /// 別の場所・名前で保存します(中身・文字コード・改行は同じ)。以後の Save() もこの場所に保存します。
        /// </summary>
        public void SaveAs(string newPath)
        {
            path = newPath;
            Save();
        }

        // ---------- 読み取り ----------

        /// <summary>
        /// 値を返します(行末のコメントは除く)。書かれていなければ null。
        /// </summary>
        public string? Get(string section, string key)
        {
            int i = FindKey(section, key);
            if (i < 0) return null;
            SplitLine(lines[i], out _, out string value, out _, out _);
            return value;
        }

        /// <summary>
        /// セクション内の設定行(コメント行・空行以外)を、行末コメントも含めてそのまま返します。
        /// セクションがなければ null。
        /// </summary>
        public List<string>? GetSectionLines(string section)
        {
            if (!FindSection(section, out int start, out int end)) return null;
            var result = new List<string>();
            for (int i = start + 1; i < end; i++)
            {
                if (IsSettingLine(lines[i])) result.Add(lines[i].Trim());
            }
            return result;
        }

        // ---------- 書き込み ----------

        /// <summary>
        /// 値を書き換えます。キーがなければセクションの最後に、セクションもなければファイルの最後に追加します。
        /// </summary>
        public void Set(string section, string key, string value)
        {
            int i = FindKey(section, key);
            if (i >= 0)
            {
                // 「キー = 」の部分とコメントはそのままに、値だけ入れ替える
                SplitLine(lines[i], out string head, out string oldValue, out string gap, out string comment);
                if (comment == "")
                {
                    lines[i] = head + value;
                }
                else
                {
                    // コメントの位置がなるべくずれないように空白を調整(最低1つ)
                    int width = oldValue.Length + gap.Length;
                    string pad = new string(' ', Math.Max(1, width - value.Length));
                    lines[i] = head + value + pad + comment;
                }
                return;
            }

            if (FindSection(section, out int start, out int end))
            {
                // セクションの最後の空でない行の次に追加
                int insertAt = end;
                while (insertAt - 1 > start && lines[insertAt - 1].Trim() == "") insertAt--;
                string sep = SectionUsesSpaces(start, end) ? " = " : "=";
                lines.Insert(insertAt, key + sep + value);
            }
            else
            {
                if (lines.Count > 0 && lines[^1].Trim() != "") lines.Add("");
                lines.Add("[" + section + "]");
                lines.Add(key + "=" + value);
            }
        }

        /// <summary>
        /// キーの行を消します。
        /// </summary>
        public void Remove(string section, string key)
        {
            int i = FindKey(section, key);
            if (i >= 0) lines.RemoveAt(i);
        }

        /// <summary>
        /// セクション内の設定行をまとめて入れ替えます(コメントだけの行は残します)。
        /// newLines が空なら設定行を全部消します。
        /// </summary>
        public void ReplaceSectionLines(string section, List<string> newLines)
        {
            if (!FindSection(section, out int start, out int end))
            {
                if (newLines.Count == 0) return;
                if (lines.Count > 0 && lines[^1].Trim() != "") lines.Add("");
                lines.Add("[" + section + "]");
                lines.AddRange(newLines);
                return;
            }

            // 今の設定行を消して、最初の設定行があった位置に新しい行を入れる
            int insertAt = -1;
            for (int i = end - 1; i > start; i--)
            {
                if (IsSettingLine(lines[i]))
                {
                    lines.RemoveAt(i);
                    insertAt = i;
                }
            }
            if (insertAt < 0)
            {
                // 設定行がなかったときは、セクションの最後の空でない行の次
                FindSection(section, out start, out end);
                insertAt = end;
                while (insertAt - 1 > start && lines[insertAt - 1].Trim() == "") insertAt--;
            }
            lines.InsertRange(insertAt, newLines);
        }

        // ---------- 内部で使う道具 ----------

        private static bool IsCommentOrEmpty(string line)
        {
            string t = line.TrimStart();
            return t == "" || t.StartsWith(";") || t.StartsWith("#") || t.StartsWith("//");
        }

        private static bool IsSettingLine(string line)
        {
            return !IsCommentOrEmpty(line) && !line.TrimStart().StartsWith("[") && line.Contains('=');
        }

        /// <summary>
        /// セクション見出しの行(start)と、次のセクション見出しの行またはファイルの終わり(end)を探します。
        /// </summary>
        private bool FindSection(string section, out int start, out int end)
        {
            start = -1;
            end = lines.Count;
            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i].Trim();
                if (!t.StartsWith("[")) continue;
                int close = t.IndexOf(']');
                if (close < 0) continue;
                string name = t.Substring(1, close - 1).Trim();

                if (start < 0)
                {
                    if (name.Equals(section, StringComparison.OrdinalIgnoreCase)) start = i;
                }
                else
                {
                    end = i;
                    break;
                }
            }
            return start >= 0;
        }

        private int FindKey(string section, string key)
        {
            if (!FindSection(section, out int start, out int end)) return -1;
            for (int i = start + 1; i < end; i++)
            {
                if (!IsSettingLine(lines[i])) continue;
                string k = lines[i].Substring(0, lines[i].IndexOf('=')).Trim();
                if (k.Equals(key, StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        private bool SectionUsesSpaces(int start, int end)
        {
            for (int i = start + 1; i < end; i++)
            {
                if (IsSettingLine(lines[i])) return lines[i].Contains(" =") || lines[i].Contains("= ");
            }
            return false;
        }

        /// <summary>
        /// 「キー = 値   ; コメント」を、head(「キー = 」)・value・gap(値とコメントの間の空白)・comment に分けます。
        /// </summary>
        private static void SplitLine(string line, out string head, out string value, out string gap, out string comment)
        {
            int eq = line.IndexOf('=');
            int valueStart = eq + 1;
            while (valueStart < line.Length && (line[valueStart] == ' ' || line[valueStart] == '\t')) valueStart++;
            head = line.Substring(0, valueStart);

            string rest = line.Substring(valueStart);
            int semi = rest.IndexOf(';');
            comment = semi >= 0 ? rest.Substring(semi) : "";
            string body = semi >= 0 ? rest.Substring(0, semi) : rest;
            value = body.TrimEnd();
            gap = body.Substring(value.Length);
        }
    }
}
