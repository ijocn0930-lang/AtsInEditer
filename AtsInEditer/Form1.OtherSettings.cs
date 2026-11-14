using System.Globalization;
using System.Text;
using static AtsInEditer.Program;

namespace AtsInEditer
{
    /// <summary>
    /// 「その他の設定」タブの中身を、ATS_ini_params.csv から自動で作る部分です。
    /// (Form1 の続き。Form1.cs と同じ画面のプログラムです)
    /// </summary>
    public partial class Form1 : IMessageFilter
    {
        /// <summary>
        /// マウスホイールの動きを横取りして、「その他の設定」タブをスクロールします。
        /// (マウスが乗っている数字の欄や選択欄の値が、ホイールで勝手に変わるのも防ぎます)
        /// </summary>
        public bool PreFilterMessage(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg != WM_MOUSEWHEEL) return false;
            if (Form.ActiveForm != this) return false;              // 他の画面(バージョン情報など)のときは何もしない
            if (tabMain.SelectedTab != tabOther) return false;      // 「その他の設定」タブを表示中のときだけ

            // マウスが「その他の設定」タブの上にあるか
            Rectangle area = tabOther.RectangleToScreen(tabOther.ClientRectangle);
            if (!area.Contains(Cursor.Position)) return false;

            // 選択欄のリストを開いているときは、リストのスクロールを優先
            foreach (OtherField f in otherFields)
            {
                if (f.Input is ComboBox cmb && cmb.DroppedDown) return false;
            }

            // ホイール1目盛り(120)あたり、約60pxスクロールする
            int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
            int step = LogicalToDeviceUnits(60);
            int newY = -otherScroll.AutoScrollPosition.Y - delta / 120 * step;
            otherScroll.AutoScrollPosition = new Point(0, Math.Max(0, newY));
            return true; // ここで処理したので、他には渡さない
        }

        /// <summary>
        /// 自動で作った入力欄1つ分の情報
        /// </summary>
        private class OtherField
        {
            public ParamDef Def = null!;   // CSVの1行
            public string Section = "";    // 実際のセクション名(Gauge{n} → Gauge1 など)
            public Control Input = null!;  // 入力欄(NumericUpDown / CheckBox / ComboBox / TextBox)
            public List<Label> RowLabels = new(); // 同じ行のラベル(キー名・単位・説明)。グレーアウトで使う
        }

        // 「使える車両か」のチェックと、それで有効・無効を切り替えるグループ
        // (チェックを外すと、そのグループの入力欄がグレーアウトする)
        private static readonly (string Section, string Key, string Group)[] enableLinks =
        {
            ("VehicleSpec", "TascEnable", "TASC"),
            ("VehicleSpec", "AtoEnable", "ATO"),
        };

        /// <summary>
        /// TascEnable / AtoEnable のチェックに合わせて、TASC / ATO グループの有効・無効を切り替える仕組みを付けます。
        /// </summary>
        private void SetupEnableLinks()
        {
            foreach (var link in enableLinks)
            {
                OtherField? master = otherFields.FirstOrDefault(f => f.Section == link.Section && f.Def.Key == link.Key);
                if (master?.Input is not CheckBox chk) continue; // 一覧表にない場合は何もしない

                string group = link.Group;
                void ApplyEnabled()
                {
                    foreach (OtherField f in otherFields.Where(f => f.Def.Group == group))
                    {
                        f.Input.Enabled = chk.Checked;
                        foreach (Label l in f.RowLabels) l.Enabled = chk.Checked;
                    }
                }
                chk.CheckedChanged += (s, e) => ApplyEnabled();
                ApplyEnabled(); // 最初の状態を反映
            }
        }

        // 自動で作った入力欄の一覧(読み込み・保存で使う)
        private readonly List<OtherField> otherFields = new();

        // 一覧表(CSV)の全行。「基本設定」タブの既定値を調べるのにも使う
        private List<ParamDef> paramDefs = new();

        // 「基本設定」タブにすでにあるので、「その他の設定」には出さない項目
        private static readonly HashSet<string> basicTabKeys = new()
        {
            "VehicleSpec|MaxBrake", "VehicleSpec|AtcType",
            "LineSpec|MinLine", "LineSpec|MaxLine", "LineSpec|DefaultLine",
            "ATC|ATCFail", "ATC|FailParameter",
        };

