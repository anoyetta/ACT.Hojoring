namespace ACT.TTSYukkuri.HOYA
{
    partial class HOYASettingsPanel
    {
        /// <summary> 
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナーで生成されたコード

        /// <summary> 
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.SpeakerComboBox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.EmotionComboBox = new System.Windows.Forms.ComboBox();
            this.EmotionLevelComboBox = new System.Windows.Forms.ComboBox();
            this.PitchTrackBar = new System.Windows.Forms.TrackBar();
            this.PitchTextBox = new System.Windows.Forms.TextBox();
            this.SpeedTextBox = new System.Windows.Forms.TextBox();
            this.PitchValueLabel = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.VolumeTextBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.VolumeTrackBar = new System.Windows.Forms.TrackBar();
            this.SpeedTrackBar = new System.Windows.Forms.TrackBar();
            this.DefaultButton = new System.Windows.Forms.Button();
            this.APIKeyTextBox = new System.Windows.Forms.TextBox();
            this.APIKeyLinkLabel = new System.Windows.Forms.LinkLabel();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.PitchTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.VolumeTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedTrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "話者";
            // 
            // SpeakerComboBox
            // 
            this.SpeakerComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SpeakerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SpeakerComboBox.FormattingEnabled = true;
            this.SpeakerComboBox.Location = new System.Drawing.Point(82, 53);
            this.SpeakerComboBox.Name = "SpeakerComboBox";
            this.SpeakerComboBox.Size = new System.Drawing.Size(292, 20);
            this.SpeakerComboBox.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 82);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "感情";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 108);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 12);
            this.label3.TabIndex = 3;
            this.label3.Text = "感情レベル";
            // 
            // EmotionComboBox
            // 
            this.EmotionComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.EmotionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.EmotionComboBox.FormattingEnabled = true;
            this.EmotionComboBox.Location = new System.Drawing.Point(82, 79);
            this.EmotionComboBox.Name = "EmotionComboBox";
            this.EmotionComboBox.Size = new System.Drawing.Size(292, 20);
            this.EmotionComboBox.TabIndex = 2;
            // 
            // EmotionLevelComboBox
            // 
            this.EmotionLevelComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.EmotionLevelComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.EmotionLevelComboBox.FormattingEnabled = true;
            this.EmotionLevelComboBox.Location = new System.Drawing.Point(82, 105);
            this.EmotionLevelComboBox.Name = "EmotionLevelComboBox";
            this.EmotionLevelComboBox.Size = new System.Drawing.Size(292, 20);
            this.EmotionLevelComboBox.TabIndex = 3;
            // 
            // PitchTrackBar
            // 
            this.PitchTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PitchTrackBar.LargeChange = 10;
            this.PitchTrackBar.Location = new System.Drawing.Point(113, 190);
            this.PitchTrackBar.Maximum = 200;
            this.PitchTrackBar.Minimum = 50;
            this.PitchTrackBar.Name = "PitchTrackBar";
            this.PitchTrackBar.Size = new System.Drawing.Size(261, 45);
            this.PitchTrackBar.TabIndex = 6;
            this.PitchTrackBar.TickFrequency = 10;
            this.PitchTrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.PitchTrackBar.Value = 50;
            // 
            // PitchTextBox
            // 
            this.PitchTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.PitchTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.PitchTextBox.Location = new System.Drawing.Point(82, 187);
            this.PitchTextBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.PitchTextBox.Name = "PitchTextBox";
            this.PitchTextBox.Size = new System.Drawing.Size(25, 12);
            this.PitchTextBox.TabIndex = 25;
            this.PitchTextBox.TabStop = false;
            this.PitchTextBox.Text = "100";
            this.PitchTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // SpeedTextBox
            // 
            this.SpeedTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.SpeedTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.SpeedTextBox.Location = new System.Drawing.Point(82, 163);
            this.SpeedTextBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.SpeedTextBox.Name = "SpeedTextBox";
            this.SpeedTextBox.Size = new System.Drawing.Size(25, 12);
            this.SpeedTextBox.TabIndex = 24;
            this.SpeedTextBox.TabStop = false;
            this.SpeedTextBox.Text = "100";
            this.SpeedTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // PitchValueLabel
            // 
            this.PitchValueLabel.AutoSize = true;
            this.PitchValueLabel.Location = new System.Drawing.Point(3, 187);
            this.PitchValueLabel.Name = "PitchValueLabel";
            this.PitchValueLabel.Size = new System.Drawing.Size(29, 12);
            this.PitchValueLabel.TabIndex = 23;
            this.PitchValueLabel.Text = "音程";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 163);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(42, 12);
            this.label8.TabIndex = 22;
            this.label8.Text = "スピード";
            // 
            // VolumeTextBox
            // 
            this.VolumeTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.VolumeTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.VolumeTextBox.Location = new System.Drawing.Point(82, 139);
            this.VolumeTextBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.VolumeTextBox.Name = "VolumeTextBox";
            this.VolumeTextBox.Size = new System.Drawing.Size(25, 12);
            this.VolumeTextBox.TabIndex = 21;
            this.VolumeTextBox.TabStop = false;
            this.VolumeTextBox.Text = "100";
            this.VolumeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 139);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(50, 12);
            this.label7.TabIndex = 20;
            this.label7.Text = "ボリューム";
            // 
            // VolumeTrackBar
            // 
            this.VolumeTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.VolumeTrackBar.LargeChange = 10;
            this.VolumeTrackBar.Location = new System.Drawing.Point(113, 139);
            this.VolumeTrackBar.Maximum = 200;
            this.VolumeTrackBar.Minimum = 50;
            this.VolumeTrackBar.Name = "VolumeTrackBar";
            this.VolumeTrackBar.Size = new System.Drawing.Size(261, 45);
            this.VolumeTrackBar.TabIndex = 4;
            this.VolumeTrackBar.TickFrequency = 10;
            this.VolumeTrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.VolumeTrackBar.Value = 100;
            // 
            // SpeedTrackBar
            // 
            this.SpeedTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SpeedTrackBar.LargeChange = 20;
            this.SpeedTrackBar.Location = new System.Drawing.Point(113, 163);
            this.SpeedTrackBar.Maximum = 400;
            this.SpeedTrackBar.Minimum = 50;
            this.SpeedTrackBar.Name = "SpeedTrackBar";
            this.SpeedTrackBar.Size = new System.Drawing.Size(261, 45);
            this.SpeedTrackBar.TabIndex = 5;
            this.SpeedTrackBar.TickFrequency = 20;
            this.SpeedTrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.SpeedTrackBar.Value = 100;
            // 
            // DefaultButton
            // 
            this.DefaultButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.DefaultButton.Location = new System.Drawing.Point(5, 225);
            this.DefaultButton.Name = "DefaultButton";
            this.DefaultButton.Size = new System.Drawing.Size(75, 23);
            this.DefaultButton.TabIndex = 7;
            this.DefaultButton.Text = "リセット";
            this.DefaultButton.UseVisualStyleBackColor = true;
            // 
            // APIKeyTextBox
            // 
            this.APIKeyTextBox.Location = new System.Drawing.Point(82, 23);
            this.APIKeyTextBox.Name = "APIKeyTextBox";
            this.APIKeyTextBox.Size = new System.Drawing.Size(292, 19);
            this.APIKeyTextBox.TabIndex = 0;
            // 
            // APIKeyLinkLabel
            // 
            this.APIKeyLinkLabel.AutoSize = true;
            this.APIKeyLinkLabel.Location = new System.Drawing.Point(3, 26);
            this.APIKeyLinkLabel.Name = "APIKeyLinkLabel";
            this.APIKeyLinkLabel.Size = new System.Drawing.Size(46, 12);
            this.APIKeyLinkLabel.TabIndex = 30;
            this.APIKeyLinkLabel.TabStop = true;
            this.APIKeyLinkLabel.Text = "API Key";
            this.toolTip.SetToolTip(this.APIKeyLinkLabel, "無料登録してAPI Keyを取得する");
            // 
            // HOYASettingsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.APIKeyLinkLabel);
            this.Controls.Add(this.APIKeyTextBox);
            this.Controls.Add(this.DefaultButton);
            this.Controls.Add(this.PitchTrackBar);
            this.Controls.Add(this.SpeedTrackBar);
            this.Controls.Add(this.PitchTextBox);
            this.Controls.Add(this.SpeedTextBox);
            this.Controls.Add(this.PitchValueLabel);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.VolumeTextBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.VolumeTrackBar);
            this.Controls.Add(this.EmotionLevelComboBox);
            this.Controls.Add(this.EmotionComboBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.SpeakerComboBox);
            this.Controls.Add(this.label1);
            this.Name = "HOYASettingsPanel";
            this.Size = new System.Drawing.Size(377, 251);
            ((System.ComponentModel.ISupportInitialize)(this.PitchTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.VolumeTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedTrackBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox SpeakerComboBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox EmotionComboBox;
        private System.Windows.Forms.ComboBox EmotionLevelComboBox;
        private System.Windows.Forms.TrackBar PitchTrackBar;
        private System.Windows.Forms.TextBox PitchTextBox;
        private System.Windows.Forms.TextBox SpeedTextBox;
        private System.Windows.Forms.Label PitchValueLabel;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox VolumeTextBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TrackBar VolumeTrackBar;
        private System.Windows.Forms.TrackBar SpeedTrackBar;
        private System.Windows.Forms.Button DefaultButton;
        private System.Windows.Forms.TextBox APIKeyTextBox;
        private System.Windows.Forms.LinkLabel APIKeyLinkLabel;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
