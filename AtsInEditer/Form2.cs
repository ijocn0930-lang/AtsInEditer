using System.Diagnostics;

namespace AtsInEditer
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
        }
        private bool isEnglish = false; // デフォルトは日本語

        public Form2(bool isEnglish)
        {
            InitializeComponent();
            this.isEnglish = isEnglish;
            UpdateAboutLanguage();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                // URLを開く (お好みのリンク先に変更してください)
                string url = "https://x.com/vbHGjiftDe";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                String message = isEnglish
                    ? "Could not open the link: " + ex.Message
                    : "リンクを開けませんでした: " + ex.Message;
                DialogResult result = MessageBox.Show(message, isEnglish ? "Error" : "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    
    private void UpdateAboutLanguage()
        { 
            if (isEnglish)
            {
                // 英語に切り替え
                label1.Text = "ATS Plugin INI Editor Version v0.0.2";
                label2.Text = "Created by Tetsudo";
                button1.Text = "Close";
            }
            else
            {
                // 日本語に切り替え
                label1.Text = "ATSプラグイン設定　バージョン v0.0.2";
                label2.Text = "作成者　テツドウ";
                button1.Text = "閉じる";
            }
        }
    }
}