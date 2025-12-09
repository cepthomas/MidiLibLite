

namespace Ephemera.MidiLibLite.Test
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            btnLogMidi = new System.Windows.Forms.ToolStripButton();
            btnKillMidi = new System.Windows.Forms.ToolStripButton();
            txtViewer = new Ephemera.NBagOfUis.TextViewer();
            sldMasterVolume = new Ephemera.NBagOfUis.Slider();
            button1 = new System.Windows.Forms.Button();
            ch_ctrl1 = new ChannelControl();
            ch_ctrl2 = new ChannelControl();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { btnLogMidi, btnKillMidi });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(777, 26);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // btnLogMidi
            // 
            btnLogMidi.CheckOnClick = true;
            btnLogMidi.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnLogMidi.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnLogMidi.Name = "btnLogMidi";
            btnLogMidi.Size = new System.Drawing.Size(62, 23);
            btnLogMidi.Text = "log midi";
            btnLogMidi.ToolTipText = "Enable logging midi events";
            // 
            // btnKillMidi
            // 
            btnKillMidi.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnKillMidi.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnKillMidi.Name = "btnKillMidi";
            btnKillMidi.Size = new System.Drawing.Size(29, 23);
            btnKillMidi.Text = "kill";
            btnKillMidi.ToolTipText = "Kill all midi channels";
            // 
            // txtViewer
            // 
            txtViewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtViewer.Location = new System.Drawing.Point(12, 345);
            txtViewer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtViewer.MaxText = 5000;
            txtViewer.Name = "txtViewer";
            txtViewer.Prompt = "";
            txtViewer.Size = new System.Drawing.Size(753, 242);
            txtViewer.TabIndex = 58;
            txtViewer.WordWrap = true;
            // 
            // sldMasterVolume
            // 
            sldMasterVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            sldMasterVolume.DrawColor = System.Drawing.Color.Red;
            sldMasterVolume.Label = "";
            sldMasterVolume.Location = new System.Drawing.Point(8, 38);
            sldMasterVolume.Maximum = 10D;
            sldMasterVolume.Minimum = 0D;
            sldMasterVolume.Name = "sldMasterVolume";
            sldMasterVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            sldMasterVolume.Resolution = 0.1D;
            sldMasterVolume.Size = new System.Drawing.Size(138, 48);
            sldMasterVolume.TabIndex = 99;
            sldMasterVolume.Value = 5D;
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(181, 41);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(54, 45);
            button1.TabIndex = 104;
            button1.Text = "test";
            button1.UseVisualStyleBackColor = true;
            // 
            // ch_ctrl1
            // 
            ch_ctrl1.Location = new System.Drawing.Point(8, 101);
            ch_ctrl1.Name = "ch_ctrl1";
            ch_ctrl1.Size = new System.Drawing.Size(356, 56);
            ch_ctrl1.TabIndex = 105;
            // 
            // ch_ctrl2
            // 
            ch_ctrl2.Location = new System.Drawing.Point(409, 101);
            ch_ctrl2.Name = "ch_ctrl2";
            ch_ctrl2.Size = new System.Drawing.Size(356, 56);
            ch_ctrl2.TabIndex = 106;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(777, 600);
            Controls.Add(ch_ctrl2);
            Controls.Add(ch_ctrl1);
            Controls.Add(button1);
            Controls.Add(sldMasterVolume);
            Controls.Add(txtViewer);
            Controls.Add(toolStrip1);
            Location = new System.Drawing.Point(300, 50);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            Text = "Midi Lib Test";
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private NBagOfUis.Slider sldMasterVolume;
        private NBagOfUis.TextViewer txtViewer;
        private System.Windows.Forms.ToolStripButton btnLogMidi;
        private System.Windows.Forms.ToolStripButton btnKillMidi;
        private System.Windows.Forms.Button button1;
        private ChannelControl ch_ctrl1;
        private ChannelControl ch_ctrl2;
    }
}

