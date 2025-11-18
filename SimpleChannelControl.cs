using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    /// <summary>A simpler channel UI component.</summary>
    public partial class SimpleChannelControl : UserControl
    {
        #region Backing fields
        int _channelNumber = 0;
        int _patch = -1;
        #endregion

        #region Properties
        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { _channelNumber = MathUtils.Constrain(value, 1, MidiDefs.NUM_CHANNELS); cmbChannel.SelectedIndex = _channelNumber - 1; }
        }

        /// <summary>Current patch.</summary>
        public int Patch
        {
            get { return _patch; }
            set { _patch = MathUtils.Constrain(value, 0, MidiDefs.MAX_MIDI); lblPatch.Text = MidiDefs.GetInstrumentName(_patch); }
        }

        /// <summary>Current volume.</summary>
        public double Volume
        {
            get { return sldVolume.Value; }
            set { sldVolume.Value = value; }
        }

        /// <summary>The color used for active control surfaces.</summary>
        public Color ControlColor { get; set; } = Color.Crimson;
        #endregion

        #region Events
        /// <summary>Notify host of asynchronous changes from user.</summary>
        public event EventHandler<ChannelChangeEventArgs>? ChannelChange;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public SimpleChannelControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            sldVolume.DrawColor = ControlColor;
            sldVolume.Minimum = 0.0;
            sldVolume.Maximum = MidiLibDefs.MAX_VOLUME;
            sldVolume.Value = MidiLibDefs.DEFAULT_VOLUME;

            lblPatch.Click += Patch_Click;

            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                cmbChannel.Items.Add($"{i + 1}");
            }
            cmbChannel.SelectedIndex = ChannelNumber - 1;
            cmbChannel.SelectedIndexChanged += (_, __) => _channelNumber = cmbChannel.SelectedIndex + 1;
            
            base.OnLoad(e);
        }
        #endregion

        #region Handlers for user selections
        /// <summary>
        /// User wants to change the patch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patch_Click(object? sender, EventArgs e)
        {
            int currentPatch = Patch;

            PatchPicker pp = new();
            pp.ShowDialog();
            if (pp.PatchNumber != -1)
            {
                Patch = pp.PatchNumber;
                ChannelChange?.Invoke(this, new() { PatchChange = true } );
            }
        }
        #endregion



        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.sldVolume = new NBagOfUis.Slider();
            this.lblPatch = new System.Windows.Forms.Label();
            this.cmbChannel = new System.Windows.Forms.ComboBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // sldVolume
            // 
            this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldVolume.DrawColor = System.Drawing.Color.White;
            this.sldVolume.Label = "";
            this.sldVolume.Location = new System.Drawing.Point(217, 5);
            this.sldVolume.Maximum = 10D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.Resolution = 0.1D;
            this.sldVolume.Size = new System.Drawing.Size(83, 30);
            this.sldVolume.TabIndex = 50;
            this.toolTip1.SetToolTip(this.sldVolume, "Channel Volume");
            this.sldVolume.Value = 5D;
            // 
            // lblPatch
            // 
            this.lblPatch.Location = new System.Drawing.Point(67, 9);
            this.lblPatch.Name = "lblPatch";
            this.lblPatch.Size = new System.Drawing.Size(144, 25);
            this.lblPatch.TabIndex = 49;
            this.lblPatch.Text = "?????";
            this.toolTip1.SetToolTip(this.lblPatch, "Patch");
            // 
            // cmbChannel
            // 
            this.cmbChannel.BackColor = System.Drawing.SystemColors.Control;
            this.cmbChannel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbChannel.FormattingEnabled = true;
            this.cmbChannel.Location = new System.Drawing.Point(5, 7);
            this.cmbChannel.Name = "cmbChannel";
            this.cmbChannel.Size = new System.Drawing.Size(52, 28);
            this.cmbChannel.TabIndex = 51;
            this.toolTip1.SetToolTip(this.cmbChannel, "Midi Channel Number");
            // 
            // SimpleChannelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cmbChannel);
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.lblPatch);
            this.Name = "SimpleChannelControl";
            this.Size = new System.Drawing.Size(309, 41);
            this.ResumeLayout(false);

        }

        #endregion

        private NBagOfUis.Slider sldVolume;
        private System.Windows.Forms.Label lblPatch;
        private System.Windows.Forms.ComboBox cmbChannel;
        private System.Windows.Forms.ToolTip toolTip1;
        
    }
}
