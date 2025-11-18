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



// from Nebulua + stuff
namespace Ephemera.MidiLibLite
{
    /// <summary>Notify host of changes.</summary>
//    public class ChannelControlEventArgs() : EventArgs;
    
    /// <summary>Channel events and other properties.</summary>
    public class ChannelControl : UserControl
    {
        #region Fields
        readonly Container components = new();
        // readonly Container? components = null;
        readonly ToolTip toolTip;

        readonly Color _selColor = Color.Blue;
        readonly Color _unselColor = Color.Red;

        readonly Label lblChannelInfo;
        readonly Label lblSolo;
        readonly Label lblMute;
        readonly Slider sldVolume;

        PlayState _state = PlayState.Normal;
        #endregion

        #region Events
        /// <summary>Notify host of user changes.</summary>
        public event EventHandler<ChannelChangeEventArgs>? ChannelChange;
        #endregion


        #region Properties
        /// <summary>Handle.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public ChannelHandle ChHandle { get; init; }

        /// <summary>For display.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public List<string> Info { get; set; } = ["???"];

        /// <summary>For muting/soloing.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public PlayState State
        {
            get { return _state; }
            set { _state = value; UpdateUi(); }
        }

        /// <summary>Current volume.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public double Volume
        {
            get { return sldVolume.Value; }
            set { sldVolume.Value = value; }
        }



        /// <summary>Everything about me.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Channel Channel { get; set; } = new(); // ====>>> namespace MidiGenerator

        /// <summary>Cosmetics.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Color ControlColor { get; set; } = Color.Red; // ====>>> namespace MidiGenerator

        /// <summary>Cosmetics.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Color ActiveColor { get; set; } = Color.Red; // ====>>> namespace MidiGenerator

        /// <summary>Cosmetics.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Color SelectedColor { get; set; } = Color.Red; // ====>>> namespace MidiGenerator

        /// <summary>Cosmetics.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Color BackColor { get; set; } = Color.Red; // ====>>> namespace MidiGenerator


        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="channelNumber"></param>
        public ChannelControl(ChannelHandle ch) : this()
        {
            ChHandle = ch;

            // Colors.
            _selColor = SelectedColor;
            _unselColor = BackColor;
            lblChannelInfo.BackColor = _unselColor;
            lblSolo.BackColor = _unselColor;
            lblMute.BackColor = _unselColor;
            sldVolume.BackColor = _unselColor;
            sldVolume.ForeColor = ActiveColor;

            toolTip.SetToolTip(this, string.Join(Environment.NewLine, Info));
        }

        /// <summary>
        /// Designer constructor.
        /// </summary>
        public ChannelControl()
        {
            // Dummy to keep the designer happy.
            ChHandle = new(-1, -1, Direction.None);

            SuspendLayout();

            lblChannelInfo = new()
            {
                Location = new Point(2, 9),
                Size = new Size(40, 20),
                Text = "?"
            };
            components.Add(lblChannelInfo);

            lblSolo = new()
            {
                Location = new Point(lblChannelInfo.Right + 4, 9),
                Size = new Size(20, 20),
                Text = "S"
            };
            components.Add(lblSolo);

            lblMute = new()
            {
                Location = new Point(lblSolo.Right + 4, 9),
                Size = new Size(20, 20),
                Text = "M"
            };
            components.Add(lblMute);

            sldVolume = new()
            {
                Location = new Point(lblMute.Right + 4, 4),
                Size = new Size(40, 30),
                Orientation = Orientation.Horizontal,
                BorderStyle = BorderStyle.FixedSingle,
                Maximum = MidiLibDefs.MAX_VOLUME,
                Minimum = 0.0,
                Value = MidiLibDefs.DEFAULT_VOLUME,
                Resolution = 0.05
            };
            components.Add(sldVolume);

            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Size = new Size(sldVolume.Right + 5, 38);
            BorderStyle = BorderStyle.FixedSingle;

            toolTip = new(components);

            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>
        /// Final UI init.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            //lblChannelInfo.BorderStyle = BorderStyle.FixedSingle;
            //lblSolo.BorderStyle = BorderStyle.FixedSingle;
            //lblMute.BorderStyle = BorderStyle.FixedSingle;

            lblChannelInfo.Text = $"{ChHandle.DeviceId}:{ChHandle.ChannelNumber}";
            lblChannelInfo.BackColor = _unselColor;
            lblChannelInfo.BackColor = Color.LightBlue;
            lblChannelInfo.Click += ChannelEd_Click;

            toolTip.SetToolTip(lblChannelInfo, string.Join(Environment.NewLine, Info));

            sldVolume.DrawColor = ActiveColor;
            sldVolume.DrawColor = ControlColor;
            sldVolume.Value = Channel.Volume;
            sldVolume.ValueChanged += Volume_ValueChanged;

            lblSolo.Click += SoloMute_Click;
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
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion



        #region Handlers for user selections
        /// <summary>Handles solo and mute.</summary>
        void SoloMute_Click(object? sender, EventArgs e)
        {
            var lbl = sender as Label;

            // Figure out state.
            if (sender == lblSolo)
            {
                State = lblSolo.BackColor == _selColor ? PlayState.Normal : PlayState.Solo;
            }
            else if (sender == lblMute)
            {
                State = lblMute.BackColor == _selColor ? PlayState.Normal : PlayState.Mute;
            }
            //else ??

            ChannelChange?.Invoke(this, new ChannelChangeEventArgs());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Volume_ValueChanged(object? sender, EventArgs e) // ====>>> namespace MidiGenerator
        {
            // No need to chec`k limits.
            Channel.Volume = (sender as Slider)!.Value;
        }

        /// <summary>
        /// Handle selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelEd_Click(object? sender, EventArgs e) // ====>>> namespace MidiGenerator
        {
            var changes = SettingsEditor.Edit(Channel, "Channel", 300);

            // Detect changes of interest.
            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "ChannelNumber":
                        ChannelChange?.Invoke(this, new() { ChannelNumberChange = true });
                        Channel.SendPatch();
                        break;
                    case "Patch": // or PatchPicker?
                        ChannelChange?.Invoke(this, new() { PatchChange = true });
                        Channel.SendPatch();
                        break;
                    case "PresetFile":
                        // Handled in property setter.
                        break;
                }
            }

            UpdateUi();
        }
        #endregion




        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            lblSolo.BackColor = _state == PlayState.Solo ? _selColor :  _unselColor;
            lblMute.BackColor = _state == PlayState.Mute ? _selColor :  _unselColor;

            // General.
            lblChannelInfo.Text = $"Ch{Channel.ChannelNumber}";
            toolTip.SetToolTip(this, ToString());
        }


        /// <summary>
        /// Read me.
         /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Ch:{Channel.ChannelNumber} Patch:{Channel.GetPatchName()}({Channel.Patch})";
        }
    }
}
