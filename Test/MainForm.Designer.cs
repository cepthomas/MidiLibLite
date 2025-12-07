

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
            sldVolume = new Ephemera.NBagOfUis.Slider();
            button1 = new System.Windows.Forms.Button();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { btnLogMidi, btnKillMidi });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(926, 26);
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
            txtViewer.Location = new System.Drawing.Point(357, 103);
            txtViewer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtViewer.MaxText = 5000;
            txtViewer.Name = "txtViewer";
            txtViewer.Prompt = "";
            txtViewer.Size = new System.Drawing.Size(557, 358);
            txtViewer.TabIndex = 58;
            txtViewer.WordWrap = true;
            // 
            // sldVolume
            // 
            sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            sldVolume.DrawColor = System.Drawing.Color.Red;
            sldVolume.Label = "";
            sldVolume.Location = new System.Drawing.Point(8, 38);
            sldVolume.Maximum = 10D;
            sldVolume.Minimum = 0D;
            sldVolume.Name = "sldVolume";
            sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            sldVolume.Resolution = 0.1D;
            sldVolume.Size = new System.Drawing.Size(138, 48);
            sldVolume.TabIndex = 99;
            sldVolume.Value = 5D;
            // 
            // button1
            // 
            button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button1.Location = new System.Drawing.Point(790, 41);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(54, 45);
            button1.TabIndex = 104;
            button1.Text = "test";
            button1.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(926, 475);
            Controls.Add(button1);
            Controls.Add(sldVolume);
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
        private NBagOfUis.Slider sldVolume;
        private NBagOfUis.TextViewer txtViewer;
        private System.Windows.Forms.ToolStripButton btnLogMidi;
        private System.Windows.Forms.ToolStripButton btnKillMidi;
        private CustomChannelControl cch1;
        private CustomChannelControl cch2;
        private System.Windows.Forms.Button button1;
    }
}