        /// <summary>
        /// CSVを読み込み、「その他の設定」タブにグループごとの入力欄を並べます。
        /// </summary>
        private void BuildOtherTab()
        {
            // ★ タブの中を「上: ボタン列(スクロールしない)」と「下: 表を入れる枠(スクロールする)」に分ける
            SetupOtherTabLayout();

            string csvPath = Path.Combine(AppContext.BaseDirectory, "ATS_ini_params.csv");
            List<ParamDef> defs;
            try
            {
                defs = ParamDef.LoadCsv(csvPath);
                paramDefs = defs;
                ActionLogger.Log($"パラメーター一覧を読み込み: {defs.Count}件 ({csvPath})");
            }
            catch (Exception ex)
            {
                // CSVが見つからないときなどは、タブに案内を出すだけにする
                ActionLogger.Log($"パラメーター一覧の読み込みに失敗: {ex.Message}");
                btnCollapseAll.Enabled = false;
                btnExpandAll.Enabled = false;
                otherScroll.Controls.Add(new Label
                {
                    Text = "パラメーター一覧 (ATS_ini_params.csv) を読み込めませんでした。\n" + ex.Message,
                    AutoSize = true,
                    Location = new Point(10, 10),
                });
                return;
            }

            // 表(4列: キー名 / 入力欄 / 単位 / 説明)
            var table = new TableLayoutPanel
            {
                ColumnCount = 4,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new Point(0, 0),
                Padding = new Padding(6),
            };
            table.SuspendLayout();

            // ★ 並べる順番を決める。
            //   繰り返しのある項目(例: Gauge{n} の Source～Digits、1-8)は、
            //   「計器1の全項目 → 計器2の全項目 → …」の順になるように並べ替える。
            //   (一覧表のままだと「計器1のSource → 計器2のSource → …」となり、計器がばらばらになる)
            var shown = defs.Where(d => !basicTabKeys.Contains(d.Section + "|" + d.Key)).ToList();
            var entries = new List<(ParamDef Def, int N)>();
            for (int i = 0; i < shown.Count; )
            {
                ParamDef first = shown[i];
                if (first.Repeat == "")
                {
                    entries.Add((first, 0));
                    i++;
                    continue;
                }
                // 同じセクション・同じ繰り返しの項目をまとめる
                int j = i;
                while (j < shown.Count && shown[j].Section == first.Section && shown[j].Repeat == first.Repeat) j++;
                var block = shown.GetRange(i, j - i);
                foreach (int n in GetRepeatNumbers(first.Repeat))
                {
                    foreach (ParamDef d in block) entries.Add((d, n));
                }
                i = j;
            }

            string currentGroup = "";
            string currentParentGroup = "";
            Label? currentHeader = null;
            Label? currentParent = null;
            foreach (var (def, n) in entries)
            {
                {
                    string section = def.Section.Replace("{n}", n.ToString());
                    string groupTitle = def.Repeat == "" ? def.Group : $"{def.Group} {n}";

                    // ★ 繰り返しのあるグループ(計器)は、まとめの見出し「計器」の下に「計器 1」などの小見出しを置く
                    if (def.Repeat != "" && def.Group != currentParentGroup)
                    {
                        currentParent = AddGroupHeader(table, def.Group, null);
                        currentParentGroup = def.Group;
                        currentGroup = "";
                    }
                    else if (def.Repeat == "")
                    {
                        currentParent = null;
                        currentParentGroup = "";
                    }

                    // グループが変わったら見出しを入れる
                    if (groupTitle != currentGroup)
                    {
                        currentHeader = AddGroupHeader(table, groupTitle, currentParent);
                        currentGroup = groupTitle;
                    }

                    Control input = CreateInput(def);
                    AttachChangedEvent(input, section, def.Key);
                    List<Label> rowLabels = AddParamRow(table, def, section, input);
                    otherFields.Add(new OtherField { Def = def, Section = section, Input = input, RowLabels = rowLabels });

                    // 折り畳みのために、見出しの下の部品として覚えておく
                    if (currentHeader != null)
                    {
                        groupRows[currentHeader].Add(input);
                        groupRows[currentHeader].AddRange(rowLabels);
                    }
                }
            }

            table.ResumeLayout();
            otherScroll.Controls.Add(table);
            otherTable = table;

            // ★ 列の幅を、全部の行が見えている今の幅で固定する。
            //   (自動のままだと、グループを折り畳んだときに「見えている行」だけで幅を計算し直すので、
            //    他のグループの文字やチェックボックスの位置がずれてしまう)
            table.PerformLayout();
            int[] widths = table.GetColumnWidths();
            if (widths.Length == 4 && widths.All(w => w > 0)) // 幅が取れなかったときは自動のままにする
            {
                // 説明の列は、英語に切り替えても切れないよう、折り返しの最大幅(330)ぶんは必ず取る
                widths[3] = Math.Max(widths[3], LogicalToDeviceUnits(330) + LogicalToDeviceUnits(6));
                table.ColumnStyles.Clear();
                foreach (int w in widths)
                {
                    table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, w));
                }
                ActionLogger.Log($"その他の設定の列幅を固定: {string.Join(", ", widths)}");
            }

            // ★ TascEnable / AtoEnable で TASC / ATO の欄を有効・無効にする
            SetupEnableLinks();
        }

        // ===== INIファイルからの読み込み =====

        /// <summary>
        /// 開いたINIファイルから、「その他の設定」タブの値を読み込みます。
        /// INIに書かれていない項目は、一覧表の既定値に戻します。
        /// (行末の「; コメント」は値に含めません)
        /// </summary>
        private void LoadOtherFields()
        {
            var problems = new List<string>(); // 読めなかった値の一覧
            IniFile ini = IniFile.Load(iniPath);

            foreach (OtherField f in otherFields)
            {
                string? value;
                if (f.Def.Type == "special")
                {
                    // [PanelTiming] の全行(行末コメントも残す)
                    List<string>? sectionLines = ini.GetSectionLines(f.Section);
                    value = sectionLines == null ? null : string.Join("\r\n", sectionLines);
                }
                else
                {
                    value = ini.Get(f.Section, f.Def.Key); // 書かれていなければ null
                }

                if (!SetFieldValue(f, value))
                {
                    problems.Add($"[{f.Section}] {f.Def.Key} = {value}");
                }
            }

            ActionLogger.Log($"その他の設定を読み込み: {otherFields.Count}項目, 読めなかった値 {problems.Count}件");

            if (problems.Count > 0)
            {
                MessageBox.Show(
                    "次の値は読み取れなかったため、既定値を表示しています。\n\n" + string.Join("\n", problems.Take(20)) +
                    (problems.Count > 20 ? $"\n…ほか {problems.Count - 20} 件" : ""),
                    "読み込みの注意", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 入力欄に値を入れます。value が null なら既定値。読み取れない値なら既定値にして false を返します。
        /// </summary>
        private bool SetFieldValue(OtherField f, string? value)
        {
            bool ok = true;
            string text = value ?? f.Def.Default;

            switch (f.Input)
            {
                case NumericUpDown num:
                    if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal d))
                    {
                        ok = value == null; // 既定値が空欄のときはエラーにしない
                        decimal.TryParse(f.Def.Default, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
                    }
                    num.Value = Math.Max(num.Minimum, Math.Min(num.Maximum, d));
                    break;

                case CheckBox chk:
                    string b = text.ToLowerInvariant();
                    if (b == "true" || b == "1") chk.Checked = true;
                    else if (b == "false" || b == "0") chk.Checked = false;
                    else
                    {
                        ok = false;
                        chk.Checked = f.Def.Default.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                    break;

                case ComboBox cmb:
                    {
                        bool hasNone = f.Def.Default == ""; // 先頭に「(使わない)」がある
                        int offset = hasNone ? 1 : 0;
                        if (text == "")
                        {
                            cmb.SelectedIndex = 0; // 使わない
                            break;
                        }
                        int index = f.Def.Choices.FindIndex(c => c.Key.Equals(text, StringComparison.OrdinalIgnoreCase));
                        if (index < 0)
                        {
                            ok = false;
                            index = f.Def.Choices.FindIndex(c => c.Key == f.Def.Default);
                            cmb.SelectedIndex = index < 0 ? 0 : index + offset;
                        }
                        else
                        {
                            cmb.SelectedIndex = index + offset;
                        }
                        break;
                    }

                case TextBox txt:
                    txt.Text = text;
                    break;
            }
            return ok;
        }

        // ===== INIファイルへの保存 =====

        /// <summary>
        /// 「その他の設定」タブの値を、読み込んだINI(ini)に書き込みます。ファイルへの保存は呼び出し側で行います。
        /// ・INIにすでにあるキー → 値を書き換える
        /// ・INIにないキー → 既定値と違うときだけ書き足す(既定値のままなら書かない)
        /// ・「(使わない)」や空欄 → キーを消す
        /// </summary>
        private void SaveOtherFields(IniFile ini)
        {
            int written = 0, removed = 0;

            foreach (OtherField f in otherFields)
            {
                if (f.Def.Type == "special")
                {
                    SaveSpecialSection(ini, f);
                    continue;
                }

                string? value = GetFieldValue(f);
                string? current = ini.Get(f.Section, f.Def.Key); // 今INIに書かれている値(なければ null)

                if (value == null)
                {
                    // 消す(もともと書かれていなければ何もしない)
                    if (current != null)
                    {
                        ini.Remove(f.Section, f.Def.Key);
                        removed++;
                    }
                }
                else if (current != null)
                {
                    // 値が変わったときだけ書き換える(2.0 → 2 のような書き方の違いでは書き換えない)
                    if (!IsSameValue(f.Def.Type, current, value))
                    {
                        ini.Set(f.Section, f.Def.Key, value);
                        written++;
                    }
                }
                else if (!IsSameValue(f.Def.Type, f.Def.Default, value))
                {
                    // 書かれていないキーは、既定値と違うときだけ書き足す
                    ini.Set(f.Section, f.Def.Key, value);
                    written++;
                }
            }

            ActionLogger.Log($"その他の設定を保存: 書き込み {written}件, 削除 {removed}件");
        }

        // ===== 新規作成 =====

        /// <summary>
        /// 両方のタブを、一覧表(CSV)の既定値に戻します。
        /// </summary>
        private void ResetAllToDefaults()
        {
            // 「基本設定」タブ(一覧表に既定値がなければ今の値のまま)
            SetNumberFromDefault(numMaxBrake, "VehicleSpec", "MaxBrake");
            SetNumberFromDefault(numMinLine, "LineSpec", "MinLine");
            SetNumberFromDefault(numMaxLine, "LineSpec", "MaxLine");
            SetNumberFromDefault(numDefaultLine, "LineSpec", "DefaultLine");
            SetNumberFromDefault(numFailParameter, "ATC", "FailParameter");

            string? atcType = FindDefault("VehicleSpec", "AtcType");
            if (int.TryParse(atcType, out int atcIndex) && atcIndex >= 0 && atcIndex < cmbAtcType.Items.Count)
            {
                cmbAtcType.SelectedIndex = atcIndex;
            }
            string? atcFail = FindDefault("ATC", "ATCFail");
            if (atcFail != null) chkAtcFail.Checked = atcFail.Equals("true", StringComparison.OrdinalIgnoreCase);

            // 「その他の設定」タブ(値なし = 既定値)
            foreach (OtherField f in otherFields)
            {
                SetFieldValue(f, null);
            }
            ActionLogger.Log("すべての値を既定値に戻しました");
        }

        private string? FindDefault(string section, string key)
        {
            ParamDef? def = paramDefs.FirstOrDefault(d => d.Section == section && d.Key == key);
            return def == null || def.Default == "" ? null : def.Default;
        }

        /// <summary>
        /// 一覧表の既定値を数字の欄に入れます。欄の範囲を超えるときは、欄の範囲に収めます(落ちないように)。
        /// </summary>
        private void SetNumberFromDefault(NumericUpDown num, string section, string key)
        {
            if (decimal.TryParse(FindDefault(section, key), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal v))
            {
                num.Value = Math.Max(num.Minimum, Math.Min(num.Maximum, v));
            }
        }

        /// <summary>
        /// 新しいINIに「その他の設定」の項目を全部書きます(既定値のままでも書く)。
        /// ただし「(使わない)」を選んだ項目があるセクション(例: Source が使わないの計器)は、セクションごと書きません。
        /// </summary>
        private void WriteAllOtherFields(IniFile ini)
        {
            // 「使わない」を選んだ項目があるセクションを集める
            var unusedSections = otherFields
                .Where(f => f.Input is ComboBox && f.Def.Default == "" && GetFieldValue(f) == null)
                .Select(f => f.Section)
                .ToHashSet();

            foreach (OtherField f in otherFields)
            {
                if (unusedSections.Contains(f.Section)) continue;

                if (f.Def.Type == "special")
                {
                    SaveSpecialSection(ini, f); // 空欄なら何も書かない
                    continue;
                }

                string? value = GetFieldValue(f);
                if (value != null) ini.Set(f.Section, f.Def.Key, value); // 空欄の項目は書かない
            }
        }

        /// <summary>
        /// 保存の前に、数字の欄が一覧表の min～max に入っているかを調べます。
        /// 範囲外があれば一覧を出して「このまま保存しますか?」と聞きます。保存してよければ true。
        /// (既定値と同じ値は、範囲外でも注意しません。例: FailureRate の 0 は「故障しない」の意味)
        /// </summary>
        private bool ConfirmOtherRanges()
        {
            var outOfRange = new List<string>();
            foreach (OtherField f in otherFields)
            {
                if (f.Input is not NumericUpDown num) continue;

                decimal v = num.Value;
                bool tooSmall = f.Def.Min != null && v < f.Def.Min;
                bool tooLarge = f.Def.Max != null && v > f.Def.Max;
                if (!tooSmall && !tooLarge) continue;

                // 既定値と同じなら問題なしとする
                if (decimal.TryParse(f.Def.Default, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal def) && v == def) continue;

                string range = $"{f.Def.Min?.ToString(CultureInfo.InvariantCulture) ?? ""} ～ {f.Def.Max?.ToString(CultureInfo.InvariantCulture) ?? ""}";
                outOfRange.Add($"[{f.Section}] {f.Def.Key} = {GetFieldValue(f)}  (範囲: {range} {f.Def.Unit})");
            }

            // ★ 関係のある値どうしのチェック
            List<string> relation = CheckRelations();

            if (outOfRange.Count == 0 && relation.Count == 0) return true;

            ActionLogger.Log($"範囲外の値: {outOfRange.Count}件, 関係の問題: {relation.Count}件");
            string msg = "";
            if (outOfRange.Count > 0)
            {
                msg += isEnglish
                    ? "The following values are out of range:\n\n"
                    : "次の値が範囲外です。\n\n";
                msg += string.Join("\n", outOfRange.Take(20));
                if (outOfRange.Count > 20) msg += isEnglish ? $"\n...and {outOfRange.Count - 20} more" : $"\n…ほか {outOfRange.Count - 20} 件";
            }
            if (relation.Count > 0)
            {
                if (msg != "") msg += "\n\n";
                msg += isEnglish
                    ? "The following values do not match each other:\n\n"
                    : "次の値の組み合わせに問題があります。\n\n";
                msg += string.Join("\n", relation.Take(20));
                if (relation.Count > 20) msg += isEnglish ? $"\n...and {relation.Count - 20} more" : $"\n…ほか {relation.Count - 20} 件";
            }
            msg += isEnglish
                ? "\n\nDo you want to save anyway?"
                : "\n\nATSプラグインが正しく動かない可能性があります。このまま保存しますか？";

            DialogResult result = MessageBox.Show(msg, isEnglish ? "Check Values" : "値の確認",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            ActionLogger.Log($"範囲外のまま保存するか: {result}");
            return result == DialogResult.Yes;
        }

        /// <summary>
        /// 関係のある値どうしを調べて、問題の一覧を返します。
        /// ・list(カンマ区切り)の中に数字でないものがある
        /// ・TASC の ExtendedNotches が PressureRates の個数より多い
        /// ・ATO の DriverPowerMap に ExtendedPowerNotches より大きい数字がある
        /// ・同じセクションの Min が Max 以上 (計器・架線電圧・制御電圧)
        /// 「(使わない)」を選んだセクション(使わない計器など)は調べません。
        /// </summary>
        private List<string> CheckRelations()
        {
            var problems = new List<string>();

            // 使わないセクション(例: Source が「使わない」の計器)
            var unusedSections = otherFields
                .Where(f => f.Input is ComboBox && f.Def.Default == "" && GetFieldValue(f) == null)
                .Select(f => f.Section)
                .ToHashSet();

            OtherField? Find(string section, string key) =>
                otherFields.FirstOrDefault(f => f.Section == section && f.Def.Key == key);

            // 1. list の中身が数字か
            foreach (OtherField f in otherFields.Where(f => f.Def.Type == "list" && !unusedSections.Contains(f.Section)))
            {
                List<string>? items = SplitList(GetFieldValue(f));
                if (items == null) continue;
                var bad = items.Where(s => !decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _)).ToList();
                if (bad.Count > 0)
                {
                    problems.Add(isEnglish
                        ? $"[{f.Section}] {f.Def.Key}: not a number: {string.Join(", ", bad.Take(5))}"
                        : $"[{f.Section}] {f.Def.Key}: 数字でないものがあります: {string.Join(", ", bad.Take(5))}");
                }
            }

            // 2. TASC: ExtendedNotches は PressureRates の個数以下
            if (Find("TASC", "ExtendedNotches")?.Input is NumericUpDown ext && ext.Value > 0)
            {
                OtherField? rates = Find("TASC", "PressureRates");
                int count = rates == null ? 0 : (SplitList(GetFieldValue(rates))?.Count ?? 0);
                if (count < ext.Value)
                {
                    problems.Add(isEnglish
                        ? $"[TASC] ExtendedNotches = {ext.Value} but PressureRates has only {count} values"
                        : $"[TASC] ExtendedNotches = {ext.Value} ですが、PressureRates は {count} 個です({ext.Value} 個以上必要)");
                }
            }

            // 2-2. ATO: DriverPowerMap の数字は ExtendedPowerNotches 以下
            //      (ExtendedPowerNotches が 0 = 拡張ノッチを使わないときは調べない)
            if (Find("ATO", "ExtendedPowerNotches")?.Input is NumericUpDown extPower && extPower.Value > 0)
            {
                OtherField? map = Find("ATO", "DriverPowerMap");
                List<string>? items = map == null ? null : SplitList(GetFieldValue(map));
                if (items != null)
                {
                    var tooLarge = items
                        .Where(s => decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal v) && v > extPower.Value)
                        .ToList();
                    if (tooLarge.Count > 0)
                    {
                        problems.Add(isEnglish
                            ? $"[ATO] DriverPowerMap has values larger than ExtendedPowerNotches ({extPower.Value}): {string.Join(", ", tooLarge.Take(5))}"
                            : $"[ATO] DriverPowerMap に ExtendedPowerNotches ({extPower.Value}) より大きい数字があります: {string.Join(", ", tooLarge.Take(5))}");
                    }
                }
            }

            // 3. 同じセクションの Min < Max
            foreach (OtherField fMin in otherFields.Where(f => f.Def.Key == "Min" && !unusedSections.Contains(f.Section)))
            {
                if (fMin.Input is not NumericUpDown nMin) continue;
                if (Find(fMin.Section, "Max")?.Input is not NumericUpDown nMax) continue;
                if (nMin.Value >= nMax.Value)
                {
                    problems.Add(isEnglish
                        ? $"[{fMin.Section}] Min ({nMin.Value}) must be less than Max ({nMax.Value})"
                        : $"[{fMin.Section}] Min ({nMin.Value}) は Max ({nMax.Value}) より小さくしてください");
                }
            }

            return problems;
        }

        /// <summary>
        /// "1, 3, 5" → ["1","3","5"]。空なら null。
        /// </summary>
        private static List<string>? SplitList(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return text.Split(',').Select(s => s.Trim()).Where(s => s != "").ToList();
        }

        /// <summary>
        /// 入力欄の値を、INIに書く文字にします。null は「書かない(消す)」。
        /// </summary>
        private string? GetFieldValue(OtherField f)
        {
            switch (f.Input)
            {
                case NumericUpDown num:
                    return f.Def.Type == "int"
                        ? decimal.Round(num.Value).ToString(CultureInfo.InvariantCulture)
                        : num.Value.ToString("0.##########", CultureInfo.InvariantCulture); // 余分な0を付けない

                case CheckBox chk:
                    return chk.Checked ? "true" : "false";

                case ComboBox cmb:
                    {
                        int offset = f.Def.Default == "" ? 1 : 0; // 先頭が「(使わない)」か
                        int index = cmb.SelectedIndex - offset;
                        if (index < 0 || index >= f.Def.Choices.Count) return null; // 使わない
                        return f.Def.Choices[index].Key;
                    }

                case TextBox txt:
                    string t = txt.Text.Trim();
                    return t == "" ? null : t;
            }
            return null;
        }

        /// <summary>
        /// 2つの値が同じかどうか。数字は 1.0 と 1 を同じ、bool は 1 と true を同じとみなす。
        /// </summary>
        private static bool IsSameValue(string type, string a, string b)
        {
            if ((type == "int" || type == "float") &&
                decimal.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal da) &&
                decimal.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal db))
            {
                return da == db;
            }
            if (type == "bool")
            {
                static string Norm(string s) => s.Trim().ToLowerInvariant() switch { "1" => "true", "0" => "false", var x => x };
                return Norm(a) == Norm(b);
            }
            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [PanelTiming] のように、キーが決まっていないセクションの設定行をまとめて入れ替えます。
        /// </summary>
        private void SaveSpecialSection(IniFile ini, OtherField f)
        {
            // 「番号 = 間隔, 遅延」の行だけを集める(空行は無視)
            var newLines = f.Input.Text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l != "")
                .ToList();

            // 中身が変わっていなければ何もしない
            List<string> oldLines = ini.GetSectionLines(f.Section) ?? new List<string>();
            if (oldLines.SequenceEqual(newLines)) return;

            ini.ReplaceSectionLines(f.Section, newLines);
            ActionLogger.Log($"[{f.Section}] を書き換え: {newLines.Count}行");
        }

        /// <summary>
        /// 入力欄が変更されたら「未保存の変更あり」にします。
        /// </summary>
        private void AttachChangedEvent(Control input, string section, string key)
        {
            EventHandler handler = (s, e) =>
            {
                ActionLogger.Log($"[{section}] {key} の値を変更");
                MarkAsModified();
            };
            switch (input)
            {
                case NumericUpDown num: num.ValueChanged += handler; break;
                case CheckBox chk: chk.CheckedChanged += handler; break;
                case ComboBox cmb: cmb.SelectedIndexChanged += handler; break;
                case TextBox txt: txt.TextChanged += handler; break;
            }
        }

        /// <summary>
        /// "1-8" → 1,2,...,8。空欄なら1回だけ(番号0)。
        /// </summary>
        private static IEnumerable<int> GetRepeatNumbers(string repeat)
        {
            string[] r = repeat.Split('-');
            if (r.Length == 2 && int.TryParse(r[0], out int from) && int.TryParse(r[1], out int to))
            {
                for (int i = from; i <= to; i++) yield return i;
            }
            else
            {
                yield return 0;
            }
        }

        // ===== グループの折り畳み =====

        // 画面の描き直しを一時的に止める/再開するための Windows の機能
        private const int WM_SETREDRAW = 0x000B;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // 見出しごとに、その下の行の部品(入力欄・ラベル)を覚えておく
        private readonly Dictionary<Label, List<Control>> groupRows = new();

        // 小見出し → それをまとめる見出し(例: 「計器 1」→「計器」)
        private readonly Dictionary<Label, Label> parentHeaderOf = new();

        /// <summary>
        /// グループの見出し行(太字・色付き)を追加します。クリックすると折り畳み/展開します。
        /// parent を指定すると、その見出しの下の「小見出し」になります(少し右に寄せ、色を薄くする)。
        /// </summary>
        private Label AddGroupHeader(TableLayoutPanel table, string title, Label? parent)
        {
            bool isChild = parent != null;
            var header = new Label
            {
                Text = "▼ " + title,
                Tag = title, // 折り畳みの印を付け直すときに使う
                AutoSize = false,
                Dock = DockStyle.Fill,
                Height = LogicalToDeviceUnits(isChild ? 22 : 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(this.Font, FontStyle.Bold),
                BackColor = isChild ? Color.Lavender : Color.LightSteelBlue,
                Margin = isChild ? new Padding(LogicalToDeviceUnits(16), 6, 0, 4) : new Padding(0, 10, 0, 4),
                Padding = new Padding(4, 0, 0, 0),
                Cursor = Cursors.Hand,
            };
            toolTip1.SetToolTip(header, "クリックで折り畳み / 展開");
            header.Click += (s, e) => ToggleGroup(header);

            groupRows[header] = new List<Control>();
            if (parent != null) parentHeaderOf[header] = parent;
            table.Controls.Add(header, 0, table.RowCount);
            table.SetColumnSpan(header, 4);
            table.RowCount++;
            return header;
        }

        // 今折り畳んでいる見出し
        private readonly HashSet<Label> closedHeaders = new();

        /// <summary>
        /// 見出しをクリックしたとき: そのグループの開く/閉じるを切り替えます。
        /// </summary>
        private void ToggleGroup(Label header)
        {
            bool open = closedHeaders.Contains(header); // 今閉じていれば開く
            SetGroupsOpen(new[] { header }, open);
            ActionLogger.Log($"グループ「{header.Tag}」を{(open ? "展開" : "折り畳み")}");
        }

        /// <summary>
        /// 指定した見出しのグループを、まとめて開く(open=true)/閉じる(open=false)。
        /// まとめの見出し(例: 「計器」)を閉じると、その下の小見出しと行もすべて隠れます。
        /// もう一度開くと、小見出しはそれぞれ前の開閉の状態に戻ります。
        /// </summary>
        private void SetGroupsOpen(IEnumerable<Label> headers, bool open)
        {
            var list = headers.ToList();
            if (list.Count == 0) return;

            // ★ 表(TableLayoutPanel)の並べ直しを一時停止してから、まとめて表示/非表示を切り替える。
            //   (止めないと、部品1つごとに表全体(約600部品)を並べ直すので、とても遅くなる)
            Control? table = list[0].Parent;

            // ★ ちらつき防止: 切り替えが終わるまで、タブの画面の描き直しを止める
            SendMessage(tabOther.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            table?.SuspendLayout();
            otherScroll.SuspendLayout();
            try
            {
                // 開閉の状態を覚える
                foreach (Label header in list)
                {
                    if (open) closedHeaders.Remove(header); else closedHeaders.Add(header);
                }

                // すべての見出しについて、見える/隠れるを決め直す
                // (同じ値を入れ直すだけなら、ほとんど時間はかからない)
                foreach (Label header in groupRows.Keys)
                {
                    bool parentOpen = !parentHeaderOf.TryGetValue(header, out Label? parent) || !closedHeaders.Contains(parent);
                    if (parent != null) header.Visible = parentOpen; // 小見出しは、まとめの見出しが開いているときだけ見える

                    bool rowsVisible = parentOpen && !closedHeaders.Contains(header);
                    foreach (Control c in groupRows[header]) c.Visible = rowsVisible;

                    header.Text = (closedHeaders.Contains(header) ? "▶ " : "▼ ") + header.Tag;
                }
            }
            finally
            {
                // 最後に1回だけ並べ直す
                table?.ResumeLayout(true);
                otherScroll.ResumeLayout(true);

                // 描き直しを再開して、タブ全体を1回で描き直す
                SendMessage(tabOther.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                tabOther.Invalidate(true);
                tabOther.Update();
            }
        }

        // ===== 「その他の設定」タブの上のボタン列 =====

        private readonly Panel otherScroll = new Panel();          // 表を入れる枠(スクロールする)
        private readonly Button btnCollapseAll = new Button();     // すべて折り畳む
        private readonly Button btnExpandAll = new Button();       // すべて展開

        /// <summary>
        /// タブの中を「上: ボタン列」「下: 表を入れる枠」に分けます。
        /// </summary>
        private void SetupOtherTabLayout()
        {
            otherScroll.Dock = DockStyle.Fill;
            otherScroll.AutoScroll = true;

            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(4, 4, 4, 0),
            };
            foreach (Button b in new[] { btnCollapseAll, btnExpandAll })
            {
                b.AutoSize = true;
                b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                bar.Controls.Add(b);
            }
            btnCollapseAll.Text = "すべて折り畳む";
            btnExpandAll.Text = "すべて展開";
            btnCollapseAll.Click += (s, e) =>
            {
                ActionLogger.Log("すべて折り畳む");
                SetGroupsOpen(groupRows.Keys, false);
                otherScroll.AutoScrollPosition = new Point(0, 0); // 一番上に戻す
            };
            btnExpandAll.Click += (s, e) =>
            {
                ActionLogger.Log("すべて展開");
                SetGroupsOpen(groupRows.Keys, true);
            };

            // 先に「枠(Fill)」、あとに「ボタン列(Top)」を入れ、枠を前に出すと、ボタン列の下に枠が並ぶ
            tabOther.Controls.Add(otherScroll);
            tabOther.Controls.Add(bar);
            otherScroll.BringToFront();
        }

        /// <summary>
        /// 1つのパラメーターの行(キー名 / 入力欄 / 単位 / 説明)を追加します。
        /// </summary>
        private List<Label> AddParamRow(TableLayoutPanel table, ParamDef def, string section, Control input)
        {
            var lblKey = new Label
            {
                Text = def.Key,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
            };
            var lblUnit = new Label
            {
                Text = def.Unit,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
            };
            var lblDesc = new Label
            {
                Text = GetDescription(def),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Color.DimGray,
                MaximumSize = new Size(LogicalToDeviceUnits(330), 0), // 長い説明は折り返す
            };

            // ツールチップ: [セクション] キー、範囲、既定値、説明
            string tip = BuildParamToolTip(def, section);
            toolTip1.SetToolTip(lblKey, tip);
            toolTip1.SetToolTip(input, tip);

            int row = table.RowCount;
            table.Controls.Add(lblKey, 0, row);
            table.Controls.Add(input, 1, row);
            table.Controls.Add(lblUnit, 2, row);
            table.Controls.Add(lblDesc, 3, row);
            table.RowCount++;

            return new List<Label> { lblKey, lblUnit, lblDesc };
        }

        // ===== 言語の切り替え(「その他の設定」タブ) =====

        private TableLayoutPanel? otherTable; // 自動で作った表

        /// <summary>
        /// 今の言語の説明文。英語のときに英語の説明がなければ、日本語を出します。
        /// </summary>
        private string GetDescription(ParamDef def)
        {
            return isEnglish && def.DescriptionEn != "" ? def.DescriptionEn : def.Description;
        }

        /// <summary>
        /// 入力欄・キー名のツールチップの文を作ります(今の言語で)。
        /// </summary>
        private string BuildParamToolTip(ParamDef def, string section)
        {
            string tip = $"[{section}] {def.Key}";
            if (def.Min != null || def.Max != null)
            {
                tip += $"\n{(isEnglish ? "Range" : "範囲")}: {def.Min?.ToString(CultureInfo.InvariantCulture) ?? ""} ～ {def.Max?.ToString(CultureInfo.InvariantCulture) ?? ""} {def.Unit}";
            }
            if (def.Default != "") tip += $"\n{(isEnglish ? "Default" : "既定値")}: {def.Default}";
            tip += "\n" + GetDescription(def);
            return tip;
        }

        /// <summary>
        /// 言語を切り替えたとき: 「その他の設定」タブの説明文とツールチップを、今の言語に入れ替えます。
        /// </summary>
        private void UpdateOtherTabLanguage()
        {
            if (otherTable == null) return; // 一覧表を読めなかったとき

            // 説明文の書き換えで、表の並べ直しと描き直しが何度も起きないように止めておく
            SendMessage(tabOther.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            otherTable.SuspendLayout();
            try
            {
                foreach (OtherField f in otherFields)
                {
                    Label lblKey = f.RowLabels[0];
                    Label lblDesc = f.RowLabels[2];
                    lblDesc.Text = GetDescription(f.Def);

                    string tip = BuildParamToolTip(f.Def, f.Section);
                    toolTip1.SetToolTip(lblKey, tip);
                    toolTip1.SetToolTip(f.Input, tip);
                }
                foreach (Label header in groupRows.Keys)
                {
                    toolTip1.SetToolTip(header, isEnglish ? "Click to collapse / expand" : "クリックで折り畳み / 展開");
                }
            }
            finally
            {
                otherTable.ResumeLayout(true);
                SendMessage(tabOther.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                tabOther.Invalidate(true);
            }
        }

        /// <summary>
        /// type に合わせた入力欄を作り、既定値を入れます。
        /// int/float=数字、bool=チェック、enum=選択、list/special=文字
        /// </summary>
        private Control CreateInput(ParamDef def)
        {
            int width = LogicalToDeviceUnits(140);

            switch (def.Type)
            {
                case "int":
                case "float":
                    {
                        // 範囲外の値も一度は入力できるようにして、保存前に注意を出す
                        var num = new NumericUpDown
                        {
                            Width = width,
                            Minimum = -999999999m,
                            Maximum = 999999999m,
                            DecimalPlaces = def.Type == "int" ? 0 : GetDecimalPlaces(def),
                            Increment = def.Type == "int" ? 1m : 0.1m,
                            TextAlign = HorizontalAlignment.Right,
                        };
                        if (decimal.TryParse(def.Default, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal d))
                        {
                            num.Value = d;
                        }
                        return num;
                    }

                case "bool":
                    return new CheckBox
                    {
                        AutoSize = true,
                        Checked = def.Default.Equals("true", StringComparison.OrdinalIgnoreCase),
                    };

                case "enum":
                    {
                        var cmb = new ComboBox
                        {
                            Width = width,
                            DropDownStyle = ComboBoxStyle.DropDownList,
                        };
                        // 既定値がないもの(書かなければ使わない)には「使わない」を用意
                        if (def.Default == "") cmb.Items.Add("(使わない)");
                        foreach (var choice in def.Choices)
                        {
                            cmb.Items.Add($"{choice.Key}: {choice.Value}");
                        }
                        int index = def.Choices.FindIndex(c => c.Key == def.Default);
                        cmb.SelectedIndex = def.Default == "" ? 0 : Math.Max(0, index);
                        cmb.DropDownWidth = LogicalToDeviceUnits(220);
                        return cmb;
                    }

                case "special":
                    // パネル番号 = 更新間隔ms, 反映遅延ms を1行ずつ書く
                    return new TextBox
                    {
                        Width = width,
                        Height = LogicalToDeviceUnits(80),
                        Multiline = true,
                        ScrollBars = ScrollBars.Vertical,
                    };

                default: // list など
                    return new TextBox
                    {
                        Width = width,
                        Text = def.Default,
                    };
            }
        }

        /// <summary>
        /// float の小数点以下の桁数を、既定値・最小値・最大値の細かさから決めます(1～10桁)。
        /// </summary>
        private static int GetDecimalPlaces(ParamDef def)
        {
            int places = 1;
            foreach (string s in new[] { def.Default, def.Min?.ToString(CultureInfo.InvariantCulture) ?? "", def.Max?.ToString(CultureInfo.InvariantCulture) ?? "" })
            {
                int dot = s.IndexOf('.');
                if (dot >= 0) places = Math.Max(places, s.Length - dot - 1);
            }
            return Math.Min(places, 10);
        }
    }
}
