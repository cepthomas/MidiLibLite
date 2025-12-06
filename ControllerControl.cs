using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLibLite
{
    /// <summary>One channel controller config. Host can persist these.</summary>
    [Serializable]
    public class ControllerConfig
    {
        [Editor(typeof(DeviceTypeEditor), typeof(UITypeEditor))]
        public string DeviceName { get; set; } = "";

        [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
        [Range(1, MidiDefs.NUM_CHANNELS)]
        public int ChannelNumber { get; set; } = 0;

        /// <summary>Edit current controller number.</summary>
        [Editor(typeof(ControllerIdTypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ControllerIdConverter))]
        [Range(0, MidiDefs.MAX_MIDI)]
        public int ControllerId { get; set; } = 1;

        /// <summary>Controller payload.</summary>
        [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
        [Range(0, MidiDefs.MAX_MIDI)]
        public int ControllerValue { get; set; } = 0;
    }



    public class ControllerControl : UserControl
    {
        #region Fields
        readonly Container components = new();
        readonly protected ToolTip toolTip;
        readonly TextBox txtInfo;
        readonly Slider sldControllerValue;
        readonly Button btnSend;

        const int PAD = 4;
        const int SIZE = 32;
        #endregion

        #region Properties
        /// <summary>My config.</summary>
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ControllerConfig Config
        {
            get {  return _config; }
            set {  _config = value; UpdateUi(); }
        }
        ControllerConfig _config = new();

        /// <summary>Associated device.</summary>
        // [Browsable(false)]
        // [JsonIgnore]
        public IOutputDevice Device { get; init; } // TODO1
        // public int DeviceId { get; set; } = 0;


        /// <summary>Drawing the active elements of a control.</summary>
      //  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ControlColor 
        {
            set
            {
                sldControllerValue.DrawColor = value;
                txtInfo.BackColor = value;
                btnSend.BackColor = value;
            }
        }

        /// <summary>Drawing the control when selected.</summary>
       // [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedColor { get; set; }

        /// <summary>The graphics draw area. TODO1 needed?</summary>
     //   [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected Rectangle DrawRect
        {
            get { return new Rectangle(0, sldControllerValue.Bottom + PAD, Width, Height - (sldControllerValue.Bottom + PAD)); }
        }
        #endregion

        #region Events
        /// <summary>UI midi send.</summary>
        public event EventHandler<BaseMidiEvent>? SendMidi;

        /// <summary>Derived class helper.</summary>
        protected virtual void OnSendMidi(BaseMidiEvent e) { SendMidi?.Invoke(this, e); }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor. Create controls.
        /// </summary>
        public ControllerControl()
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
                Text = "Hello world"
            };
            txtInfo.Click += Info_Click;
            Controls.Add(txtInfo);

            btnSend = new()
            {
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = true,
                Location = new(txtInfo.Right + PAD, PAD),
                Size = new(SIZE, SIZE),
                Text = "S",
            };
            btnSend.Click += Send_Click;
            Controls.Add(btnSend);

            sldControllerValue = new()
            {
                Minimum = 0,
                Maximum = MidiDefs.MAX_MIDI,
                Resolution = 1,
                Value = 50,
                BorderStyle = BorderStyle.FixedSingle,
                Orientation = Orientation.Horizontal,
                Location = new(btnSend.Right + PAD, PAD),
                Size = new(80, SIZE),
                Label = "value"
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
        /// Apply customization from system. c should be valid now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            sldControllerValue.Value = Config.ControllerValue;

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
            Config.ControllerValue = (int)(sender as Slider)!.Value;
        }

        /// <summary>
        /// Notify client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Send_Click(object? sender, EventArgs e)
        {
            // No need to check limits.
            OnSendMidi(new Controller(_config.ChannelNumber, _config.ControllerId, _config.ControllerValue));
        }

        /// <summary>
        /// Edit controller properties.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Info_Click(object? sender, EventArgs e)
        {
            var changes = SettingsEditor.Edit(Config, "Controller", 300);

            UpdateUi();
        }
        #endregion

        #region Misc
        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            txtInfo.Text = ToString();
            sldControllerValue.Value = Config.ControllerValue;

            StringBuilder sb = new();
            sb.AppendLine($"Channel, patch TODO_defs etc");
            // sb.AppendLine($"Patch {BoundChannel.GetPatchName(BoundChannel.Patch)}({BoundChannel.Patch})");
            toolTip.SetToolTip(txtInfo, sb.ToString());
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            //return $"{Config.ChannelHandle} CId:{Config.ControllerId}";
            return $"Ch:{Config.ChannelNumber} CId:{Config.ControllerId}";
        }
        #endregion
    }

}
