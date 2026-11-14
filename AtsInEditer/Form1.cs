using Microsoft.VisualBasic; // FileInfo や UnauthorizedAccessException を使用するために必要です
using System;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static AtsInEditer.Program;

namespace AtsInEditer
{
    public partial class Form1 : Form
    {
        // ★ 現在開いているファイルの排他ロック用ストリーム
        private FileStream? fileLockStream = null;
        // --- Windows API の呼び出し設定 (INIファイル読込/書込) ---
        // ★ Win32 API (kernel32.dll) の GetPrivateProfileIntA 宣言を追加
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, EntryPoint = "GetPrivateProfileIntA")]
        private static extern uint GetPrivateProfileIntA(
            string lpAppName,
            string lpKeyName,
            int nDefault,
            string lpFileName
        );
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, EntryPoint = "GetPrivateProfileStringA")]
        private static extern uint GetPrivateProfileStringA(
    string lpAppName,
    string lpKeyName,
    string lpDefault,
    StringBuilder lpReturnedString,
    uint nSize,
    string lpFileName
);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        private bool WriteIni(string section, string key, string value)
        {
            long result = WritePrivateProfileString(section, key, value, this.iniPath);
            return result != 0; // 0以外なら成功 (true)
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        // 選択されたINIファイルのパスを保持する変数
        private string iniPath = "";
        /// <summary>
        /// 指定されたファイルを「他プロセスからの書き込み・削除禁止」でロックします。
        /// </summary>
        private bool LockFile(string filePath)
        {
            // 既にロックしているストリームがあれば先に解除
            UnlockFile();

            try
            {
                // FileShare.Read: 他プロセスの「読み込み」のみ許可し、「書き込み」「削除」をブロック
                fileLockStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                ActionLogger.Log($"ファイルロック成功: {filePath}");
                return true;
            }
            catch (IOException ex)
            {
                // 既に他プロセスで排他ロックされている場合など
                ActionLogger.Log($"ファイルロック失敗 (他で開かれています): {ex.Message}");
                string msg = isEnglish
                    ? "The file is currently being used by another process."
                    : "指定されたファイルは他のプログラムによって使用されています。";
                MessageBox.Show(msg, isEnglish ? "File Lock Error" : "ファイルアクセスエラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        /// <summary>
        /// 現在保持しているファイルロックを解除します。
        /// </summary>
        private void UnlockFile()
        {
            if (fileLockStream != null)
            {
                fileLockStream.Dispose(); // または .Close();
                fileLockStream = null;
                ActionLogger.Log("ファイルロック解除");
            }
        }

        public Form1()
        {
            InitializeComponent();

            // Shift-JIS (CP932) を使うためのコードページ登録
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // ★ 今の画面を「基本設定」タブに入れ、「その他の設定」タブを追加する
            SetupTabs();

            // ★ ファイルメニューに「名前を付けて保存」を追加する
            SetupSaveAsMenu();

            // ★ 号線の番号の横に路線名を出す
            SetupLineNameLabels();

            // ★ 起動したときの値を、一覧表(CSV)の既定値にそろえる
            //   (これをしないと ATCタイプ が空のままになり、ファイルを開かずに
            //    新規作成・名前を付けて保存をすると AtcType が書かれなかった)
            ResetAllToDefaults();
            isModified = false;
        }

        // ★ 「名前を付けて保存」メニュー(画面の設計ファイルは触らず、ここで作る)
        private ToolStripMenuItem 名前を付けて保存ToolStripMenuItem = new ToolStripMenuItem();

        private void SetupSaveAsMenu()
        {
            名前を付けて保存ToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+S";
            名前を付けて保存ToolStripMenuItem.Click += 名前を付けて保存_Click;
            // 「上書き保存」のすぐ下に入れる
            int index = ファイルToolStripMenuItem.DropDownItems.IndexOf(保存ToolStripMenuItem);
            ファイルToolStripMenuItem.DropDownItems.Insert(index + 1, 名前を付けて保存ToolStripMenuItem);
        }

        // ★ タブ(基本設定 / その他の設定)
        private TabControl tabMain = new TabControl();
        private TabPage tabBasic = new TabPage();
        private TabPage tabOther = new TabPage();

        /// <summary>
        /// 画面上の部品(メニュー以外)を「基本設定」タブへ移し、空の「その他の設定」タブを作ります。
        /// 部品の見た目と位置は今のままです。
        /// </summary>
        private void SetupTabs()
        {
            int menuHeight = menuStrip1.Height;

            // メニュー以外の部品を集める
            var moveList = new List<Control>();
            foreach (Control c in this.Controls)
            {
                if (c != menuStrip1) moveList.Add(c);
            }

            // 「基本設定」タブへ移す(メニューの高さ分だけ上にずらす)
            foreach (Control c in moveList)
            {
                this.Controls.Remove(c);
                c.Top -= menuHeight;
                tabBasic.Controls.Add(c);
            }

            // 「その他の設定」タブ。中身は一覧表(CSV)から自動で作る。入りきらないときはスクロール
            // (スクロールは、タブの中の「表を入れる枠」で行う。上のボタン列はスクロールしない)
            tabOther.AutoScroll = false;
            BuildOtherTab();

            // マウスホイールで「その他の設定」タブをスクロールできるようにする(画面を閉じたら解除)
            Application.AddMessageFilter(this);
            this.FormClosed += (s, e) => Application.RemoveMessageFilter(this);

            tabMain.TabPages.Add(tabBasic);
            tabMain.TabPages.Add(tabOther);
            tabMain.Dock = DockStyle.Fill;

            // タブの見出し分だけ画面を縦に広げる
            this.ClientSize = new Size(this.ClientSize.Width, this.ClientSize.Height + LogicalToDeviceUnits(40));

            this.Controls.Add(tabMain);
            tabMain.BringToFront(); // メニューの下に配置されるようにする
        }

        // --- 1. INIファイルを選択するボタン ---
        private void button1_Click(object sender, EventArgs e)
        {
            ActionLogger.Log("ファイルを開くをクリック");
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (isEnglish)
                {
                    ofd.Filter = "INI Files (*.ini)|*.ini|All Files (*.*)|*.*";
                    ofd.Title = "Please select the INI file for the ATS plugin";
                }
                else
                {
                    ofd.Filter = "INI ファイル (*.ini)|*.ini|すべてのファイル (*.*)|*.*";
                    ofd.Title = "ATSプラグインのINIファイルを選択してください";
                }

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    iniPath = ofd.FileName;
                    txtFilePath.Text = iniPath;

                    // 念のため画面の表示を即時更新する
                    txtFilePath.Refresh();

                    // ファイル選択後、自動的に読み込みを実行
                    LoadIniData();
                }
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void txtFilePath_TextChanged(object sender, EventArgs e)
        {
            // 開いているファイルが変わったら、タイトルバーのファイル名も変える
            UpdateTitle();

        }

        /// <summary>
        /// INIから整数を読みます。書かれていない・数字でないときは defaultValue。
        /// (前の Windows 標準機能と同じく、「8 ; コメント」なら 8 と読みます)
        /// </summary>
        private static int GetIniInt(IniFile ini, string section, string key, int defaultValue)
        {
            string? s = ini.Get(section, key);
            return int.TryParse(s?.Trim(), out int v) ? v : defaultValue;
        }

        // --- 2. INIファイルからデータを読み込む処理 ---
        private void LoadIniData()
        {
            // ★ 読み込み前にファイルをロックする
            if (!LockFile(iniPath))
            {
                return; // ロック失敗時は処理を中断
            }
            ActionLogger.Log($"INIファイルを読み込み開始: {iniPath}");
            if (string.IsNullOrEmpty(iniPath)) return;

            // ★ INIを1行ずつ読み込む(行末の「; コメント」は値に含めない。文字コードも自動判別)
            IniFile ini = IniFile.Load(iniPath);

            // --- セクション存在チェック ---
            // [VehicleSpec] の MaxBrake が書かれていなければ "__NOT_FOUND__" になります
            string checkSection = ini.Get("VehicleSpec", "MaxBrake") ?? "__NOT_FOUND__";
            // MinLineとMaxLineが記述されていない場合はデフォルトの値を使用するため、ここでは__NOT_FOUND__をチェックしない。

            // --- [LineSpec] セクションの読み込み (既定値は一覧表(CSV)と同じ 1, 1, 9) ---
            int defaultLine = GetIniInt(ini, "LineSpec", "DefaultLine", 1);
            int minLine = GetIniInt(ini, "LineSpec", "MinLine", 1);
            int maxLine = GetIniInt(ini, "LineSpec", "MaxLine", 9);

            // 範囲外（1〜9以外）の値がINIに入っていた場合のガード
            numDefaultLine.Value = Math.Max(1, Math.Min(9, defaultLine));
            numMinLine.Value = Math.Max(1, Math.Min(9, minLine));
            numMaxLine.Value = Math.Max(1, Math.Min(9, maxLine));

            ActionLogger.Log($"[LineSpec] 読み込み完了: Default={defaultLine}, Min={minLine}, Max={maxLine}");

            // MaxBrakeキー自体がない場合も考慮し、もうひとつ別のキー(またはダミー)でもチェックするか、
            // セクション全体が存在するか判定します
            if (checkSection == "__NOT_FOUND__")
            {
                // セクションが存在しない場合、エラーを出して処理を中断する
                MessageBox.Show(
                    "選択されたINIファイルには [VehicleSpec] と [LineSpec] セクションが存在しません。\n正しいATSプラグイン用のINIファイルを選択してください。",
                    "ファイル読み込みエラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                // 画面の数値をリセットまたはパス表示をクリア（お好みで調整してください）
                iniPath = "";
                txtFilePath.Text = ""; // (iniPath を先に空にしてから。タイトルバーの表示を正しくするため)
                return; // ここで読み込み処理を打ち切る
            }

            // --- 2. MaxBrake の読み込みと数値範囲チェック ---
            // 例: MaxBrake の許容範囲を 1 ～ 13（ NumericUpDown の設定値に合わせて調整してください）
            int minMaxBrake = (int)numMaxBrake.Minimum;
            int maxMaxBrake = (int)numMaxBrake.Maximum;

            string maxBrakeStr = ini.Get("VehicleSpec", "MaxBrake") ?? "";

            // 数値変換チェック (intに変換できるか ＆ 範囲内か)
            if (int.TryParse(maxBrakeStr, out int maxBrakeVal))
            {
                if (maxBrakeVal < minMaxBrake || maxBrakeVal > maxMaxBrake)
                {
                    MessageBox.Show(
                        $"MaxBrake の値 ({maxBrakeVal}) が範囲外です。\n{minMaxBrake} ～ {maxMaxBrake} の範囲の値を設定してください。",
                        "ファイルを読み込めません",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return; // 読み込み中断
                }
                numMaxBrake.Value = maxBrakeVal;
            }
            else
            {
                MessageBox.Show(
                    $"MaxBrake の値 (\"{maxBrakeStr}\") は不正な値です。数値形式で指定してください。",
                    "ファイルを読み込めません",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return; // 読み込み中断
            }

            // --- 3. AtcType の読み込みと数値範囲チェック ---
            // 許容範囲: 0 ～ 2
            string atcTypeStr = ini.Get("VehicleSpec", "AtcType") ?? "";

            if (int.TryParse(atcTypeStr, out int atcTypeVal))
            {
                if (atcTypeVal < 0 || atcTypeVal > 2)
                {
                    MessageBox.Show(
                        $"AtcType の値 ({atcTypeVal}) が範囲外です。\n0 ～ 2 の範囲の値を設定してください。",
                        "ファイルを読み込めません",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return; // 読み込み中断
                }
                cmbAtcType.SelectedIndex = atcTypeVal;
            }
            else
            {
                MessageBox.Show(
                    $"AtcType の値 (\"{atcTypeStr}\") は不正な値です。数値形式で指定してください。",
                    "ファイルを読み込めません",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return; // 読み込み中断
            }
            // --- [ATC] セクションの読み込み ---
            // 1. ATCFail の読み込み (既定は "false")
            string atcFailStr = (ini.Get("ATC", "ATCFail") ?? "false").Trim();
            bool atcFail = atcFailStr == "1" || (bool.TryParse(atcFailStr, out bool parsedBool) && parsedBool);

            // 2. FailParameter の読み込み (既定は "0")
            string failParamStr = ini.Get("ATC", "FailParameter") ?? "0";

            decimal failParam = 0m;
            if (decimal.TryParse(failParamStr.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedDecimal))
            {
                failParam = NormalizeFailParameter(parsedDecimal);
            }

            // UIへ反映
            chkAtcFail.Checked = atcFail;
            numFailParameter.Value = failParam;

            // グレーアウト状態の更新
            chkAtcFail_CheckedChanged(chkAtcFail, EventArgs.Empty);

            ActionLogger.Log($"[ATC] 読み込み完了: ATCFail={atcFail}, FailParameter={failParam}");

            // ★ 「その他の設定」タブの値を読み込む
            LoadOtherFields();

            isModified = false;
            MessageBox.Show("設定を読み込みました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        // FailParameter の丸め・しきい値補正ヘルパーメソッド
        private decimal NormalizeFailParameter(decimal val)
        {
            // 1.0 超は 1.0 に丸める
            if (val > 1.0m) return 1.0m;

            // 0.0000001 (10^-7) 未満は計算発散防止のため 0 にする
            if (val < 0.0000001m) return 0.0m;

            return val;
        }
        private void numMaxBrake_ValueChanged(object sender, EventArgs e)
        {
            ActionLogger.Log($"MaxBrake の値を変更: {numMaxBrake.Value}");
            isModified = true;
            MarkAsModified();
        }

        // 手動で読み込みボタンを押した場合
        private void btnLoad_Click_1(object sender, EventArgs e)
        {
            LoadIniData();
        }

        // --- ヘルパー関数: INI読み込み ---
        private string ReadIni(string section, string key, string defaultValue)
        {
            StringBuilder sb = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, sb, 255, iniPath);
            return sb.ToString();
        }

        // 変更フラグ（未保存の変更があるか）
        // ★ 値を変えたり保存したりするたびに、タイトルバーの「*」も自動で付け外しする
        private bool isModifiedValue = false;
        private bool isModified
        {
            get => isModifiedValue;
            set
            {
                isModifiedValue = value;
                UpdateTitle();
            }
        }

        // タイトルバーのアプリ名(言語で変わる)
        private string appTitle = "";

        /// <summary>
        /// タイトルバーを「アプリ名 - *ファイル名」の形にします。
        /// ・* は保存していない変更があるときだけ付く
        /// ・ファイルを開いていないときは「無題」(英語: Untitled)
        /// </summary>
        private void UpdateTitle()
        {
            if (appTitle == "") return; // 言語の設定前(起動の途中)は何もしない
            string fileName = string.IsNullOrEmpty(iniPath)
                ? (isEnglish ? "Untitled" : "無題")
                : Path.GetFileName(iniPath);
            this.Text = $"{appTitle} - {(isModifiedValue ? "*" : "")}{fileName}";
        }

        // 設定値が変更されたときに呼ぶメソッド
        private void MarkAsModified()
        {
            // ★ ファイルを開いていなくても(起動直後でも)変更フラグを立てる。
            //   (前は「ファイルを開いているときだけ」だったので、起動直後に値を変えて閉じても
            //    「変更内容が保存されていません」が出なかった)
            //   起動時・読み込み時・新規作成時などに、プログラムが値を入れたあとは isModified = false に戻している
            isModified = true;
        }


        private void cmbAtcType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (updatingAtcTypeItems) return; // 言語切り替えで名前を入れ替えているときは「変更」にしない
            ActionLogger.Log($"AtcType の値を変更: {cmbAtcType.SelectedIndex}");
            MarkAsModified();
        }

        // ATCタイプの選択肢の名前を入れ替えている最中か
        private bool updatingAtcTypeItems = false;

        /// <summary>
        /// ATCタイプの選択肢を「番号: 名前」で表示します(言語に合わせて入れ替え)。
        /// 選んでいる位置(0/1/2)はそのままなので、保存される値は今までと同じです。
        /// </summary>
        private void UpdateAtcTypeItems()
        {
            string[] names = isEnglish
                ? new[] { "0: WS-ATC only", "1: CS-ATC only", "2: Both" }
                : new[] { "0: WS-ATCのみ", "1: CS-ATCのみ", "2: 両方" };

            updatingAtcTypeItems = true;
            try
            {
                int index = cmbAtcType.SelectedIndex;
                cmbAtcType.Items.Clear();
                cmbAtcType.Items.AddRange(names);
                cmbAtcType.SelectedIndex = index; // 選んでいた位置に戻す(-1 なら未選択のまま)
            }
            finally
            {
                updatingAtcTypeItems = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // ★ 前回選んだ言語に戻す(覚えていなければ日本語)
            isEnglish = AppSettings.LoadIsEnglish() ?? false;
            UpdateLanguage(); // 言語設定を初期化

            // ★ ウィンドウの大きさを変えられるようにする
            //   (画面の並べ方が決まったあとでないと、右端からの距離が正しく取れないので、ここで行う)
            SetupResizable();

            // ★ 前回閉じたときのウィンドウの位置と大きさに戻す(閉じるときに覚える)
            RestoreWindowBounds();
            this.FormClosed += (s, e2) => SaveWindowBounds();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void btnCreateNew_Click(object sender, EventArgs e)
        {
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 未保存の変更がある場合
            if (isModified)
            {
                DialogResult result = MessageBox.Show(
                    "変更内容が保存されていません。\n保存せずに終了すると変更が失われます。よろしいでしょうか？",
                    "確認",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                // 「いいえ」が押された場合、フォームを閉じる処理をキャンセルする
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
                // ★ アプリ終了前にロックを完全に解除
                UnlockFile();
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            ActionLogger.Log($"キー入力検知: Ctrl={e.Control}, KeyCode={e.KeyCode}");
            // Ctrl + Shift + S : 名前を付けて保存(Ctrl + S より先に調べる)
            if (e.Control && e.Shift && e.KeyCode == Keys.S)
            {
                名前を付けて保存ToolStripMenuItem.PerformClick();
                e.SuppressKeyPress = true;
            }
            // Ctrl + S : 上書き保存
            else if (e.Control && e.KeyCode == Keys.S)
            {
                保存ToolStripMenuItem.PerformClick(); // メニュー項目を直接呼び出す
                e.SuppressKeyPress = true;
            }
            // Ctrl + O : ファイルを開く
            else if (e.Control && e.KeyCode == Keys.O)
            {
                開くToolStripMenuItem.PerformClick();
                e.SuppressKeyPress = true;
            }
            // Ctrl + N : 新規作成
            else if (e.Control && e.KeyCode == Keys.N)
            {
                新規作成ToolStripMenuItem.PerformClick();
                e.SuppressKeyPress = true;
            }
        }

        private void 開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionLogger.Log("ファイルを開くをクリック");
            // 既存のファイル選択ボタンの処理を直接呼び出す
            // (PerformClick だと、ボタンが見えないタブを表示中のときに何も起きないため)
            button1_Click(sender, e);
        }

        private void 新規作成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionLogger.Log("新規作成メニューがクリックされました");
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                if (isEnglish)
                {
                    sfd.Filter = "INI Files (*.ini)|*.ini|All Files (*.*)|*.*";
                    sfd.Title = "Specify the save location and name for the new INI file";
                }
                else
                {
                    sfd.Filter = "INI ファイル (*.ini)|*.ini|すべてのファイル (*.*)|*.*";
                    sfd.Title = "新規作成するINIファイルの保存先と名前を指定してください";
                }

                sfd.DefaultExt = "ini";
                sfd.FileName = "Detail.ini"; // デフォルトのファイル名（適宜変更してください）

                if (sfd.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                // ★ 今の画面に未保存の変更があれば、消えてよいか確認する
                if (isModified)
                {
                    string confirmMsg = isEnglish
                        ? "Unsaved changes will be lost because all values are reset to defaults. Continue?"
                        : "保存していない変更があります。新規作成するとすべての値が既定値に戻り、変更は失われます。続けますか？";
                    if (MessageBox.Show(confirmMsg, isEnglish ? "Confirm" : "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        return;
                    }
                }

                try
                {
                    // 前に開いていたファイルのロックを外す(同じファイルを選んだときに書き込めるように)
                    UnlockFile();

                    // ★ 両方のタブを一覧表(CSV)の既定値に戻す
                    ResetAllToDefaults();

                    // ★ 新しいINIを作り、全項目を書き込む
                    IniFile ini = IniFile.CreateNew(sfd.FileName);

                    // [VehicleSpec]
                    ini.Set("VehicleSpec", "MaxBrake", numMaxBrake.Value.ToString());
                    if (cmbAtcType.SelectedIndex >= 0)
                    {
                        ini.Set("VehicleSpec", "AtcType", cmbAtcType.SelectedIndex.ToString());
                    }
                    else if (FindDefault("VehicleSpec", "AtcType") is string atcDefault)
                    {
                        // 念のため: ATCタイプが選ばれていないときは一覧表の既定値を書く
                        ini.Set("VehicleSpec", "AtcType", atcDefault);
                    }
                    // [LineSpec]
                    ini.Set("LineSpec", "MinLine", numMinLine.Value.ToString());
                    ini.Set("LineSpec", "MaxLine", numMaxLine.Value.ToString());
                    ini.Set("LineSpec", "DefaultLine", numDefaultLine.Value.ToString());
                    // [ATC] 数値を最終確認（1.0超は1.0、0.0000001未満は0）。浮動小数は不変カルチャで出力
                    decimal finalFailParam = NormalizeFailParameter(numFailParameter.Value);
                    numFailParameter.Value = finalFailParam;
                    ini.Set("ATC", "ATCFail", chkAtcFail.Checked ? "true" : "false");
                    ini.Set("ATC", "FailParameter", finalFailParam.ToString(System.Globalization.CultureInfo.InvariantCulture));

                    // 「その他の設定」の全項目
                    WriteAllOtherFields(ini);

                    ini.Save();

                    // 新しいファイルを「開いている状態」にする
                    iniPath = sfd.FileName;
                    txtFilePath.Text = iniPath;
                    LockFile(iniPath);
                    isModified = false;
                    ActionLogger.Log($"INIファイルを新規作成しました: {iniPath}");

                    string successMessage = isEnglish
                        ? "A new INI file has been created with default values."
                        : "既定値で新しいINIファイルを作成しました。";
                    MessageBox.Show(successMessage, isEnglish ? "Created" : "作成完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ActionLogger.Log($"INIファイルの新規作成に失敗: {ex.Message}");
                    MessageBox.Show(
                        (isEnglish ? "Could not create the file:\n" : "ファイルを作成できませんでした:\n") + ex.Message,
                        isEnglish ? "Error" : "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void 保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionLogger.Log("上書き保存を開始");
            if (string.IsNullOrEmpty(iniPath) || !File.Exists(iniPath))
            {
                // ★ まだファイルを開いていないときは「名前を付けて保存」にする
                名前を付けて保存_Click(sender, e);
                return;
            }

            // 1. 保存前のチェック(範囲外・MaxBrake 0・号線)
            if (!ValidateBeforeSave())
            {
                return;
            }

            // 2. 読み取り専用属性のチェック
            try
            {
                FileInfo fileInfo = new FileInfo(iniPath);
                if (fileInfo.Exists && fileInfo.IsReadOnly)
                {
                    string errorMessage = isEnglish
                        ? "The specified file cannot be saved.\nAccess is denied."
                        : "指定されたファイルを保存できません。\nアクセスが拒否されました";
                    string errorTitle = isEnglish ? "Save Error" : "保存エラー";
                    MessageBox.Show(errorMessage, errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // 処理を中断
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "ファイル状態の確認中にエラーが発生しました:\n" + ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            // 3. 実際の書き込み(書き込む直前にロックを外す)
            UnlockFile();
            try
            {
                // ★ ファイル全体を作り直すのではなく、INIを1行ずつ読み込んで「値」の部分だけを書き換える。
                //   (他のセクション、コメント行、行末の「; コメント」はそのまま残る)
                IniFile ini = IniFile.Load(iniPath);
                WriteValuesToIni(ini);

                // ファイルに書き戻す(読み込んだときと同じ文字コード)
                ini.Save();

                // 保存成功時の処理
                isModified = false;
                ActionLogger.Log($"INIファイルを保存しました: {iniPath}");
                // ★ 保存完了後、再度排他ロックをかける
                LockFile(iniPath);
                string successMsg = isEnglish ? "Saved successfully!" : "保存が完了しました！";
                MessageBox.Show(successMsg, isEnglish ? "Success" : "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException)
            {
                // アクセス拒否（管理者権限が必要なフォルダへの書き込みなど）
                ActionLogger.Log($"INIファイルの保存に失敗（アクセス拒否）: {iniPath}");
                LockFile(iniPath);
                MessageBox.Show(
                    "アクセスが拒否されました。\n書き込み権限のある場所に移動するか、管理者権限で実行してください。",
                    "アクセス拒否",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch (Exception ex)
            {
                // 万が一失敗した場合も再ロックを試みる
                LockFile(iniPath);
                ActionLogger.Log($"INIファイルの保存に失敗（予期しないエラー）: {iniPath}");
                MessageBox.Show(
                    "保存中に予期しないエラーが発生しました:\n" + ex.Message,
                    "保存エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// 「名前を付けて保存」: 保存先を選んで、今の画面の値で保存します。
        /// 開いているファイルがあれば、その中身(コメントなど)を引き継いで別の名前で保存します。
        /// 保存したあとは、新しいファイルを開いている状態になります。
        /// </summary>
        private void 名前を付けて保存_Click(object? sender, EventArgs e)
        {
            ActionLogger.Log("名前を付けて保存を開始");
            using SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = isEnglish ? "INI Files (*.ini)|*.ini|All Files (*.*)|*.*" : "INI ファイル (*.ini)|*.ini|すべてのファイル (*.*)|*.*",
                Title = isEnglish ? "Save As" : "名前を付けて保存",
                DefaultExt = "ini",
                FileName = string.IsNullOrEmpty(iniPath) ? "ATS.ini" : Path.GetFileName(iniPath),
            };
            if (!string.IsNullOrEmpty(iniPath))
            {
                sfd.InitialDirectory = Path.GetDirectoryName(iniPath);
            }
            if (sfd.ShowDialog() != DialogResult.OK) return;

            // 保存前のチェック(範囲外・MaxBrake 0・号線)
            if (!ValidateBeforeSave()) return;

            string oldPath = iniPath;
            try
            {
                // 開いているファイルがあればその中身を元にする。なければ空から作る
                IniFile ini = (!string.IsNullOrEmpty(oldPath) && File.Exists(oldPath))
                    ? IniFile.Load(oldPath)
                    : IniFile.CreateNew(sfd.FileName);
                WriteValuesToIni(ini);

                UnlockFile(); // 同じファイルを選んだときでも書き込めるように
                ini.SaveAs(sfd.FileName);

                // 新しいファイルを「開いている状態」にする
                iniPath = sfd.FileName;
                txtFilePath.Text = iniPath;
                LockFile(iniPath);
                isModified = false;
                ActionLogger.Log($"名前を付けて保存しました: {iniPath}");

                string successMsg = isEnglish ? "Saved successfully!" : "保存が完了しました！";
                MessageBox.Show(successMsg, isEnglish ? "Success" : "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ActionLogger.Log($"名前を付けて保存に失敗: {ex.Message}");
                // 前のファイルのロックを元に戻す
                if (!string.IsNullOrEmpty(oldPath) && File.Exists(oldPath) && fileLockStream == null) LockFile(oldPath);
                MessageBox.Show(
                    (isEnglish ? "Could not save the file:\n" : "保存できませんでした:\n") + ex.Message,
                    isEnglish ? "Save Error" : "保存エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 画面(両方のタブ)の値を、INI に書き込みます(ファイルへの保存は呼び出し側)。
        /// </summary>
        private void WriteValuesToIni(IniFile ini)
        {
            // [VehicleSpec]
            ini.Set("VehicleSpec", "MaxBrake", numMaxBrake.Value.ToString());
            if (cmbAtcType.SelectedIndex >= 0)
            {
                ini.Set("VehicleSpec", "AtcType", cmbAtcType.SelectedIndex.ToString());
            }
            else if (FindDefault("VehicleSpec", "AtcType") is string atcDefault)
            {
                // 念のため: ATCタイプが選ばれていないときは一覧表の既定値を書く
                ini.Set("VehicleSpec", "AtcType", atcDefault);
            }

            // 数値を最終確認（1.0超は1.0、0.0000001未満は0）
            decimal finalFailParam = NormalizeFailParameter(numFailParameter.Value);
            numFailParameter.Value = finalFailParam;

            // [LineSpec]
            ini.Set("LineSpec", "DefaultLine", numDefaultLine.Value.ToString());
            ini.Set("LineSpec", "MinLine", numMinLine.Value.ToString());
            ini.Set("LineSpec", "MaxLine", numMaxLine.Value.ToString());
            // [ATC] 浮動小数の表記揺れを防ぐため不変カルチャで出力
            ini.Set("ATC", "ATCFail", chkAtcFail.Checked ? "true" : "false");
            ini.Set("ATC", "FailParameter", finalFailParam.ToString(System.Globalization.CultureInfo.InvariantCulture));

            // ★ 「その他の設定」タブの値
            SaveOtherFields(ini);
        }

        /// <summary>
        /// 保存前のチェック。保存してよければ true。
        /// (「その他の設定」の範囲外 / MaxBrake が 0 / 号線の大小関係)
        /// </summary>
        private bool ValidateBeforeSave()
        {
            // ★ 「その他の設定」の値が min～max の範囲外なら注意を出す(「いいえ」なら保存しない)
            if (!ConfirmOtherRanges())
            {
                return false;
            }

            // MaxBrake 0 チェック
            if (numMaxBrake.Value == 0)
            {
                string warningMessage = isEnglish
                    ? "MaxBrake is set to 0. Saving may cause ATS brakes to not function correctly. Do you want to continue?"
                    : "MaxBrakeが0に設定されています。保存するとATSブレーキが正しく動作しない可能性があります。続けますか？";
                string warningTitle = isEnglish ? "Warning" : "警告";
                DialogResult result = MessageBox.Show(warningMessage, warningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    return false;
                }
            }

            // MinLine が MaxLine を超えている場合のエラー
            if (numMinLine.Value > numMaxLine.Value)
            {
                string msg = isEnglish
                    ? "MinLine cannot be greater than MaxLine."
                    : "最小号線 (MinLine) は最大号線 (MaxLine) 以下の値にしてください。";
                MessageBox.Show(msg, isEnglish ? "Input Error" : "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // DefaultLine が範囲外の場合のエラー
            if (numDefaultLine.Value < numMinLine.Value || numDefaultLine.Value > numMaxLine.Value)
            {
                string msg = isEnglish
                    ? "DefaultLine must be between MinLine and MaxLine."
                    : "初期号線 (DefaultLine) は 最小号線〜最大号線 の範囲内で設定してください。";
                MessageBox.Show(msg, isEnglish ? "Input Error" : "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }
        private void 終了ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ActionLogger.Log("終了メニューがクリックされました");
            this.Close(); // 画面を閉じる（FormClosingの未保存チェックも自動で走ります）
        }

        private void バージョン情報ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionLogger.Log("バージョン情報ダイアログを表示");
            // AboutForm をモーダルダイアログ（閉じないと親画面に戻れない画面）として表示
            using (Form2 aboutForm = new Form2(isEnglish))
            {
                aboutForm.ShowDialog(); // モーダル表示
            }
        }

        private void 開くToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            ActionLogger.Log("ファイルを開くをクリック");
            // 既存のファイル選択ボタンの処理を直接呼び出す
            // (PerformClick だと、ボタンが見えないタブを表示中のときに何も起きないため)
            button1_Click(sender, e);
        }

        private void 日本語ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionLogger.Log("言語を Japanese に切り替え");
            isEnglish = false;
            UpdateLanguage();
            AppSettings.SaveIsEnglish(isEnglish); // ★ 次に起動したときもこの言語にする
        }

        private void 英語ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionLogger.Log("言語を English に切り替え");
            isEnglish = true;
            UpdateLanguage();
            AppSettings.SaveIsEnglish(isEnglish); // ★ 次に起動したときもこの言語にする
        }
        private bool isEnglish = false; // デフォルトは日本語
        private void UpdateLanguage()
        {
            日本語ToolStripMenuItem.Checked = !isEnglish;
            英語ToolStripMenuItem.Checked = isEnglish;
            if (isEnglish)
            {
                // 英語に切り替え
                appTitle = "ATS Plugin INI Editor";
                btnSelectFile.Text = "Select INI File";
                label1.Text = "ATC Type:";
                label2.Text = "Max Brake:";
                label3.Text = "This is a beta version. Please be careful when using it.";
                label4.Text = "Set vehicle parameters in the [Vehicle] section.";
                ファイルToolStripMenuItem.Text = "File(&F)";
                保存ToolStripMenuItem.Text = "Save(&S)";
                名前を付けて保存ToolStripMenuItem.Text = "Save As(&A)";
                開くToolStripMenuItem.Text = "Open(&O)";
                新規作成ToolStripMenuItem.Text = "New(&N)";
                終了ToolStripMenuItem1.Text = "Exit(&X)";
                バージョン情報ToolStripMenuItem.Text = "About(&A)";
                日本語ToolStripMenuItem.Text = "Japanese(&J)";
                英語ToolStripMenuItem.Text = "English(&E)";
                ヘルプToolStripMenuItem.Text = "Help";
                //CrashButton1.Text = "Test Crash"; //ユーザーが意図せずにクラッシュするの防ぐため、リリース版では削除してください
                grpLineSpec.Text = "Line [LineSpec]";
                grpVehicle.Text = "Vehicle [VehicleSpec]"; // 枠の幅に収まるよう短めに
                label5.Text = "Default Line";
                label6.Text = "Min Line";
                label7.Text = "Max Line";
                grpAtc.Text = "ATC Settings [ATC]";
                chkAtcFail.Text = "Enable ATC Failure (ATCFail)";
                lblFailParameter.Text = "Failure Probability (FailParameter):";
                tabBasic.Text = "Basic Settings";
                tabOther.Text = "Other Settings";
                btnCollapseAll.Text = "Collapse All";
                btnExpandAll.Text = "Expand All";
            }
            else
            {
                // 日本語に切り替え
                appTitle = "ATSプラグイン用INIエディタ";
                btnSelectFile.Text = "INIファイルを選択";
                label1.Text = "ATCタイプ:";
                label2.Text = "最大ブレーキ:";
                label3.Text = "現在、ベータ版バージョンです。不具合が出る可能性があります。注意して使用してください。";
                label4.Text = "[Vehicle]セクション　車両パラメーターを設定します。";
                ファイルToolStripMenuItem.Text = "ファイル(&F)";
                保存ToolStripMenuItem.Text = "上書き保存(&S)";
                名前を付けて保存ToolStripMenuItem.Text = "名前を付けて保存(&A)";
                開くToolStripMenuItem.Text = "開く(&O)";
                新規作成ToolStripMenuItem.Text = "新規作成(&N)";
                終了ToolStripMenuItem1.Text = "終了(&X)";
                バージョン情報ToolStripMenuItem.Text = "バージョン情報(&A)";
                日本語ToolStripMenuItem.Text = "日本語(&J)";
                英語ToolStripMenuItem.Text = "英語(&E)";
                ヘルプToolStripMenuItem.Text = "ヘルプ";
                //CrashButton1.Text = "クラッシュテスト"; //ユーザーが意図せずにクラッシュするの防ぐため、リリース版では削除してください
                grpLineSpec.Text = "[LineSpec]号線設定";
                grpVehicle.Text = "[VehicleSpec] 車両設定";
                label5.Text = "初期号線";
                label6.Text = "最小号線";
                label7.Text = "最大号線";
                grpAtc.Text = "ATC設定 [ATC]";
                chkAtcFail.Text = "ATC故障を有効化 (ATCFail)";
                lblFailParameter.Text = "故障確率 (FailParameter):";
                tabBasic.Text = "基本設定";
                tabOther.Text = "その他の設定";
                btnCollapseAll.Text = "すべて折り畳む";
                btnExpandAll.Text = "すべて展開";
            }
            UpdateToolTips(); // ツールチップの内容も更新
            UpdateAtcTypeItems(); // ATCタイプの選択肢の名前も言語に合わせる
            UpdateOtherTabLanguage(); // 「その他の設定」の説明文・ツールチップも言語に合わせる
            UpdateLineNames(); // 号線の路線名も言語に合わせる
            UpdateTitle(); // タイトルバー(ファイル名・* ・アプリ名)
            ArrangeFailParameter(); // 言語でラベルの長さが変わるので、数字の欄の位置を合わせる
        }

        /// <summary>
        /// ウィンドウの大きさを変えられるようにします(端をドラッグ・最大化)。
        /// ・今の大きさより小さくはできない(「基本設定」の部品が切れないように)
        /// ・ファイルの場所の欄は横に伸び、「INIファイルを選択」ボタンは右端についていく
        /// ・「その他の設定」の表は、広げたぶんだけ多く見える(タブいっぱいに広がる)
        /// </summary>
        private void SetupResizable()
        {
            this.MinimumSize = this.Size;
            txtFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnSelectFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        }

        // ===== ウィンドウの位置と大きさを覚える =====

        /// <summary>
        /// 前回閉じたときの位置と大きさに戻します。
        /// ・覚えていないとき(初めての起動)は、今までどおりの位置と大きさ
        /// ・その位置がどの画面にも見えないとき(モニターを外したなど)も、今までどおり
        /// ・最大化して閉じたときは、最大化して開く
        /// </summary>
        private void RestoreWindowBounds()
        {
            IniFile settings = AppSettings.Load();
            int? x = AppSettings.GetInt(settings, "Left");
            int? y = AppSettings.GetInt(settings, "Top");
            int? w = AppSettings.GetInt(settings, "Width");
            int? h = AppSettings.GetInt(settings, "Height");
            if (x == null || y == null || w == null || h == null) return;

            var rect = new Rectangle(x.Value, y.Value,
                Math.Max(w.Value, MinimumSize.Width), Math.Max(h.Value, MinimumSize.Height));

            // タイトルバーのあたりが、どれかの画面に見えているか
            var titleArea = new Rectangle(rect.Left, rect.Top, rect.Width, LogicalToDeviceUnits(30));
            if (!Screen.AllScreens.Any(sc => sc.WorkingArea.IntersectsWith(titleArea)))
            {
                ActionLogger.Log("前回のウィンドウ位置が画面の外なので、使いません");
                return;
            }

            StartPosition = FormStartPosition.Manual;
            Bounds = rect;
            if (AppSettings.GetInt(settings, "Maximized") == 1) WindowState = FormWindowState.Maximized;
            ActionLogger.Log($"前回のウィンドウ位置・大きさに戻しました: {rect}");
        }

        /// <summary>
        /// 閉じるときに、今の位置と大きさを覚えます(最大化中は、元に戻したときの位置と大きさ)。
        /// </summary>
        private void SaveWindowBounds()
        {
            Rectangle r = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            if (r.Width <= 0 || r.Height <= 0) return; // 最小化のまま閉じたときなど、正しく取れないときは覚えない

            IniFile settings = AppSettings.Load();
            AppSettings.SetInt(settings, "Left", r.Left);
            AppSettings.SetInt(settings, "Top", r.Top);
            AppSettings.SetInt(settings, "Width", r.Width);
            AppSettings.SetInt(settings, "Height", r.Height);
            AppSettings.SetInt(settings, "Maximized", WindowState == FormWindowState.Maximized ? 1 : 0);
            AppSettings.Save(settings);
            ActionLogger.Log($"ウィンドウ位置・大きさを保存: {r} 最大化={WindowState == FormWindowState.Maximized}");
        }

        // ===== 号線の路線名 =====

        // 号線の番号(1～9)ごとの路線名 [日本語, 英語]。※ は注意書きがあるもの(ツールチップに出す)
        private static readonly string[][] lineNames =
        {
            new[] { "御堂筋線", "Midosuji Line" },
            new[] { "谷町線", "Tanimachi Line" },
            new[] { "四つ橋線", "Yotsubashi Line" },
            new[] { "中央線 ※", "Chuo Line *" },
            new[] { "千日前線", "Sennichimae Line" },
            new[] { "堺筋線 ※", "Sakaisuji Line *" },
            new[] { "長堀鶴見緑地線", "Nagahori Tsurumi-ryokuchi Line" },
            new[] { "今里筋線", "Imazatosuji Line" },
            new[] { "北大阪急行", "Kita-Osaka Kyuko Line" },
        };

        // 号線の番号ごとの注意書き [日本語, 英語](ないものは空)
        private static string[] GetLineNote(int line)
        {
            return line switch
            {
                4 => new[] { "近鉄モードは中央線のみです。", "Kintetsu mode is available only on the Chuo Line." },
                6 => new[] { "阪急ATSと互換性がありません。", "Not compatible with the Hankyu ATS." },
                _ => new[] { "", "" },
            };
        }

        // 号線の3つの欄の横に出す路線名のラベル
        private readonly Label lblDefaultLineName = new Label();
        private readonly Label lblMinLineName = new Label();
        private readonly Label lblMaxLineName = new Label();

        /// <summary>
        /// 号線の3つの欄の右に、路線名のラベルを置きます。欄の数字が変わると路線名も変わります。
        /// </summary>
        private void SetupLineNameLabels()
        {
            foreach (var (num, lbl) in LinePairs())
            {
                lbl.AutoSize = true;
                lbl.ForeColor = Color.DimGray;
                grpLineSpec.Controls.Add(lbl);
                num.ValueChanged += (s, e) => UpdateLineNames();
                num.ValueChanged += (s, e) => MarkAsModified(); // 号線を変えたら「未保存の変更あり」(前は印が付かなかった)
            }
            UpdateLineNames();
        }

        private IEnumerable<(NumericUpDown, Label)> LinePairs()
        {
            yield return (numDefaultLine, lblDefaultLineName);
            yield return (numMinLine, lblMinLineName);
            yield return (numMaxLine, lblMaxLineName);
        }

        /// <summary>
        /// 欄の数字に合わせて路線名を表示し、入りきらないときは枠を広げます。
        /// </summary>
        private void UpdateLineNames()
        {
            int lang = isEnglish ? 1 : 0;
            int needWidth = 0;
            foreach (var (num, lbl) in LinePairs())
            {
                int line = (int)num.Value;
                lbl.Text = (line >= 1 && line <= lineNames.Length) ? lineNames[line - 1][lang] : "";
                lbl.Left = num.Right + LogicalToDeviceUnits(6);
                lbl.Top = num.Top + (num.Height - lbl.Height) / 2;

                string note = GetLineNote(line)[lang];
                toolTip1.SetToolTip(lbl, note == "" ? lbl.Text : $"{lbl.Text.TrimEnd(' ', '※', '*')}\n{note}");

                needWidth = Math.Max(needWidth, lbl.Right + LogicalToDeviceUnits(10));
            }

            // 路線名が入りきらないときは、左の2つの枠をそろえて広げ、ATC故障の枠を右へずらす
            if (grpLineSpec.Width < needWidth)
            {
                grpLineSpec.Width = needWidth;
                grpVehicle.Width = needWidth;
                grpAtc.Left = grpLineSpec.Right + LogicalToDeviceUnits(6);
            }
        }

        /// <summary>
        /// ATC故障の「確率」の数字の欄を、ラベルのすぐ右に置きます(ラベルの長さは言語で変わるため)。
        /// 枠からはみ出すときは、枠を右へ広げます。
        /// </summary>
        private void ArrangeFailParameter()
        {
            int gap = LogicalToDeviceUnits(6);
            numFailParameter.Left = lblFailParameter.Right + gap;
            // 数字の欄の縦の中心を、ラベルの縦の中心にそろえる
            numFailParameter.Top = lblFailParameter.Top + (lblFailParameter.Height - numFailParameter.Height) / 2;

            int needWidth = numFailParameter.Right + LogicalToDeviceUnits(10);
            if (grpAtc.Width < needWidth) grpAtc.Width = needWidth;
        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {
        }
        private void UpdateToolTips()
        {
            if (isEnglish)
            {
                toolTip1.ToolTipTitle = "Setting Help";
                toolTip1.SetToolTip(label3, "This is a beta version. Please be careful when using it.");
                toolTip1.SetToolTip(txtFilePath, "File path");
                toolTip1.SetToolTip(numMaxBrake, "Maximum brake setting \n Enter a value of 1 or more");
                toolTip1.SetToolTip(cmbAtcType, "ATC type \n 0 WS-ATC \n 1 CS-ATC \n 2 Both enabled");
                toolTip1.SetToolTip(numDefaultLine, "Default line setting \n Specify a value between the minimum and maximum line settings");
                toolTip1.SetToolTip(numMinLine, "Minimum line setting \n Specify a value of 1 or more");
                toolTip1.SetToolTip(numMaxLine, "Maximum line setting \n Specify a value equal to or greater than the minimum line setting");
                toolTip1.SetToolTip(chkAtcFail, "Enable or disable random ATC system failure.");
                toolTip1.SetToolTip(numFailParameter, "Random failure probability (0.0000001 to 1.0). Values below 0.0000001% will be rounded to 0.");
                if (btnSelectFile != null)
                {
                    toolTip1.SetToolTip(btnSelectFile, "Select INI file");
                }
            }
            else
            {
                toolTip1.ToolTipTitle = "設定ヒント";
                toolTip1.SetToolTip(label3, "現在、ベータ版バージョンです。不具合が出る可能性があります。注意して使用してください。");
                toolTip1.SetToolTip(txtFilePath, "ファイルパス");
                toolTip1.SetToolTip(numMaxBrake, "最大ブレーキ設定 \n 1以上の数値を入力してください");
                toolTip1.SetToolTip(cmbAtcType, "ATCタイプ \n 0 WS-ATC \n 1 CS-ATC \n 2 両方有効");
                toolTip1.SetToolTip(numDefaultLine, "初期号線の設定 \n 最小号線以上最大号線以内で指定してください");
                toolTip1.SetToolTip(numMinLine, "最小号線の設定 \n 1以上、最大号線以下で指定してください");
                toolTip1.SetToolTip(numMaxLine, "最大号線の設定 \n 最小号線以上で指定してください。 \n最小号線未満は不具合が出る可能性があります。");
                toolTip1.SetToolTip(chkAtcFail, "ATCシステムのランダム故障を有効化するかどうかを設定します。");
                toolTip1.SetToolTip(numFailParameter, "故障発生の確率を指定します (0.0000001〜1.0)。0.0000001%未満は0にリセットされます。");
                if (btnSelectFile != null)
                {
                    toolTip1.SetToolTip(btnSelectFile, "INIファイルを選択");
                }
            }

        }

        private void CrashButton1_Click(object sender, EventArgs e) // クラッシュテスト用のボタン
        {

            //throw new Exception("This is a test exception for crash handling."); // クラッシュテスト用の例外を発生させる リリース版では削除してください
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void chkAtcFail_CheckedChanged(object sender, EventArgs e)
        {
            // ATCFail が true の時だけ FailParameter を操作可能にする
            bool isEnabled = chkAtcFail.Checked;
            lblFailParameter.Enabled = isEnabled;
            numFailParameter.Enabled = isEnabled;

            ActionLogger.Log($"ATCFail 変更: {chkAtcFail.Checked}");
            isModified = true;
        }

        private void numFailParameter_ValueChanged(object sender, EventArgs e)
        {
            // 故障確率を変えたら「未保存の変更あり」(前は印が付かなかった)
            MarkAsModified();
        }
    }
}