using System.Text;

namespace AtsInEditer
{
    /// <summary>
    /// ATS_ini_params.csv の1行分(1つのパラメーター)を表します。
    /// </summary>
    public class ParamDef
    {
        public string Group = "";       // 画面でまとめる単位(例: 車両, TASC)
        public string Section = "";     // INIのセクション名(例: TASC, Gauge{n})
        public string Key = "";         // INIのキー名
        public string Type = "";        // int / float / bool / enum / list / special
        public string Default = "";     // 既定値
        public decimal? Min;            // 最小値(空欄なら制限なし)
        public decimal? Max;            // 最大値(空欄なら制限なし)
        public string Unit = "";        // 単位
        public List<KeyValuePair<string, string>> Choices = new(); // enumの選択肢(値, 表示名)
        public string Repeat = "";      // 繰り返し範囲(例: 1-8)
        public string Description = ""; // 説明文(日本語)
        public string DescriptionEn = ""; // 説明文(英語)。CSV に description_en 列がなければ空

        /// <summary>
        /// CSVファイルを読み込んで、パラメーターの一覧を返します。
        /// </summary>
        public static List<ParamDef> LoadCsv(string path)
        {
            var list = new List<ParamDef>();
            // UTF-8 (BOM付きでもOK) で読み込む
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);

            if (lines.Length == 0) return list;

            // 1行目の見出しから、列の位置を調べる(列の順番が変わっても読めるように)
            // 例: group,section,key,type,default,min,max,unit,choices,repeat,description_en,description
            string[] header = lines[0].Split(',').Select(h => h.Trim().TrimStart('﻿')).ToArray();
            int columnCount = header.Length;
            int Col(string name) => Array.IndexOf(header, name);
            int iGroup = Col("group"), iSection = Col("section"), iKey = Col("key"), iType = Col("type");
            int iDefault = Col("default"), iMin = Col("min"), iMax = Col("max"), iUnit = Col("unit");
            int iChoices = Col("choices"), iRepeat = Col("repeat"), iDesc = Col("description"), iDescEn = Col("description_en");

            // 1行目は見出しなので飛ばす
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                // 一番右の列(description)にはカンマが入ることがあるので、見出しの列数までしか分けない
                string[] c = line.Split(',', columnCount);
                if (c.Length < columnCount) continue; // 列が足りない行は無視

                string Get(int index) => index >= 0 ? c[index].Trim() : "";

                var p = new ParamDef
                {
                    Group = Get(iGroup),
                    Section = Get(iSection),
                    Key = Get(iKey),
                    Type = Get(iType).ToLowerInvariant(),
                    Default = Get(iDefault),
                    Min = ParseDecimal(Get(iMin)),
                    Max = ParseDecimal(Get(iMax)),
                    Unit = Get(iUnit),
                    Repeat = Get(iRepeat),
                    Description = Get(iDesc),
                    DescriptionEn = Get(iDescEn),
                };

                // 選択肢は「値=表示名|値=表示名|...」の形
                foreach (string item in Get(iChoices).Split('|', StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] kv = item.Split('=', 2);
                    string val = kv[0].Trim();
                    string label = kv.Length > 1 ? kv[1].Trim() : val;
                    p.Choices.Add(new KeyValuePair<string, string>(val, label));
                }

                list.Add(p);
            }
            return list;
        }

        private static decimal? ParseDecimal(string s)
        {
            if (decimal.TryParse(s.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out decimal v))
            {
                return v;
            }
            return null;
        }
    }
}
