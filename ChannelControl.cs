using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
        const int SIZE = 24;

        readonly Container components = new();
        readonly ToolTip toolTip;

        readonly TextBox txtInfo;
        readonly Slider sldVolume;

        // TODO1 option ===>
        readonly Slider sldControllerValue;

        // TODO1 option ===>
        //SimpleChannelControl - has only editable channel_num, patch pick, volume, ControlColor !!! not used???
        readonly Label lblSolo;
        readonly Label lblMute;
        #endregion

        #region Properties
        /// <summary>My channel.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OutputChannel BoundChannel { get; init; }
        
        /// <summary>Drawing the active elements of a control.</summary>
        public Color DrawColor { get; set; } = Color.Red;

        /// <summary>Drawing the control when selected.</summary>
        public Color SelectedColor { get; set; } = Color.Green;

        /// <summary>The graphics draw area.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected Rectangle DrawRect
        {
            get { return new Rectangle(0, sldVolume.Bottom + PAD, Width, Height - (sldVolume.Bottom + PAD)); }
        }

        /// <summary>For muting/soloing.</summary>
        public ChannelState State
        {
            get { return _state; }
            set { _state = value; UpdateUi(); }
        }

        /// <summary>Current volume.</summary>
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
        public event EventHandler<BaseEvent>? SendMidi;

        /// <summary>Derived class helper.</summary>
        protected virtual void OnSendMidi(BaseEvent e) { SendMidi?.Invoke(this, e); }
        #endregion

        #region Lifecycle


        /// <summary>
        /// Constructor. Create controls.
        /// </summary>
        public ChannelControl()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // InitializeComponent();
            SuspendLayout();

            txtInfo = new()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left,// | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new(PAD, PAD),
                Size = new(100, SIZE),
                ReadOnly = true,
                Text = "---???---"
            };
            txtInfo.Click += ChannelInfo_Click;
            Controls.Add(txtInfo);

            sldVolume = new()
            {
                Minimum = 0.0,
                Maximum = Defs.MAX_VOLUME,
                Resolution = 0.05,
                Value = 1.0,
                BorderStyle = BorderStyle.FixedSingle,
                Orientation = Orientation.Horizontal,
                Location = new(txtInfo.Right + PAD, PAD),
                Size = new(80, SIZE)
            };
            sldVolume.ValueChanged += (sender, e) => BoundChannel.Volume = (sender as Slider)!.Value;
            Controls.Add(sldVolume);

            lblSolo = new()
            {
                Location = new(sldVolume.Right + PAD, PAD),
                Size = new(20, SIZE/2),
                Text = "S"
            };
            lblSolo.Click += SoloMute_Click;
            Controls.Add(lblSolo);

            lblMute = new()
            {
                Location = new(sldVolume.Right + PAD, 20),
                Size = new(20, SIZE/2),
                Text = "M"
            };
            lblMute.Click += SoloMute_Click;
            Controls.Add(lblMute);

            sldControllerValue = new()
            {
                Minimum = 0,
                Maximum = MidiDefs.MAX_MIDI,
                Resolution = 1,
                Value = 50,
                BorderStyle = BorderStyle.FixedSingle,
                Orientation = Orientation.Horizontal,
                Location = new(lblSolo.Right + PAD, PAD),
                Size = new(80, SIZE)
            };
            sldControllerValue.ValueChanged += Controller_ValueChanged;
            Controls.Add(sldControllerValue);

            // Form.
            Size = new Size(sldControllerValue.Right + PAD, SIZE + PAD + PAD);

            ResumeLayout(false);
            PerformLayout();

            toolTip = new(components);
        }

        /// <summary>
        /// Apply customization from system. BoundChannel should be valid now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            sldVolume.DrawColor = DrawColor;
            sldVolume.BackColor = BackColor;

            sldControllerValue.DrawColor = DrawColor;
            sldControllerValue.BackColor = BackColor;

            txtInfo.BackColor = DrawColor;
            toolTip.SetToolTip(txtInfo, txtInfo.Text);

            lblSolo.BackColor = BackColor;
            lblSolo.Click += SoloMute_Click;

            lblMute.BackColor = BackColor;
            lblMute.Click += SoloMute_Click;

            if (!DesignMode)
            {
                sldVolume.Value = BoundChannel.Volume;
                sldControllerValue.Value = BoundChannel.ControllerValue;
            }

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
        /// Notify client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Controller_ValueChanged(object? sender, EventArgs e)
        {
            // No need to check limits.
            BoundChannel.ControllerValue = (int)(sender as Slider)!.Value;
            OnSendMidi(new Controller(BoundChannel.ChannelNumber, BoundChannel.ControllerId, BoundChannel.ControllerValue));
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
            ChannelChangeEventArgs args = new()
            {
                StateChange = true
            };

            ChannelChange?.Invoke(this, new() { ChannelNumberChange = true });
        }
        #endregion

        #region Misc
        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            if (!DesignMode)
            {
                txtInfo.Text = ToString();

                StringBuilder sb = new();
                sb.AppendLine($"Channel {BoundChannel.ChannelNumber}");
                sb.AppendLine($"{BoundChannel.GetPatchName(BoundChannel.Patch)} {BoundChannel.Patch}");
                //// Determine patch name.
                //string sname;
                //if (ch.ChannelNumber == MidiDefs.DEFAULT_DRUM_CHANNEL)
                //{
                //    sname = $"kit: {patchNum}";
                //    if (MidiDefs.DrumKits.TryGetValue(patchNum, out string? kitName))
                //    {
                //        sname += ($" {kitName}");
                //    }
                //}
                //else
                //{
                //    sname = $"patch: {patchNum}";
                //    if (MidiDefs.Instruments.TryGetValue(patchNum, out string? patchName))
                //    {
                //        sname += ($" {patchName}");
                //    }
                //}
                toolTip.SetToolTip(txtInfo, sb.ToString());
            }

            lblSolo.BackColor = _state == ChannelState.Solo ? SelectedColor :  BackColor;
            lblMute.BackColor = _state == ChannelState.Mute ? SelectedColor : BackColor;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            if (!DesignMode)
            {
                return $"{BoundChannel.Handle} P:{BoundChannel.Patch}";
            }
            else
            {
                return "DesignMode";
            }
        }
        #endregion
    }
}
