using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Windows.Storage.Streams;
namespace AtsInEditer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            // --- 未処理の例外（UIスレッドおよび別スレッド）のハンドラーを登録 ---
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            ApplicationConfiguration.Initialize();
            // ★ 1. 64bit プロセスチェック
            if (!Environment.Is64BitProcess)
            {
                MessageBox.Show(
                    "このアプリケーションは 64-bit (x64) 環境専用です。\n32-bit OS または 32-bit エミュレーション環境では動作しません。\n Erroroutput.log を確認してください。",
                    "動作環境エラー (Architecture Error)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                try
                {
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Erroroutput.log");
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("==================================================");
                    sb.AppendLine($"[Error LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine("このアプリケーションは 64-bit (x64) 環境専用です。32-bit OS または 32-bit エミュレーション環境では動作しません。");
                    sb.AppendLine("this application is 64-bit (x64) only. It does not run on 32-bit OS or 32-bit emulation environments.");
                    sb.AppendLine("==================================================");
                    sb.AppendLine($"Process Architecture: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
                    sb.AppendLine($"OS Architecture     : {GetOSArchitecture()}");
                    sb.AppendLine("==================================================");
                    sb.AppendLine("Error!!!");
                    File.AppendAllText(logPath, sb.ToString(), Encoding.UTF8);
                }
                catch
                {
                    // ログ書き込み失敗時は握りつぶす
                }
                Environment.Exit(1); // アプリ起動を中止
                return; // アプリ起動を中止
            }

            // ★ 2. Windows 11 24H2 (10.0.26100) 以上チェック
            if (!IsWindows11Version24H2OrGreater())
            {
                MessageBox.Show(
                    "このアプリケーションの実行には Windows 11 24H2 (Build 26100) 以降の OS 環境が必要です。\nお使いの OS バージョンが要件を満たしていないため起動できません。\n Erroroutput.log を確認してください。",
                    "動作環境エラー (OS Version Error)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                try
                {
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Erroroutput.log");
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("==================================================");
                    sb.AppendLine($"[Error LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine("このアプリケーションの実行には Windows 11 24H2 (Build 26100) 以降の OS 環境が必要です。");
                    sb.AppendLine("お使いの OS バージョンが要件を満たしていないため起動できません。");
                    sb.AppendLine("this application requires Windows 11 24H2 (Build 26100) or later to run.");
                    sb.AppendLine("==================================================");
                    sb.AppendLine($"OS Version: {Environment.OSVersion} ({GetOSArchitecture()})");
                    sb.AppendLine($"OS Build Number: {Environment.OSVersion.Version.Build}");
                    sb.AppendLine("==================================================");
                    sb.AppendLine("Error!!!");
                    File.AppendAllText(logPath, sb.ToString(), Encoding.UTF8);
                }
                catch
                {
                    // ログ書き込み失敗時は握りつぶす
                }
                Environment.Exit(1); // アプリ起動を中止
                return; // アプリ起動を中止
            }
            Application.Run(new Form1());
        }
        // --- OS バージョンチェック用ヘルパー関数 ---
        private static bool IsWindows11Version24H2OrGreater()
        {
            OperatingSystem os = Environment.OSVersion;

            // Windows NT 10.0 系チェック
            if (os.Platform == PlatformID.Win32NT)
            {
                Version version = os.Version;

                // メジャーバージョン 10 以上
                if (version.Major > 10) return true;

                if (version.Major == 10)
                {
                    // マイナーバージョンが 0 より大きい場合 (念のため)
                    if (version.Minor > 0) return true;

                    
                    // ビルド番号が 26100 (Windows 11 24H2) 以上か判定
                    if (version.Build >= 26100) return true;
                }
            }

            return false;
        }
        // --- UIスレッドで未ハンドリング例外が発生した際のエラーハンドラー ---
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleFatalCrash(e.Exception);
        }
        // --- UI以外のバックグラウンドスレッド等で例外が発生した際のエラーハンドラー ---
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleFatalCrash(ex);
            }
            else
            {
                // Exception 以外のオブジェクトがスローされた場合
                Environment.Exit(1);
            }
        }
        public static class ActionLogger
        {
            // 直近 32 件の操作履歴を保持
            private static readonly Queue<string> actionHistory = new Queue<string>();
            private static readonly int maxHistoryCount = 32;

            public static void Log(string action)
            {
                lock (actionHistory)
                {
                    if (actionHistory.Count >= maxHistoryCount)
                    {
                        actionHistory.Dequeue();
                    }
                    actionHistory.Enqueue($"[{System.DateTime.Now:HH:mm:ss}] {action}");
                }
            }
            public static string GetHistoryString()
            {
                lock (actionHistory)
                {
                    if (actionHistory.Count == 0) return "No actions logged.";
                    return string.Join("\n", actionHistory);
                }
            }
        }
        private static void HandleFatalCrash(Exception ex)
        {
            // 1. ログをファイルへ書き出す
            WriteErrorLog(ex);

            // 2. クラッシュダイアログを表示
            MessageBox.Show(
                $"予期せぬエラーが発生したため、アプリを強制終了します。\n\nエラー: {ex.Message}\n\n※操作履歴とエラー情報を output.log に出力しました。",
                "致命的なエラー (Fatal Error)",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

            // 3. FormClosing イベントなどの未保存ダイアログを迂回し、プロセスを即座に安全に完全終了させる
            Environment.Exit(1);
        }
        // --- CPUの詳細名（型番）を取得する関数 ---
        private static string GetProcessorName()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        // 例: "Intel(R) Core(TM) i7-12700K" や "AMD Ryzen 7 5800X 8-Core Processor"
                        return obj["Name"]?.ToString()?.Trim() ?? "Unknown Processor";
                    }
                }
            }
            catch
            {
            }
                return "Unknown Processor"; // 取得失敗時は Unknown Processor を返す

        }

        // --- フレームワークの詳細情報を取得する関数 ---
        public static String GetFrameworkDescription()
        {
            try
            {
                return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            }
            catch
            {
                return ".NET Framework (Unknown Version)"; // 取得失敗時は Unknown Version を返す
            }
        }

        // --- OSのアーキテクチャを取得する関数 ---
        public static string GetOSArchitecture()
        {
            try
            {
                return Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
            }
            catch
            {
                return "Unknown Architecture"; // 取得失敗時は Unknown Architecture を返す
            }
        }

        // --- メモリ情報を取得する関数 ---
        public static String MemoryInfo()
        {
                StringBuilder sb = new StringBuilder();
                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject os in searcher.Get())
                    {
                        ulong totalVisibleMemory = (ulong)os["TotalVisibleMemorySize"];
                        ulong freePhysicalMemory = (ulong)os["FreePhysicalMemory"];
                        ulong totalVirtualMemory = (ulong)os["TotalVirtualMemorySize"];
                    }
                }
            return sb.ToString();
        }
        
        // --- output.log へのログ書き込み処理 ---
        private static void WriteErrorLog(Exception ex)
        {
            try
            {
                // 実行ファイルと同じディレクトリに output.log を作成
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output.log");

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("==================================================");
                sb.AppendLine($"[CRASH LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Application: {Assembly.GetExecutingAssembly().GetName().Name}");
                sb.AppendLine("クラッシュログが作成されました。開発者に報告してください。");
                sb.AppendLine("Please report the crash log to the developer.");
                sb.AppendLine("==================================================");

                // --- 1. システム・コンピューター情報の取得 ---
                sb.AppendLine("[Environment Info]");
                sb.AppendLine($"Machine Name    : {Environment.MachineName}"); // コンピューター名
                sb.AppendLine($"OS Version      : {Environment.OSVersion} ({GetOSArchitecture()})"); // OSバージョン
                sb.AppendLine($"Process Architecture : {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}"); // プロセスのアーキテクチャ
                sb.AppendLine($"OS Architecture   : {GetOSArchitecture()}"); // OSのアーキテクチャ
                sb.AppendLine($"CLR / .NET Ver  : {Environment.Version}"); // CLR / .NET バージョン
                sb.AppendLine($"Framework Desc  : {GetFrameworkDescription()}"); // フレームワークの詳細情報
                sb.AppendLine($"Processor Name  : {GetProcessorName()}"); // CPU 型番
                sb.AppendLine($"Processor Count : {Environment.ProcessorCount} Cores"); // プロセッサ数
                sb.AppendLine($"Current Directory : {Environment.CurrentDirectory}"); // 実行中のカレントディレクトリ

                {
                    // Windows の場合、より詳細な情報を取得
                    sb.AppendLine($"Windows Version : {Environment.OSVersion.VersionString}");
                }

                // ★ 直前の操作履歴（ブレッドクラム）の追加
                sb.AppendLine("[User Action History (Recent Operations)]");
                sb.AppendLine(ActionLogger.GetHistoryString());
                sb.AppendLine();

                // 利用可能メモリ(RAM)の目安（物理メモリや作業セット）
                long workingSetMB = Environment.WorkingSet / (1024 * 1024);
                sb.AppendLine($"App Memory Usage: {workingSetMB} MB"); // 現在のプロセスの作業セット（メモリ使用量）
                sb.AppendLine($"Free Physical Memory: { new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory / (1024 * 1024)} MB"); // 利用可能な物理メモリ
                sb.AppendLine($"Total Physical Memory: {new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024 * 1024)} MB"); // 総物理メモリ
                sb.AppendLine($"Free Virtual Memory: {new Microsoft.VisualBasic.Devices.ComputerInfo().AvailableVirtualMemory / (1024 * 1024)} MB"); // 利用可能な仮想メモリ


                // アプリケーション自体のバージョン情報
                string appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
                sb.AppendLine($"App Version     : {appVersion}");
                sb.AppendLine();

                // --- 2. 例外の詳細情報 ---
                sb.AppendLine("[Exception Details]");
                sb.AppendLine($"Exception Type  : {ex.GetType().FullName}");
                sb.AppendLine($"Message         : {ex.Message}");
                sb.AppendLine("Source          :");
                sb.AppendLine(ex.Source);
                sb.AppendLine("Stack Trace     :");
                sb.AppendLine(ex.StackTrace);

                if (ex.InnerException != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("[Inner Exception]");
                    sb.AppendLine(ex.InnerException.ToString());
                }

                sb.AppendLine("==================================================");
                sb.AppendLine("Crash!!!"); 
                sb.AppendLine();
                // Shift-JIS または UTF-8 でログを追記 (AppendText)
                File.AppendAllText(logPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // ログ自体の書き込み失敗時（権限なし等）はアプリを道連れにしないよう握りつぶす
            }
        }

    }
}