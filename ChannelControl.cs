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

    public class ChannelControl : UserControl
    {
        #region Fields
        readonly Container components = new();
        readonly TextBox txtInfo;
        readonly Slider sldVolume;
        readonly ToolTip toolTip;

        // TODO1 option ===>
        readonly Slider sldControllerValue;

        // TODO1 option ===>
        ChannelState _state = ChannelState.Normal;
        readonly Label lblSolo;
        readonly Label lblMute;
        #endregion

        #region Properties
        /// <summary>My channel.</summary>
        [Required]
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
            get { return new Rectangle(0, sldVolume.Bottom + 4, Width, Height - (sldVolume.Bottom + 4)); }
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
        public ChannelControl(OutputChannel channel)
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // InitializeComponent();
            SuspendLayout();

            BoundChannel = channel;

            sldVolume = new()
            {
                Minimum = 0.0,
                Maximum = Defs.MAX_VOLUME,
                Resolution = 0.05,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new(5, 5),
                //Location = new Point(lblMute.Right + 4, 4),
                Name = "sldVolume",
                Orientation = Orientation.Horizontal,
                Size = new(80, 32)
            };
            sldVolume.ValueChanged += (sender, e) => BoundChannel.Volume = (sender as Slider)!.Value;
            Controls.Add(sldVolume);

            sldControllerValue = new()
            {
                Minimum = 0,
                Maximum = MidiDefs.MAX_MIDI,
                Resolution = 1,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new(95, 5),
                Name = "sldControllerValue",
                Orientation = Orientation.Horizontal,
                Size = new(80, 32)
            };
            sldControllerValue.ValueChanged += Controller_ValueChanged;
            Controls.Add(sldControllerValue);

            txtInfo = new()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(2, 9),
                Name = "txtInfo",
                ReadOnly = true,
                Size = new Size(40, 20),
                Text = "???"
            };
            txtInfo.Click += ChannelInfo_Click;
            Controls.Add(txtInfo);

            lblSolo = new()
            {
                Location = new Point(txtInfo.Right + 4, 9),
                Size = new Size(20, 20),
                Text = "S"
            };
            Controls.Add(lblSolo);

            lblMute = new()
            {
                Location = new Point(lblSolo.Right + 4, 9),
                Size = new Size(20, 20),
                Text = "M"
            };
            Controls.Add(lblMute);

            // AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            // AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // BorderStyle = BorderStyle.FixedSingle;
            // BackColor = SystemColors.Control;
            // Name = "ChannelControl";
            Size = new(372, 42);
            // Size = new Size(sldVolume.Right + 5, 38);

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
            sldVolume.Value = BoundChannel.Volume;
            sldVolume.DrawColor = DrawColor;
            sldVolume.BackColor = BackColor;

            sldControllerValue.Value = BoundChannel.ControllerValue;
            sldControllerValue.DrawColor = DrawColor;
            sldControllerValue.BackColor = BackColor;

            txtInfo.BackColor = DrawColor;
            toolTip.SetToolTip(txtInfo, txtInfo.Text);

            lblSolo.BackColor = BackColor;
            lblSolo.Click += SoloMute_Click;

            lblMute.BackColor = BackColor;
            lblMute.Click += SoloMute_Click;

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
            txtInfo.Text = ToString();

            StringBuilder sb = new();
            sb.AppendLine($"Channel {BoundChannel.ChannelNumber}");
            sb.AppendLine($"{BoundChannel.GetPatchName(BoundChannel.Patch)} {BoundChannel.Patch}");
            toolTip.SetToolTip(txtInfo, sb.ToString());

            lblSolo.BackColor = _state == ChannelState.Solo ? SelectedColor :  BackColor;
            lblMute.BackColor = _state == ChannelState.Mute ? SelectedColor : BackColor;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"{BoundChannel.ChHandle} P{BoundChannel.Patch}";

            //    List<string> ret = [];
            //    ret.Add($"{(ch.Direction == Direction.Output ? "output: " : "input: ")}:{chanName}");
            //    ret.Add($"device: {devName}");
            //    if (patchNum != -1)
            //    {
            //        // Determine patch name.
            //        string sname;
            //        if (ch.ChannelNumber == MidiDefs.DEFAULT_DRUM_CHANNEL)
            //        {
            //            sname = $"kit: {patchNum}";
            //            if (MidiDefs.DrumKits.TryGetValue(patchNum, out string? kitName))
            //            {
            //                sname += ($" {kitName}");
            //            }
            //        }
            //        else
            //        {
            //            sname = $"patch: {patchNum}";
            //            if (MidiDefs.Instruments.TryGetValue(patchNum, out string? patchName))
            //            {
            //                sname += ($" {patchName}");
            //            }
            //        }
            //        ret.Add(sname);
            //    }
        }
        #endregion
    }
}



/////////////////////////////////////////////////////////////////////////
/*  TODO1  SimpleChannelControl from MidiLib - has only editable channel_num, patch pick, volume, ControlColor !!! not used???
    public class SimpleChannelControl : UserControl
    {
        #region Fields
        readonly Container components = new();
        readonly NBagOfUis.Slider sldVolume;
        readonly Label lblPatch;
        readonly ComboBox cmbChannel;
        readonly ToolTip toolTip1;

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
            // InitializeComponent();

            components = new System.ComponentModel.Container();
            sldVolume = new NBagOfUis.Slider();
            lblPatch = new Label();
            cmbChannel = new ComboBox();
            toolTip1 = new ToolTip(components);
            SuspendLayout();

            sldVolume.BorderStyle = BorderStyle.FixedSingle;
            sldVolume.DrawColor = Color.White;
            sldVolume.Label = "";
            sldVolume.Location = new Point(217, 5);
            sldVolume.Maximum = 10D;
            sldVolume.Minimum = 0D;
            sldVolume.Name = "sldVolume";
            sldVolume.Orientation = Orientation.Horizontal;
            sldVolume.Resolution = 0.1D;
            sldVolume.Size = new Size(83, 30);
            toolTip1.SetToolTip(sldVolume, "Channel Volume");
            sldVolume.Value = 5D;

            lblPatch.Location = new Point(67, 9);
            lblPatch.Name = "lblPatch";
            lblPatch.Size = new Size(144, 25);
            lblPatch.Text = "?????";
            toolTip1.SetToolTip(lblPatch, "Patch");

            cmbChannel.BackColor = SystemColors.Control;
            cmbChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbChannel.FormattingEnabled = true;
            cmbChannel.Location = new Point(5, 7);
            cmbChannel.Name = "cmbChannel";
            cmbChannel.Size = new Size(52, 28);
            toolTip1.SetToolTip(cmbChannel, "Midi Channel Number");

            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(cmbChannel);
            Controls.Add(sldVolume);
            Controls.Add(lblPatch);
            Name = "SimpleChannelControl";
            Size = new Size(309, 41);

            ResumeLayout(false);
        }

        /// <summary>
        /// App specific setup.
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
    }
*/
