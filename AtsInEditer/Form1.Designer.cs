namespace AtsInEditer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            btnSelectFile = new Button();
            txtFilePath = new TextBox();
            numMaxBrake = new NumericUpDown();
            cmbAtcType = new ComboBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            menuStrip1 = new MenuStrip();
            ファイルToolStripMenuItem = new ToolStripMenuItem();
            新規作成ToolStripMenuItem = new ToolStripMenuItem();
            開くToolStripMenuItem = new ToolStripMenuItem();
            保存ToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            終了ToolStripMenuItem1 = new ToolStripMenuItem();
            言語LangusageToolStripMenuItem = new ToolStripMenuItem();
            日本語ToolStripMenuItem = new ToolStripMenuItem();
            英語ToolStripMenuItem = new ToolStripMenuItem();
            ヘルプToolStripMenuItem = new ToolStripMenuItem();
            バージョン情報ToolStripMenuItem = new ToolStripMenuItem();
            toolTip1 = new ToolTip(components);
            CrashButton1 = new Button();
            grpLineSpec = new GroupBox();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            numMaxLine = new NumericUpDown();
            numMinLine = new NumericUpDown();
            numDefaultLine = new NumericUpDown();
            grpAtc = new GroupBox();
            lblFailParameter = new Label();
            numFailParameter = new NumericUpDown();
            chkAtcFail = new CheckBox();
            grpVehicle = new GroupBox();
            ((System.ComponentModel.ISupportInitialize)numMaxBrake).BeginInit();
            menuStrip1.SuspendLayout();
            grpLineSpec.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numMaxLine).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinLine).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDefaultLine).BeginInit();
            grpAtc.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numFailParameter).BeginInit();
            grpVehicle.SuspendLayout();
            SuspendLayout();
            // 
            // btnSelectFile
            // 
            btnSelectFile.ImeMode = ImeMode.NoControl;
            btnSelectFile.Location = new Point(660, 33);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new Size(102, 23);
            btnSelectFile.TabIndex = 0;
            btnSelectFile.Text = "Select File";
            btnSelectFile.UseVisualStyleBackColor = true;
            btnSelectFile.Click += button1_Click;
            // 
            // txtFilePath
            // 
            txtFilePath.Location = new Point(12, 33);
            txtFilePath.Name = "txtFilePath";
            txtFilePath.ReadOnly = true;
            txtFilePath.Size = new Size(642, 23);
            txtFilePath.TabIndex = 3;
            txtFilePath.TextChanged += txtFilePath_TextChanged;
            // 
            // numMaxBrake
            // 
            numMaxBrake.Location = new Point(106, 22);
            numMaxBrake.Name = "numMaxBrake";
            numMaxBrake.Size = new Size(45, 23);
            numMaxBrake.TabIndex = 4;
            numMaxBrake.ValueChanged += numMaxBrake_ValueChanged;
            // 
            // cmbAtcType
            // 
            cmbAtcType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAtcType.FormattingEnabled = true;
            cmbAtcType.Items.AddRange(new object[] { "0", "1", "2" });
            cmbAtcType.Location = new Point(106, 51);
            cmbAtcType.Name = "cmbAtcType";
            cmbAtcType.Size = new Size(120, 23);
            cmbAtcType.TabIndex = 5;
            cmbAtcType.SelectedIndexChanged += cmbAtcType_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.ImeMode = ImeMode.NoControl;
            label1.Location = new Point(22, 54);
            label1.Name = "label1";
            label1.Size = new Size(62, 15);
            label1.TabIndex = 6;
            label1.Text = "ATcType1";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.ImeMode = ImeMode.NoControl;
            label2.Location = new Point(22, 24);
            label2.Name = "label2";
            label2.Size = new Size(65, 15);
            label2.TabIndex = 7;
            label2.Text = "MaxBrake";
            label2.Click += label2_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ImeMode = ImeMode.NoControl;
            label3.Location = new Point(12, 329);
            label3.Name = "label3";
            label3.Size = new Size(435, 15);
            label3.TabIndex = 10;
            label3.Text = "現在、ベータ版バージョンです。不具合が出る可能性があります。注意して使用してください。";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.ImeMode = ImeMode.NoControl;
            label4.Location = new Point(12, 59);
            label4.Name = "label4";
            label4.Size = new Size(255, 15);
            label4.TabIndex = 11;
            label4.Text = "[Vehicle]セクション　車両パラメーターを設定します。";
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { ファイルToolStripMenuItem, 言語LangusageToolStripMenuItem, ヘルプToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(772, 24);
            menuStrip1.TabIndex = 12;
            menuStrip1.Text = "menuStrip1";
            // 
            // ファイルToolStripMenuItem
            // 
            ファイルToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 新規作成ToolStripMenuItem, 開くToolStripMenuItem, 保存ToolStripMenuItem, toolStripSeparator1, 終了ToolStripMenuItem1 });
            ファイルToolStripMenuItem.Name = "ファイルToolStripMenuItem";
            ファイルToolStripMenuItem.Size = new Size(45, 20);
            ファイルToolStripMenuItem.Text = "Files";
            // 
            // 新規作成ToolStripMenuItem
            // 
            新規作成ToolStripMenuItem.Name = "新規作成ToolStripMenuItem";
            新規作成ToolStripMenuItem.Size = new Size(149, 22);
            新規作成ToolStripMenuItem.Text = "新規作成(&N)";
            新規作成ToolStripMenuItem.Click += 新規作成ToolStripMenuItem_Click;
            // 
            // 開くToolStripMenuItem
            // 
            開くToolStripMenuItem.Name = "開くToolStripMenuItem";
            開くToolStripMenuItem.Size = new Size(149, 22);
            開くToolStripMenuItem.Text = "開く(&O)";
            開くToolStripMenuItem.Click += 開くToolStripMenuItem_Click_1;
            // 
            // 保存ToolStripMenuItem
            // 
            保存ToolStripMenuItem.Name = "保存ToolStripMenuItem";
            保存ToolStripMenuItem.Size = new Size(149, 22);
            保存ToolStripMenuItem.Text = "上書き保存(&S)";
            保存ToolStripMenuItem.Click += 保存ToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(146, 6);
            // 
            // 終了ToolStripMenuItem1
            // 
            終了ToolStripMenuItem1.Name = "終了ToolStripMenuItem1";
            終了ToolStripMenuItem1.Size = new Size(149, 22);
            終了ToolStripMenuItem1.Text = "Exit(&X)";
            終了ToolStripMenuItem1.Click += 終了ToolStripMenuItem1_Click;
            // 
            // 言語LangusageToolStripMenuItem
            // 
            言語LangusageToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 日本語ToolStripMenuItem, 英語ToolStripMenuItem });
            言語LangusageToolStripMenuItem.Name = "言語LangusageToolStripMenuItem";
            言語LangusageToolStripMenuItem.Size = new Size(115, 20);
            言語LangusageToolStripMenuItem.Text = "言語(Langusage)";
            // 
            // 日本語ToolStripMenuItem
            // 
            日本語ToolStripMenuItem.Checked = true;
            日本語ToolStripMenuItem.CheckState = CheckState.Checked;
            日本語ToolStripMenuItem.Name = "日本語ToolStripMenuItem";
            日本語ToolStripMenuItem.Size = new Size(188, 22);
            日本語ToolStripMenuItem.Text = "日本語(Japanese)(&J)";
            日本語ToolStripMenuItem.Click += 日本語ToolStripMenuItem_Click;
            // 
            // 英語ToolStripMenuItem
            // 
            英語ToolStripMenuItem.Name = "英語ToolStripMenuItem";
            英語ToolStripMenuItem.Size = new Size(188, 22);
            英語ToolStripMenuItem.Text = "英語(English)(&E)";
            英語ToolStripMenuItem.Click += 英語ToolStripMenuItem_Click;
            // 
            // ヘルプToolStripMenuItem
            // 
            ヘルプToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { バージョン情報ToolStripMenuItem });
            ヘルプToolStripMenuItem.Name = "ヘルプToolStripMenuItem";
            ヘルプToolStripMenuItem.Size = new Size(45, 20);
            ヘルプToolStripMenuItem.Text = "Help";
            // 
            // バージョン情報ToolStripMenuItem
            // 
            バージョン情報ToolStripMenuItem.Name = "バージョン情報ToolStripMenuItem";
            バージョン情報ToolStripMenuItem.Size = new Size(143, 22);
            バージョン情報ToolStripMenuItem.Text = "Version Info";
            バージョン情報ToolStripMenuItem.Click += バージョン情報ToolStripMenuItem_Click;
            // 
            // toolTip1
            // 
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 500;
            toolTip1.IsBalloon = true;
            toolTip1.ReshowDelay = 200;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.Popup += toolTip1_Popup;
            // 
            // CrashButton1
            // 
            CrashButton1.Enabled = false;
            CrashButton1.Location = new Point(687, 297);
            CrashButton1.Name = "CrashButton1";
            CrashButton1.Size = new Size(75, 44);
            CrashButton1.TabIndex = 13;
            CrashButton1.Text = "Crash!";
            CrashButton1.UseVisualStyleBackColor = true;
            CrashButton1.Visible = false;
            CrashButton1.Click += CrashButton1_Click;
            // 
            // grpLineSpec
            // 
            grpLineSpec.Controls.Add(label7);
            grpLineSpec.Controls.Add(label6);
            grpLineSpec.Controls.Add(label5);
            grpLineSpec.Controls.Add(numMaxLine);
            grpLineSpec.Controls.Add(numMinLine);
            grpLineSpec.Controls.Add(numDefaultLine);
            grpLineSpec.Location = new Point(12, 172);
            grpLineSpec.Name = "grpLineSpec";
            grpLineSpec.Size = new Size(240, 115);
            grpLineSpec.TabIndex = 14;
            grpLineSpec.TabStop = false;
            grpLineSpec.Text = "[LineSpec] 路線設定";
            grpLineSpec.Enter += groupBox1_Enter;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(25, 82);
            label7.Name = "label7";
            label7.Size = new Size(55, 15);
            label7.TabIndex = 20;
            label7.Text = "最大号線";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(25, 53);
            label6.Name = "label6";
            label6.Size = new Size(55, 15);
            label6.TabIndex = 19;
            label6.Text = "最小号線";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(22, 24);
            label5.Name = "label5";
            label5.Size = new Size(67, 15);
            label5.TabIndex = 18;
            label5.Text = "初期値号線";
            label5.Click += label5_Click;
            // 
            // numMaxLine
            // 
            numMaxLine.Location = new Point(114, 80);
            numMaxLine.Maximum = new decimal(new int[] { 9, 0, 0, 0 });
            numMaxLine.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMaxLine.Name = "numMaxLine";
            numMaxLine.Size = new Size(37, 23);
            numMaxLine.TabIndex = 17;
            numMaxLine.Value = new decimal(new int[] { 9, 0, 0, 0 });
            // 
            // numMinLine
            // 
            numMinLine.Location = new Point(114, 51);
            numMinLine.Maximum = new decimal(new int[] { 9, 0, 0, 0 });
            numMinLine.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMinLine.Name = "numMinLine";
            numMinLine.Size = new Size(37, 23);
            numMinLine.TabIndex = 16;
            numMinLine.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // numDefaultLine
            // 
            numDefaultLine.Location = new Point(114, 22);
            numDefaultLine.Maximum = new decimal(new int[] { 9, 0, 0, 0 });
            numDefaultLine.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numDefaultLine.Name = "numDefaultLine";
            numDefaultLine.Size = new Size(37, 23);
            numDefaultLine.TabIndex = 15;
            numDefaultLine.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numDefaultLine.ValueChanged += numericUpDown1_ValueChanged;
            // 
            // grpAtc
            // 
            grpAtc.Controls.Add(lblFailParameter);
            grpAtc.Controls.Add(numFailParameter);
            grpAtc.Controls.Add(chkAtcFail);
            grpAtc.Location = new Point(258, 80);
            grpAtc.Name = "grpAtc";
            grpAtc.Size = new Size(339, 100);
            grpAtc.TabIndex = 15;
            grpAtc.TabStop = false;
            grpAtc.Text = "ATC故障の設定";
            // 
            // lblFailParameter
            // 
            lblFailParameter.AutoSize = true;
            lblFailParameter.Enabled = false;
            lblFailParameter.Location = new Point(8, 47);
            lblFailParameter.Name = "lblFailParameter";
            lblFailParameter.Size = new Size(107, 15);
            lblFailParameter.TabIndex = 2;
            lblFailParameter.Text = "ATCが故障する確率";
            // 
            // numFailParameter
            // 
            numFailParameter.DecimalPlaces = 7;
            numFailParameter.Enabled = false;
            numFailParameter.Increment = new decimal(new int[] { 1, 0, 0, 327680 });
            numFailParameter.Location = new Point(212, 45);
            numFailParameter.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            numFailParameter.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            numFailParameter.Name = "numFailParameter";
            numFailParameter.Size = new Size(110, 23);
            numFailParameter.TabIndex = 1;
            numFailParameter.Value = new decimal(new int[] { 1, 0, 0, 458752 });
            numFailParameter.ValueChanged += numFailParameter_ValueChanged;
            // 
            // chkAtcFail
            // 
            chkAtcFail.AutoSize = true;
            chkAtcFail.Location = new Point(8, 19);
            chkAtcFail.Name = "chkAtcFail";
            chkAtcFail.Size = new Size(119, 19);
            chkAtcFail.TabIndex = 0;
            chkAtcFail.Text = "ATC故障の有効化";
            chkAtcFail.UseVisualStyleBackColor = true;
            chkAtcFail.CheckedChanged += chkAtcFail_CheckedChanged;
            //
            // grpVehicle
            //
            grpVehicle.Controls.Add(label2);
            grpVehicle.Controls.Add(numMaxBrake);
            grpVehicle.Controls.Add(label1);
            grpVehicle.Controls.Add(cmbAtcType);
            grpVehicle.Location = new Point(12, 80);
            grpVehicle.Name = "grpVehicle";
            grpVehicle.Size = new Size(240, 86);
            grpVehicle.TabIndex = 13;
            grpVehicle.TabStop = false;
            grpVehicle.Text = "[VehicleSpec] 車両設定";
            //
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(772, 353);
            Controls.Add(grpVehicle);
            Controls.Add(grpAtc);
            Controls.Add(grpLineSpec);
            Controls.Add(CrashButton1);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(txtFilePath);
            Controls.Add(btnSelectFile);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.Sizable;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MainMenuStrip = menuStrip1;
            MaximizeBox = true;
            Name = "Form1";
            Text = "ATSプラグインエディタ V0.1.0";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            KeyDown += Form1_KeyDown;
            ((System.ComponentModel.ISupportInitialize)numMaxBrake).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            grpLineSpec.ResumeLayout(false);
            grpLineSpec.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numMaxLine).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinLine).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDefaultLine).EndInit();
            grpAtc.ResumeLayout(false);
            grpAtc.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numFailParameter).EndInit();
            grpVehicle.ResumeLayout(false);
            grpVehicle.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnSelectFile;
        private TextBox txtFilePath;
        private NumericUpDown numMaxBrake;
        private ComboBox cmbAtcType;
        private GroupBox grpVehicle;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem ファイルToolStripMenuItem;
        private ToolStripMenuItem ヘルプToolStripMenuItem;
        private ToolStripMenuItem 新規作成ToolStripMenuItem;
        private ToolStripMenuItem 開くToolStripMenuItem;
        private ToolStripMenuItem 保存ToolStripMenuItem;
        private ToolStripMenuItem 終了ToolStripMenuItem1;
        private ToolStripMenuItem バージョン情報ToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem 言語LangusageToolStripMenuItem;
        private ToolStripMenuItem 日本語ToolStripMenuItem;
        private ToolStripMenuItem 英語ToolStripMenuItem;
        private ToolTip toolTip1;
        private Button CrashButton1;
        private GroupBox grpLineSpec;
        private NumericUpDown numMaxLine;
        private NumericUpDown numMinLine;
        private NumericUpDown numDefaultLine;
        private Label label7;
        private Label label6;
        private Label label5;
        private GroupBox grpAtc;
        private Label lblFailParameter;
        private NumericUpDown numFailParameter;
        private CheckBox chkAtcFail;
    }
}
