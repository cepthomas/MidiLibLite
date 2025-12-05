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
        readonly Container components = new();
        readonly protected ToolTip toolTip;
        readonly TextBox txtInfo;
        readonly Slider sldVolume;
        // TODO2 thes could be optional for a simple control
        readonly Label lblSolo;
        readonly Label lblMute;

        ChannelState _state = ChannelState.Normal;
        const int PAD = 4;
        const int SIZE = 32;
        #endregion

        #region Properties
        /// <summary>My channel.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public ChannelHandle BoundChannel { get; set; }
        public OutputChannel BoundChannel { get; set; }

        /// <summary>Drawing the active elements of a control.</summary>
        public Color ControlColor 
        {
            set
            {
                sldVolume.ControlColor = value;
                txtInfo.BackColor = value;
            }
        }

        /// <summary>Drawing the control when selected.</summary>
        public Color SelectedColor { get; set; }

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
        /// Constructor. Create controls.
        /// </summary>
        public ChannelControl()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // InitializeComponent();
            SuspendLayout();

            // Satisfy designer and initial conditions,
            var dev = new NullOutputDevice("DUMMY_DEVICE");
            BoundChannel = new OutputChannel(dev, 1, "DUMMY_CHANNEL");

            txtInfo = new()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left,// | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new(PAD, PAD),
                Size = new(100, SIZE),
                ReadOnly = true,
                Text = "Hello world"
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
                Size = new(80, SIZE),
                Label = "volume"
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

            // Form.
            Size = new Size(lblSolo.Right + PAD, SIZE + PAD + PAD);

            ResumeLayout(false);
            PerformLayout();

            toolTip = new(components);
        }

        /// <summary>
        /// Apply customization from system. Properties should be valid now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            sldVolume.Value = BoundChannel.Volume;

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
            ChannelChange?.Invoke(this, new() { StateChange = true });
        }
        #endregion

        #region Misc
        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            txtInfo.Text = ToString();

            StringBuilder sb = new();
            sb.AppendLine($"Channel {BoundChannel.Handle}");
            sb.AppendLine($"Patch {BoundChannel.GetPatchName(BoundChannel.Patch)}({BoundChannel.Patch})");
            toolTip.SetToolTip(txtInfo, sb.ToString());

            lblSolo.BackColor = _state == ChannelState.Solo ? SelectedColor :  BackColor;
            lblMute.BackColor = _state == ChannelState.Mute ? SelectedColor : BackColor;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"{BoundChannel.Handle} P:{BoundChannel.Patch}";
        }
        #endregion
    }
}
