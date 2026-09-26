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

            // --- セクション存在チェック ---
            // もし [VehicleSpec] セクションが存在しなければ、デフォルト値として渡した "__NOT_FOUND__" がそのまま返ってきます
            string checkSection = ReadIni("VehicleSpec", "MaxBrake", "__NOT_FOUND__");
            string checkSection2 = ReadIni("VehicleSpec", "AtcType", "__NOT_FOUND__");

            // もし [LineSpec] セクションが存在しなければ、デフォルト値として渡した "__NOT_FOUND__" がそのまま返ってきます
            string checkSection3 = ReadIni("LineSpec", "DefaultLine", "__NOT_FOUND__");
            // MinLineとMaxLineが記述されていない場合はデフォルトの値を使用するため、ここでは__NOT_FOUND__をチェックしない。

            // --- [LineSpec] セクションの読み込み (デフォルト値は Osaka Metro 仕様の 1, 1, 8) ---
            int defaultLine = (int)GetPrivateProfileIntA("LineSpec", "DefaultLine", 1, iniPath);
            int minLine = (int)GetPrivateProfileIntA("LineSpec", "MinLine", 1, iniPath);
            int maxLine = (int)GetPrivateProfileIntA("LineSpec", "MaxLine", 8, iniPath);

            // 範囲外（1〜8以外）の値がINIに入っていた場合のガード
            numDefaultLine.Value = Math.Max(1, Math.Min(8, defaultLine));
            numMinLine.Value = Math.Max(1, Math.Min(8, minLine));
            numMaxLine.Value = Math.Max(1, Math.Min(8, maxLine));

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
                txtFilePath.Text = "";
                iniPath = "";
                return; // ここで読み込み処理を打ち切る
            }

            // --- 2. MaxBrake の読み込みと数値範囲チェック ---
            // 例: MaxBrake の許容範囲を 1 ～ 13（ NumericUpDown の設定値に合わせて調整してください）
            int minMaxBrake = (int)numMaxBrake.Minimum;
            int maxMaxBrake = (int)numMaxBrake.Maximum;

            string maxBrakeStr = ReadIni("VehicleSpec", "MaxBrake", "");

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
            string atcTypeStr = ReadIni("VehicleSpec", "AtcType", "");

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
            StringBuilder sbAtcFail = new StringBuilder(255);
            GetPrivateProfileStringA("ATC", "ATCFail", "false", sbAtcFail, (uint)sbAtcFail.Capacity, iniPath);
            bool atcFail = bool.TryParse(sbAtcFail.ToString().Trim(), out bool parsedBool) && parsedBool;

            // 2. FailParameter の読み込み (既定は "0")
            StringBuilder sbFailParam = new StringBuilder(255);
            GetPrivateProfileStringA("ATC", "FailParameter", "0", sbFailParam, (uint)sbFailParam.Capacity, iniPath);

            decimal failParam = 0m;
            if (decimal.TryParse(sbFailParam.ToString().Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedDecimal))
            {
                failParam = NormalizeFailParameter(parsedDecimal);
            }

            // UIへ反映
            chkAtcFail.Checked = atcFail;
            numFailParameter.Value = failParam;

            // グレーアウト状態の更新
            chkAtcFail_CheckedChanged(chkAtcFail, EventArgs.Empty);

            ActionLogger.Log($"[ATC] 読み込み完了: ATCFail={atcFail}, FailParameter={failParam}");
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
        private bool isModified = false;

        // 設定値が変更されたときに呼ぶメソッド
        private void MarkAsModified()
        {
            // ファイルが開かれている状態でのみ変更フラグを立てる
            if (!string.IsNullOrEmpty(iniPath))
            {
                isModified = true;
            }
        }


        private void cmbAtcType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActionLogger.Log($"AtcType の値を変更: {cmbAtcType.SelectedIndex}");
            MarkAsModified();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateLanguage(); // 言語設定を初期化
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
            // Ctrl + S : 保存
            if (e.Control && e.KeyCode == Keys.S)
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
            btnSelectFile.PerformClick(); // 既存のファイル選択ボタンの処理を呼び出す
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

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    iniPath = sfd.FileName;
                    txtFilePath.Text = iniPath;

                    if (numMaxBrake.Value == 0)
                    {
                        string warningMessage = isEnglish
                            ? "MaxBrake is set to 0. Saving may cause ATS brakes to not function correctly. Do you want to continue?"
                            : "MaxBrakeが0に設定されています。保存するとATSブレーキが正しく動作しない可能性があります。続けますか？";
                        string warningTitle = isEnglish ? "Warning" : "警告";
                        DialogResult result = MessageBox.Show(warningMessage, warningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                        if (result == DialogResult.No)
                        {
                            return;
                        }

                        // --- 1. バリデーションチェック ---
                        // MinLine が MaxLine を超えている場合のエラー
                        if (numMinLine.Value > numMaxLine.Value)
                        {
                            string msg = isEnglish
                                ? "MinLine cannot be greater than MaxLine."
                                : "最小号線 (MinLine) は最大号線 (MaxLine) 以下の値にしてください。";
                            MessageBox.Show(msg, isEnglish ? "Input Error" : "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // DefaultLine が範囲外の場合のエラー
                        if (numDefaultLine.Value < numMinLine.Value || numDefaultLine.Value > numMaxLine.Value)
                        {
                            string msg = isEnglish
                                ? "DefaultLine must be between MinLine and MaxLine."
                                : "初期号線 (DefaultLine) は 最小号線〜最大号線 の範囲内で設定してください。";
                            MessageBox.Show(msg, isEnglish ? "Input Error" : "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // 新規作成時の初期値をセット（必要に応じて数値を調整してください）
                        numMaxBrake.Value = 8; // デフォルトのMaxBrake値
                        cmbAtcType.SelectedIndex = 0; // デフォルトのAtcType (例: 0)

                        // INIファイルに初期値を書き込む（ファイルがなければ自動作成されます）
                        WriteIni("VehicleSpec", "MaxBrake", numMaxBrake.Value.ToString());
                        WriteIni("VehicleSpec", "AtcType", cmbAtcType.SelectedIndex.ToString());

                        // 保存するテキストの内容を構築
                        // （※他のセクションやコメントを残す場合は既存ファイルを読み込んで置換、または構造を維持して書き出します）
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("[VehicleSpec]");
                        sb.AppendLine($"MaxBrake={numMaxBrake.Value}");
                        if (cmbAtcType.SelectedIndex >= 0)
                        {
                            sb.AppendLine($"AtcType={cmbAtcType.SelectedIndex}");
                        }
                        sb.AppendLine();
                        // ★ 新規追加 [LineSpec]
                        sb.AppendLine("[LineSpec]");
                        sb.AppendLine($"DefaultLine={numDefaultLine.Value}");
                        sb.AppendLine($"MinLine={numMinLine.Value}");
                        sb.AppendLine($"MaxLine={numMaxLine.Value}");
                        sb.AppendLine();
                        // ★ 新規追加 [ATC]
                        // 数値を最終確認（1.0超は1.0、0.0000001未満は0）
                        decimal finalFailParam = NormalizeFailParameter(numFailParameter.Value);
                        numFailParameter.Value = finalFailParam;

                        sb.AppendLine("[ATC]");
                        sb.AppendLine($"ATCFail={(chkAtcFail.Checked ? "true" : "false")}");
                        // 浮動小数の表記揺れを防ぐため不変カルチャで出力
                        sb.AppendLine($"FailParameter={finalFailParam.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                        sb.AppendLine();

                        // Shift-JIS (CP932) エンコーディングの取得
                        Encoding sjisEnc = Encoding.GetEncoding("shift_jis");

                        // Shift-JIS エンコーディングを指定してファイルへ一括書き込み
                        File.WriteAllText(iniPath, sb.ToString(), sjisEnc);

                        // 保存成功時の処理
                        isModified = false;
                        string successMessage = isEnglish
                            ? "INI file has been created and saved in Shift-JIS format!"
                            : "Shift-JIS形式でINIファイルへ保存しました！";
                        MessageBox.Show(successMessage, "保存完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void 保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionLogger.Log("上書き保存を開始");
            if (string.IsNullOrEmpty(iniPath) || !File.Exists(iniPath))
            {
                String message = isEnglish
                    ? "Please select an INI file first."
                    : "先にINIファイルを選択してください。";
                String errorTitle = isEnglish ? "Error" : "エラー";
                MessageBox.Show(message, errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            UnlockFile();

            // 1. MaxBrake 0 チェック
            if (numMaxBrake.Value == 0)
            {
                string warningMessage = isEnglish
                    ? "MaxBrake is set to 0. Saving may cause ATS brakes to not function correctly. Do you want to continue?"
                    : "MaxBrakeが0に設定されています。保存するとATSブレーキが正しく動作しない可能性があります。続けますか？";
                string warningTitle = isEnglish ? "Warning" : "警告";
                DialogResult result = MessageBox.Show(warningMessage, warningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    return;
                }
            }
            {
                // --- 1. バリデーションチェック ---
                // MinLine が MaxLine を超えている場合のエラー
                if (numMinLine.Value > numMaxLine.Value)
                {
                    string msg = isEnglish
                        ? "MinLine cannot be greater than MaxLine."
                        : "最小号線 (MinLine) は最大号線 (MaxLine) 以下の値にしてください。";
                    MessageBox.Show(msg, isEnglish ? "Input Error" : "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // DefaultLine が範囲外の場合のエラー
                if (numDefaultLine.Value < numMinLine.Value || numDefaultLine.Value > numMaxLine.Value)
                {
                    string msg = isEnglish
                        ? "DefaultLine must be between MinLine and MaxLine."
                        : "初期号線 (DefaultLine) は 最小号線〜最大号線 の範囲内で設定してください。";
                    MessageBox.Show(msg, isEnglish ? "Input Error" : "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
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
                    DialogResult result = MessageBox.Show(errorMessage, errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // 3. 実際の書き込みとアクセス権限（書き込み権限）のチェック
            {
                // WriteIni (WritePrivateProfileString) の書き込み実行
                bool res1 = WriteIni("VehicleSpec", "MaxBrake", numMaxBrake.Value.ToString());
                bool res2 = true;
                if (cmbAtcType.SelectedIndex >= 0)
                {
                    res2 = WriteIni("VehicleSpec", "AtcType", cmbAtcType.SelectedIndex.ToString());
                }

                // APIの返り値が false の場合は書き込み失敗（権限不足やロック中など）
                if (!res1 || !res2)
                {
                    MessageBox.Show(
                        "ファイルへの書き込みに失敗しました。アクセスが拒否されました\nアクセス権限がないか、他のプログラムによってファイルがロックされている可能性があります。",
                        "保存失敗",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
            }
            try
            {
                // Shift-JIS (CP932) エンコーディングの取得
                Encoding sjisEnc = Encoding.GetEncoding("shift_jis");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[VehicleSpec]");
                sb.AppendLine($"MaxBrake={numMaxBrake.Value}");
                if (cmbAtcType.SelectedIndex >= 0)
                {
                    sb.AppendLine($"AtcType={cmbAtcType.SelectedIndex}");
                }
                sb.AppendLine();
                // ★ 新規追加 [LineSpec]
                sb.AppendLine("[LineSpec]");
                sb.AppendLine($"DefaultLine={numDefaultLine.Value}");
                sb.AppendLine($"MinLine={numMinLine.Value}");
                sb.AppendLine($"MaxLine={numMaxLine.Value}");
                sb.AppendLine();

                // ★ 新規追加 [ATC]
                // 数値を最終確認（1.0超は1.0、0.0000001未満は0）
                decimal finalFailParam = NormalizeFailParameter(numFailParameter.Value);
                numFailParameter.Value = finalFailParam;

                sb.AppendLine("[ATC]");
                sb.AppendLine($"ATCFail={(chkAtcFail.Checked ? "true" : "false")}");
                // 浮動小数の表記揺れを防ぐため不変カルチャで出力
                sb.AppendLine($"FailParameter={finalFailParam.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                sb.AppendLine();

                // ファイル書き込み
                File.WriteAllText(iniPath, sb.ToString(), sjisEnc);
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
            btnSelectFile.PerformClick(); // 既存のファイル選択ボタンの処理を呼び出す
        }

        private void 日本語ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionLogger.Log("言語を Japanese に切り替え");
            isEnglish = false;
            UpdateLanguage();
        }

        private void 英語ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionLogger.Log("言語を English に切り替え");
            isEnglish = true;
            UpdateLanguage();
        }
        private bool isEnglish = false; // デフォルトは日本語
        private void UpdateLanguage()
        {
            日本語ToolStripMenuItem.Checked = !isEnglish;
            英語ToolStripMenuItem.Checked = isEnglish;
            if (isEnglish)
            {
                // 英語に切り替え
                this.Text = "ATS Plugin INI Editor";
                btnSelectFile.Text = "Select INI File";
                label1.Text = "ATC Type:";
                label2.Text = "Max Brake:";
                label3.Text = "This is a beta version. Please be careful when using it.";
                label4.Text = "Set vehicle parameters in the [Vehicle] section.";
                ファイルToolStripMenuItem.Text = "File(&F)";
                保存ToolStripMenuItem.Text = "Save(&S)";
                開くToolStripMenuItem.Text = "Open(&O)";
                新規作成ToolStripMenuItem.Text = "New(&N)";
                終了ToolStripMenuItem1.Text = "Exit(&X)";
                バージョン情報ToolStripMenuItem.Text = "About(&A)";
                日本語ToolStripMenuItem.Text = "Japanese(&J)";
                英語ToolStripMenuItem.Text = "English(&E)";
                ヘルプToolStripMenuItem.Text = "Help";
                //CrashButton1.Text = "Test Crash"; //ユーザーが意図せずにクラッシュするの防ぐため、リリース版では削除してください
                grpLineSpec.Text = "line settings";
                label5.Text = "Default Line";
                label6.Text = "Min Line";
                label7.Text = "Max Line";
                grpAtc.Text = "ATC Settings [ATC]";
                chkAtcFail.Text = "Enable ATC Failure (ATCFail)";
                lblFailParameter.Text = "Failure Probability (FailParameter):";
            }
            else
            {
                // 日本語に切り替え
                this.Text = "ATSプラグイン用INIエディタ";
                btnSelectFile.Text = "INIファイルを選択";
                label1.Text = "ATCタイプ:";
                label2.Text = "最大ブレーキ:";
                label3.Text = "現在、ベータ版バージョンです。不具合が出る可能性があります。注意して使用してください。";
                label4.Text = "[Vehicle]セクション　車両パラメーターを設定します。";
                ファイルToolStripMenuItem.Text = "ファイル(&F)";
                保存ToolStripMenuItem.Text = "保存(&S)";
                開くToolStripMenuItem.Text = "開く(&O)";
                新規作成ToolStripMenuItem.Text = "新規作成(&N)";
                終了ToolStripMenuItem1.Text = "終了(&X)";
                バージョン情報ToolStripMenuItem.Text = "バージョン情報(&A)";
                日本語ToolStripMenuItem.Text = "日本語(&J)";
                英語ToolStripMenuItem.Text = "英語(&E)";
                ヘルプToolStripMenuItem.Text = "ヘルプ";
                //CrashButton1.Text = "クラッシュテスト"; //ユーザーが意図せずにクラッシュするの防ぐため、リリース版では削除してください
                grpLineSpec.Text = "[LineSpec]号線設定";
                label5.Text = "初期号線";
                label6.Text = "最小号線";
                label7.Text = "最大号線";
                grpAtc.Text = "ATC設定 [ATC]";
                chkAtcFail.Text = "ATC故障を有効化 (ATCFail)";
                lblFailParameter.Text = "故障確率 (FailParameter):";
            }
            UpdateToolTips(); // ツールチップの内容も更新
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

        }
    }
}