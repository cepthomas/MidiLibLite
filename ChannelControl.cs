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
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLibLite
{
    public class ChannelControl : UserControl // from MidiGenerator
    {
        #region Fields
        readonly Container components = new();
        readonly Slider sldControllerValue = new();
        readonly TextBox txtChannelInfo = new();
        readonly Slider sldVolume = new();
        readonly ToolTip toolTip;
        #endregion

        #region Properties
        /// <summary>Everything about me.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Channel BoundChannel { get; set; } = new();

        /// <summary>Cosmetics.</summary>
        public Color ControlColor { get; set; } = Color.Red;

        /// <summary>The graphics draw area.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected Rectangle DrawRect { get { return new Rectangle(0, sldVolume.Bottom + 4, Width, Height - (sldVolume.Bottom + 4)); } }
        #endregion

        #region Events
        /// <summary>Notify host of changes from user.</summary>
        public event EventHandler<ChannelEventArgs>? ChannelChange;

        /// <summary>Click info.</summary>
        public event EventHandler<NoteEventArgs>? NoteSend;

        /// <summary>Notify host of changes from user.</summary>
        public event EventHandler<ControllerEventArgs>? ControllerSend;

        /// <summary>Derived class helper.</summary>
        protected virtual void OnNoteSend(NoteEventArgs e)
        {
            NoteSend?.Invoke(this, e);
        }

        /// <summary>Derived class helper.</summary>
        protected virtual void OnControllerSend(ControllerEventArgs e)
        {
            ControllerSend?.Invoke(this, e);
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor. Create controls.
        /// </summary>
        public ChannelControl()
        {
            // InitializeComponent();
            SuspendLayout();

            sldVolume.Minimum = 0.0;
            sldVolume.Maximum = Defs.MAX_VOLUME;
            sldVolume.Resolution = 0.05;
            sldVolume.ValueChanged += (sender, e) => BoundChannel.Volume = (sender as Slider)!.Value;
            sldVolume.BorderStyle = BorderStyle.FixedSingle;
            // sldVolume.Label = "";
            sldVolume.Location = new(5, 5);
            sldVolume.Name = "sldVolume";
            sldVolume.Orientation = Orientation.Horizontal;
            sldVolume.Size = new(80, 32);

            sldControllerValue.Minimum = 0;
            sldControllerValue.Maximum = MidiDefs.MAX_MIDI;
            sldControllerValue.Resolution = 1;
            sldControllerValue.ValueChanged += Controller_ValueChanged;
            sldControllerValue.BorderStyle = BorderStyle.FixedSingle;
            // sldControllerValue.Label = "";
            sldControllerValue.Location = new(95, 5);
            sldControllerValue.Name = "sldControllerValue";
            sldControllerValue.Orientation = Orientation.Horizontal;
            sldControllerValue.Size = new(80, 32);

            txtChannelInfo.Click += ChannelInfo_Click;
            txtChannelInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtChannelInfo.BorderStyle = BorderStyle.FixedSingle;
            txtChannelInfo.Location = new(185, 8);
            txtChannelInfo.Name = "txtChannelInfo";
            txtChannelInfo.ReadOnly = true;
            txtChannelInfo.Size = new(182, 26);

            // AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            // AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            Controls.Add(txtChannelInfo);
            Controls.Add(sldControllerValue);
            Controls.Add(sldVolume);
            Name = "ChannelControl";
            Size = new(372, 42);

            ResumeLayout(false);
            PerformLayout();

            toolTip = new(components);
        }

        /// <summary>
        /// Apply customization. Channel should be valid now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {

            sldVolume.Value = BoundChannel.Volume;
            sldVolume.DrawColor = ControlColor;
            sldVolume.BackColor = SystemColors.Control;
            sldControllerValue.Value = BoundChannel.ControllerValue;
            sldControllerValue.DrawColor = ControlColor;
            sldControllerValue.BackColor = SystemColors.Control;
            txtChannelInfo.BackColor = ControlColor;


            //sldVolume.Minimum = 0.0;
            //sldVolume.Maximum = Defs.MAX_VOLUME;
            //sldVolume.Resolution = 0.05;
            //sldVolume.Value = BoundChannel.Volume;
            //sldVolume.DrawColor = ControlColor;
            //sldVolume.ValueChanged += (object? sender, EventArgs e) => BoundChannel.Volume = (sender as Slider)!.Value;
            //sldVolume.BackColor = SystemColors.Control;
            //sldVolume.BorderStyle = BorderStyle.FixedSingle;
            //// sldVolume.Label = "";
            //sldVolume.Location = new(5, 5);
            //sldVolume.Name = "sldVolume";
            //sldVolume.Orientation = Orientation.Horizontal;
            //sldVolume.Size = new(80, 32);

            //sldControllerValue.Minimum = 0;
            //sldControllerValue.Maximum = MidiDefs.MAX_MIDI;
            //sldControllerValue.Resolution = 1;
            //sldControllerValue.Value = BoundChannel.ControllerValue;
            //sldControllerValue.DrawColor = ControlColor;
            //sldControllerValue.ValueChanged += Controller_ValueChanged;
            //sldControllerValue.BackColor = SystemColors.Control;
            //sldControllerValue.BorderStyle = BorderStyle.FixedSingle;
            //// sldControllerValue.Label = "";
            //sldControllerValue.Location = new(95, 5);
            //sldControllerValue.Name = "sldControllerValue";
            //sldControllerValue.Orientation = Orientation.Horizontal;
            //sldControllerValue.Size = new(80, 32);

            //txtChannelInfo.Click += ChannelInfo_Click;
            //txtChannelInfo.BackColor = ControlColor;
            //txtChannelInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            //txtChannelInfo.BorderStyle = BorderStyle.FixedSingle;
            //txtChannelInfo.Location = new(185, 8);
            //txtChannelInfo.Name = "txtChannelInfo";
            //txtChannelInfo.ReadOnly = true;
            //txtChannelInfo.Size = new(182, 26);


            //// AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            //// AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            //BackColor = SystemColors.Control;
            //Controls.Add(txtChannelInfo);
            //Controls.Add(sldControllerValue);
            //Controls.Add(sldVolume);
            //Name = "ChannelControl";
            //Size = new(372, 42);


            UpdateUi();

            base.OnLoad(e);
        }



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


        #endregion

        #region Handlers for user selections
        /// <summary>
        /// Notify client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Controller_ValueChanged(object? sender, EventArgs e)
        {
            // No need to check limits.
            BoundChannel.ControllerValue = (int)(sender as Slider)!.Value;
            OnControllerSend(new() { ControllerId = BoundChannel.ControllerId, Value = BoundChannel.ControllerValue });
        }

        /// <summary>
        /// Edit channel properties. Notifies client of any changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelInfo_Click(object? sender, EventArgs e)
        {
            var changes = SettingsEditor.Edit(BoundChannel, "Channel", 300);

            // Notify client.
            ChannelEventArgs args = new()
            {
                ChannelNumberChange = changes.Any(ch => ch.name == "ChannelNumber"),
                PatchChange = changes.Any(ch => ch.name == "Patch"),
                PresetFileChange = changes.Any(ch => ch.name == "PresetFile"),
            };

            ChannelChange?.Invoke(this, new() { ChannelNumberChange = true });

            UpdateUi();
        }
        #endregion

        /// <summary>
        /// Draw mode checkboxes etc.
        /// </summary>
        void UpdateUi()
        {
            txtChannelInfo.Text = ToString();

            StringBuilder sb = new();
            sb.AppendLine($"Channel {BoundChannel.ChannelNumber}");
            sb.AppendLine($"{BoundChannel.GetPatchName(BoundChannel.Patch)} {BoundChannel.Patch}");
            toolTip.SetToolTip(txtChannelInfo, sb.ToString());
        }

        /// <summary>
        /// Read me.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Ch{BoundChannel.ChannelNumber} P{BoundChannel.Patch}";
        }
    }
}
