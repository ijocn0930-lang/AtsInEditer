namespace AtsInEditer
{
    /// <summary>
    /// アプリの設定(ウィンドウの位置・大きさなど)を覚えておくためのファイルを読み書きします。
    /// 場所: %LocalAppData%\AtsInEditer\settings.ini
    /// (exe と同じフォルダだと、Program Files などに置いたときに書き込めないため)
    /// </summary>
    public static class AppSettings
    {
        private const string Section = "Window";

        public static string FilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtsInEditer", "settings.ini");

        /// <summary>
        /// 設定を読み込みます。ファイルがない・読めないときは空の設定を返します。
        /// </summary>
        public static IniFile Load()
        {
            try
            {
                if (File.Exists(FilePath)) return IniFile.Load(FilePath);
            }
            catch (Exception)
            {
                // 壊れていても起動は止めない
            }
            return IniFile.CreateNew(FilePath);
        }

        /// <summary>
        /// 設定を保存します。保存できなくてもアプリは止めません。
        /// </summary>
        public static void Save(IniFile ini)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                ini.Save();
            }
            catch (Exception)
            {
                // 書き込めない場所などでも、終了処理は止めない
            }
        }

        public static int? GetInt(IniFile ini, string key)
        {
            return int.TryParse(ini.Get(Section, key), out int v) ? v : null;
        }

        public static void SetInt(IniFile ini, string key, int value)
        {
            ini.Set(Section, key, value.ToString());
        }

        // ---------- 言語 ----------

        /// <summary>
        /// 前回選んだ言語が英語なら true、日本語なら false、覚えていなければ null。
        /// </summary>
        public static bool? LoadIsEnglish()
        {
            string? lang = Load().Get("General", "Language");
            if (lang == "en") return true;
            if (lang == "ja") return false;
            return null;
        }

        /// <summary>
        /// 選んだ言語を覚えます。
        /// </summary>
        public static void SaveIsEnglish(bool isEnglish)
        {
            IniFile ini = Load();
            ini.Set("General", "Language", isEnglish ? "en" : "ja");
            Save(ini);
        }
    }
}
