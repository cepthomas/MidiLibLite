using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLibLite
{
    public class ChannelControl : UserControl
    {
        #region Types
        /// <summary>Channel playing.</summary>
        public enum ChannelState { Normal, Solo, Mute }

        /// <summary>Notify host of UI changes.</summary>
        public class ChannelChangeEventArgs : EventArgs
        {
            public bool PatchChange { get; set; } = false;
            public bool ChannelNumberChange { get; set; } = false;
            public bool StateChange { get; set; } = false;
            public bool PresetFileChange { get; set; } = false;
        }
        #endregion

        #region Fields
        ChannelState _state = ChannelState.Normal;
        const int PAD = 4;
        const int SIZE = 48;

        readonly Container components = new();
        protected ToolTip toolTip = new();

        TextBox txtInfo = new();
        Slider sldVolume = new();
        // optional:
        Label lblSolo = new();
        Label lblMute = new();
        // optional:
        Slider sldControllerValue = new();
        Button btnSend = new();
        #endregion

        #region Properties
        /// <summary>My channel.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OutputChannel BoundChannel { get; set; }

        /// <summary>My renderer.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UserControl? UserRenderer
        {
            set
            {
                _userRenderer = value;
                _userRenderer.Location = new(PAD, Height);
                Height += _userRenderer.Height + PAD;
                Controls.Add(_userRenderer);
            }
        }
        UserControl? _userRenderer = null;

        /// <summary>Drawing the active elements of a control.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ControlColor
        {
            set
            {
                txtInfo.BackColor = value;
                sldVolume.DrawColor = value;
                sldControllerValue.DrawColor = value;
                btnSend.BackColor = value;
            }
        }

        /// <summary>Drawing the control when selected.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedColor { get; set; }

        /// <summary>For muting/soloing.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ChannelState State
        {
            get { return _state; }
            set { _state = value; UpdateUi(); }
        }

        /// <summary>Current volume.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Range(0.0, Defs.MAX_VOLUME)]
        public double Volume
        {
            get { return sldVolume.Value; }
            set { sldVolume.Value = value; }
        }
        #endregion

        #region Events
        /// <summary>UI channel config change.</summary>
        public event EventHandler<ChannelChangeEventArgs>? ChannelChange;

        /// <summary>UI midi send.</summary>
        public event EventHandler<BaseMidiEvent>? SendMidi;

        /// <summary>Derived class helper.</summary>
        protected virtual void OnSendMidi(BaseMidiEvent e) { SendMidi?.Invoke(this, e); }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public ChannelControl()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // Dummy channel to satisfy designer. Will be overwritten by the real one.
            var cfig = new OutputChannelConfig() { ChannelName = "DUMMY_CHANNEL" };
            var dev = new NullOutputDevice("DUMMY_DEVICE");
            BoundChannel = new OutputChannel(cfig, dev);
        }

        /// <summary>
        /// Apply customization from system. Properties should be valid now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            SuspendLayout();
            
            // Create the controls per the config.
            var opts = BoundChannel.Config.DisplayOptions; // shorthand
            int xPos = PAD;
            int xMax = xPos;
            int yPos = PAD;
            int yMax = yPos;

            if (true) // opts.HasFlag(ChannelControlOptions.Info))
            {
                txtInfo = new()
                {
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new(xPos, yPos),
                    Size = new(100, SIZE),
                    ReadOnly = true,
                    Multiline = true,
                    WordWrap = false,
                    Text = "Hello world\nWhat's up?"
                };
                txtInfo.Click += ChannelInfo_Click;
                Controls.Add(txtInfo);

                xPos = txtInfo.Right + PAD;
                xMax = xPos;
                yMax = Math.Max(txtInfo.Bottom, yMax);
            }

            if (opts.HasFlag(ChannelControlOptions.Notes))
            {
                sldVolume = new()
                {
                    Minimum = 0.0,
                    Maximum = Defs.MAX_VOLUME,
                    Resolution = 0.05,
                    Value = 1.0,
                    BorderStyle = BorderStyle.FixedSingle,
                    Orientation = Orientation.Horizontal,
                    Location = new(xPos, yPos),
                    Size = new(80, SIZE),
                    Label = "volume"
                };
                sldVolume.ValueChanged += (sender, e) => BoundChannel.Config.Volume = (sender as Slider)!.Value;
                Controls.Add(sldVolume);

                xPos = sldVolume.Right + PAD;
                xMax = xPos;
                yMax = Math.Max(sldVolume.Bottom, yMax);
            }

            if (opts.HasFlag(ChannelControlOptions.SoloMute))
            {
                lblSolo = new()
                {
                    Location = new(xPos, yPos),
                    Size = new(20, 20),//SIZE / 2),
                    Text = "S"
                };
                lblSolo.Click += SoloMute_Click;
                Controls.Add(lblSolo);

                lblMute = new()
                {
                    Location = new(xPos, yPos + SIZE / 2),
                    Size = new(20, 20),//SIZE / 2),
                    Text = "M"
                };
                lblMute.Click += SoloMute_Click;
                Controls.Add(lblMute);

                xPos = lblSolo.Right + PAD;
                xMax = xPos;
                yMax = Math.Max(lblMute.Bottom, yMax);
            }

            if (opts.HasFlag(ChannelControlOptions.Notes))
            {
                btnSend = new()
                {
                    FlatStyle = FlatStyle.Flat,
                    UseVisualStyleBackColor = true,
                    Location = new(xPos, yPos),
                    Size = new(SIZE, SIZE),
                    Text = "!",
                };
                btnSend.Click += Send_Click;
                Controls.Add(btnSend);

                xPos = btnSend.Right + PAD;
                yMax = Math.Max(btnSend.Bottom, yMax);

                sldControllerValue = new()
                {
                    Minimum = 0,
                    Maximum = MidiDefs.MAX_MIDI,
                    Resolution = 1,
                    Value = 50,
                    BorderStyle = BorderStyle.FixedSingle,
                    Orientation = Orientation.Horizontal,
                    Location = new(xPos, yPos),
                    Size = new(80, SIZE),
                    Label = "value"
                };
                sldControllerValue.ValueChanged += Controller_ValueChanged;
                Controls.Add(sldControllerValue);

                xPos = sldControllerValue.Right + PAD;
                xMax = xPos;
                yMax = Math.Max(sldControllerValue.Bottom, yMax);
            }

            // Form itself.
            Size = new Size(xMax + PAD, yMax + PAD);

            ResumeLayout(false);
            PerformLayout();

            toolTip = new(components);

            // Other inits.
            sldVolume.Value = BoundChannel.Config.Volume;
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
        /// <summary>Edit channel properties. Notifies client of any changes.</summary>
        void ChannelInfo_Click(object? sender, EventArgs e)
        {
            var changes = SettingsEditor.Edit(BoundChannel.Config, "Channel", 300);

            // Notify client.
            ChannelChangeEventArgs args = new()
            {
                ChannelNumberChange = changes.Any(ch => ch.name == "ChannelNumber"),
                PatchChange = changes.Any(ch => ch.name == "Patch"),
                PresetFileChange = changes.Any(ch => ch.name == "PresetFile"),
                StateChange = false
            };

            ChannelChange?.Invoke(this, new() { ChannelNumberChange = true });

            UpdateUi();
        }

        /// <summary>Handles solo and mute.</summary>
        void SoloMute_Click(object? sender, EventArgs e)
        {
            var lbl = sender as Label;

            // Figure out state.
            if (sender == lblSolo)
            {
                State = lblSolo.BackColor == SelectedColor ? ChannelState.Normal : ChannelState.Solo;
            }
            else if (sender == lblMute)
            {
                State = lblMute.BackColor == SelectedColor ? ChannelState.Normal : ChannelState.Mute;
            }

            // Notify client.
            ChannelChange?.Invoke(this, new() { StateChange = true });
        }

        /// <summary>
        /// Notify client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Controller_ValueChanged(object? sender, EventArgs e)
        {
            // No need to check limits.
            BoundChannel.Config.ControllerValue = (int)(sender as Slider)!.Value;
        }

        /// <summary>
        /// Notify client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Send_Click(object? sender, EventArgs e)
        {
            // No need to check limits.
            OnSendMidi(new Controller(BoundChannel.Config.ChannelNumber, BoundChannel.Config.ControllerId, BoundChannel.Config.ControllerValue));
        }
        #endregion

        #region Misc
        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Channel {BoundChannel.Handle}");
            sb.AppendLine($"Patch {BoundChannel.GetPatchName(BoundChannel.Config.Patch)}({BoundChannel.Config.Patch})");
            txtInfo.Text = sb.ToString();
            toolTip.SetToolTip(txtInfo, sb.ToString());

            lblSolo.BackColor = _state == ChannelState.Solo ? SelectedColor :  BackColor;
            lblMute.BackColor = _state == ChannelState.Mute ? SelectedColor : BackColor;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"{BoundChannel.Handle} P:{BoundChannel.Config.Patch}";
        }
        #endregion
    }
}
