using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLibLite;
using Ephemera.MidiLib.Test;


namespace Ephemera.MidiLibLite.Test
{
    public partial class MainForm : Form
    {
        #region Fields - app
        /// <summary>All the channel controls.</summary>
        readonly List<ChannelControl> _channelControls = [];

        /// <summary>Where to put things.</summary>
        readonly string _outPath = @"..\..\out";

        /// <summary>Cosmetics.</summary>
        readonly Color _controlColor = Color.Aquamarine;

        /// <summary>Cosmetics.</summary>
        readonly Color _selectedColor = Color.Yellow;

        /// <summary>The boss.</summary>
        readonly Manager _mgr = new();

        const string ERROR = "ERR";
        const string WARN = "WRN";
        const string INFO = "INF";

        const string INDEV = "loopMIDI Port 1";
        const string OUTDEV1 = "VirtualMIDISynth #1";
        const string OUTDEV2 = "Microsoft GS Wavetable Synth";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // Make sure out path exists.
            DirectoryInfo di = new(_outPath);
            di.Create();

            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.MatchText.Add(ERROR, Color.LightPink);
            txtViewer.MatchText.Add(WARN, Color.Plum);

            // Master volume.
            sldMasterVolume.DrawColor = _controlColor;
            sldMasterVolume.Minimum = 0.0;
            sldMasterVolume.Maximum = Defs.MAX_VOLUME;
            sldMasterVolume.Resolution = Defs.MAX_VOLUME / 50;
            sldMasterVolume.Value = Defs.DEFAULT_VOLUME;
            sldMasterVolume.Label = "master volume";

            // Hook up some simple UI handlers.
            btnKillMidi.Click += (_, __) => { _mgr.Kill(); };
            btnLogMidi.CheckedChanged += (_, __) => { };

            _mgr.InputReceive += Mgr_InputReceive;
        }

        /// <summary>
        /// Window is set up now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                DemoScriptApp();

                //DemoStandardApp();
            }
            catch (MidiLibException ex)
            {
                Tell(ERROR, ex.Message);
            }
            catch (AppException ex)
            {
                Tell(ERROR, ex.Message);
            }
            catch (Exception ex)
            {
                Tell(ERROR, ex.Message);
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            DestroyControls();
            _mgr.DestroyDevices();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        /// <summary>
        /// A standard app where controls are defined in VS designer.
        /// </summary>
        void DemoStandardApp()
        {
            // Create channels and initialize controls.
            var chan_out1 = _mgr.OpenMidiOutput(OUTDEV1, 1, "channel 1!", 0);  // TODO_defs patch by name => "AcousticGrandPiano"
            var chan_out2 = _mgr.OpenMidiOutput(OUTDEV1, 2, "channel 2!", 12); // => ???);

            List<(OutputChannel, ChannelControl)> channels = [(chan_out1, ch_ctrl1), (chan_out2, ch_ctrl2)];
            channels.ForEach(ch =>
            {
                ch.Item1.UpdatePresets();

                ch.Item2.BorderStyle = BorderStyle.FixedSingle;
                ch.Item2.ControlColor = _controlColor;
                ch.Item2.SelectedColor = _selectedColor;
                ch.Item2.Volume = Defs.DEFAULT_VOLUME;
                ch.Item2.ChannelChange += ChannelControl_ChannelChange;
                ch.Item2.SendMidi += ChannelControl_SendMidi;
                ch.Item2.BoundChannel = ch.Item1;

                var rend = new CustomRenderer() { ChannelHandle = ch.Item1.Handle };
                rend.SendMidi += Rend_SendMidi;
                ch.Item2.UserRenderer = new CustomRenderer() { ChannelHandle = ch.Item1.Handle };
            });


            //chan_out1.UpdatePresets();
            //ch_ctrl1.BorderStyle = BorderStyle.FixedSingle;
            //ch_ctrl1.ControlColor = _controlColor;
            //ch_ctrl1.SelectedColor = _selectedColor;
            //ch_ctrl1.Volume = Defs.DEFAULT_VOLUME;
            //ch_ctrl1.ChannelChange += ChannelControl_ChannelChange;
            //ch_ctrl1.SendMidi += ChannelControl_SendMidi;
            //ch_ctrl1.BoundChannel = chan_out1;
            //var rend = new CustomRenderer() { ChannelHandle = chan_out1.Handle };
            //rend.SendMidi += Rend_SendMidi;
            //ch_ctrl1.UserRenderer = new CustomRenderer() { ChannelHandle = chan_out1.Handle };;
           
            //chan_out2.UpdatePresets();
            //ch_ctrl2.BorderStyle = BorderStyle.FixedSingle;
            //ch_ctrl2.ControlColor = _controlColor;
            //ch_ctrl2.SelectedColor = _selectedColor;
            //ch_ctrl2.Volume = Defs.DEFAULT_VOLUME;
            //ch_ctrl2.ChannelChange += ChannelControl_ChannelChange;
            //ch_ctrl2.SendMidi += ChannelControl_SendMidi;
            //ch_ctrl2.BoundChannel = chan_out2;
            //rend = new CustomRenderer() { ChannelHandle = chan_out2.Handle };
            //rend.SendMidi += Rend_SendMidi;
            //ch_ctrl2.UserRenderer = rend;

            ///// 3 - do work
            // ????
        }

        /// <summary>
        /// App driven by a script - as Nebulua/Nebulator. Creates channels and controls dynamically.
        /// </summary>
        void DemoScriptApp()
        {
            ///// 0 - pre-steps - only for this demo
            ch_ctrl1.Hide();
            ch_ctrl2.Hide();

            ///// 1 - create all channels
            var chan_out1 = _mgr.OpenMidiOutput(OUTDEV1, 1, "keys", 0); // TODO_defs => AcousticGrandPiano);
            var chan_out2= _mgr.OpenMidiOutput(OUTDEV1, 10, "drums", 32); // => kit.Jazz);
            var chan_in1 = _mgr.OpenMidiInput(INDEV, 1, "my input");

            ///// 2 - create a control for each channel and bind object
            int x = sldMasterVolume.Left;
            int y = sldMasterVolume.Bottom + 10;

            List<OutputChannel> channels = [chan_out1, chan_out2];
            channels.ForEach(chan =>
            {
                chan.UpdatePresets();

                var rend = new CustomRenderer() { ChannelHandle = chan.Handle };
                rend.SendMidi += Rend_SendMidi;

                var ctrl = new ChannelControl()
                {
                    Name = $"Control for {chan.Config.ChannelName}",
                    BoundChannel = chan,
                    UserRenderer = rend,
                    Location = new(x, y),
                    BorderStyle = BorderStyle.FixedSingle,
                    ControlColor = _controlColor,
                    SelectedColor = _selectedColor,
                    Volume = Defs.DEFAULT_VOLUME,
                };
                ctrl.ChannelChange += ChannelControl_ChannelChange;
                ctrl.SendMidi += ChannelControl_SendMidi;

                Controls.Add(ctrl);
                x += ctrl.Width + 4; // Width is not valid until after previous statement.
            });

            ///// 3 - do work

            // create all channels - script api calls like:
            // local hnd_keys = api.open_midi_output("loopMIDI Port 2", 1, "keys", inst.AcousticGrandPiano)
            // local hnd_synth = api.open_midi_output("loopMIDI Port 2", 3, "synth", inst.Lead1Square)
            // local hnd_ccin = api.open_midi_input("loopMIDI Port 1", 1, "my input")

            // call script api functions
            // api.send_midi_note(hnd_strings, note_num, volume)
            // api.send_midi_controller(hnd_synth, ctrl.Pan, 90)

            // callbacks from script
            // function receive_midi_note(chan_hnd, note_num, volume)
            // function receive_midi_controller(chan_hnd, controller, value)
        }

        #region script api functions
        /// <summary>
        /// api.send_midi_note(hnd_strings, note_num, volume)
        /// </summary>
        /// <param name="chnd"></param>
        /// <param name="note_num"></param>
        /// <param name="volume"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        void SendMidiNote(ChannelHandle chnd, int note_num, double volume)
        {
            if (note_num is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(note_num)); }

            var ch = _mgr.GetOutputChannel(chnd);
            if (volume == 0.0)
            {
                ch.Device.Send(new NoteOff(chnd.ChannelNumber, note_num));
            }
            else
            {
                ch.Device.Send(new NoteOn(chnd.ChannelNumber, note_num, (int)MathUtils.Constrain(volume * MidiDefs.MAX_MIDI, 0, MidiDefs.MAX_MIDI)));
            }
        }

        /// <summary>
        /// api.send_midi_controller(hnd_synth, ctrl.Pan, 90)
        /// </summary>
        /// <param name="chnd"></param>
        /// <param name="controller_id"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        void SendMidiController(ChannelHandle chnd, int controller_id, int value)
        {
            if (controller_id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(controller_id)); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }

            var ch = _mgr.GetOutputChannel(chnd);
            ch.Device.Send(new Controller(chnd.ChannelNumber, controller_id, value));
        }

        /// <summary>
        /// Callback from script: function receive_midi_note(chan_hnd, note_num, volume)
        /// </summary>
        /// <param name="chnd"></param>
        /// <param name="note_num"></param>
        /// <param name="volume"></param>
        void ReceiveMidiNote(ChannelHandle chnd, int note_num, double volume)
        {
        }

        /// <summary>
        /// Callback from script: function receive_midi_controller(chan_hnd, controller, value)
        /// </summary>
        /// <param name="chnd"></param>
        /// <param name="controller_id"></param>
        /// <param name="value"></param>
        void ReceiveMidiController(ChannelHandle chnd, int controller_id, int value)
        {
        }
        #endregion

        #region Events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Rend_SendMidi(object? sender, BaseMidiEvent e)
        {
            var rend = sender as CustomRenderer;

            var channel = _mgr.GetOutputChannel(rend.ChannelHandle);

            Tell(INFO, $"Channel send [{e}]");

            if (channel.Enable)
            {
                channel.Device.Send(e);
            }
        }

        /// <summary>
        /// UI clicked something -> send some midi.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_SendMidi(object? sender, BaseMidiEvent e)
        {
            var cc = sender as ChannelControl;
            var channel = cc!.BoundChannel!;

            Tell(INFO, $"Channel send [{e}]");

            if (channel.Enable)
            {
                channel.Device.Send(e);
            }
        }

        /// <summary>
        /// UI clicked something -> configure channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_ChannelChange(object? sender, ChannelControl.ChannelChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            var channel = cc!.BoundChannel!;

            if (e.StateChange)
            {
                Tell(INFO, $"StateChange");

                // Update all channels.
                bool anySolo = _channelControls.Where(c => c.State == ChannelControl.ChannelState.Solo).Any();

                foreach (var cciter in _channelControls)
                {
                    bool enable = anySolo ?
                        cciter.State == ChannelControl.ChannelState.Solo :
                        cciter.State != ChannelControl.ChannelState.Mute;

                    channel.Enable = enable;
                    if (!enable)
                    {
                       // Kill just in case.
                        _mgr.Kill(channel);
                    }
                }
            }

            if (e.PatchChange)
            {
                Tell(INFO, $"PatchChange [{channel.Config.Patch}]");
                channel.Device.Send(new Patch(channel.Config.ChannelNumber, channel.Config.Patch));
            }

            if (e.PresetFileChange)
            {
                Tell(INFO, $"PresetFileChange [{channel.Config.PresetFile}]");
                channel.UpdatePresets();
            }
        }

        /// <summary>
        /// Something arrived from a midi device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_InputReceive(object? sender, BaseMidiEvent e)
        {
            Tell(INFO, $"Received [{e}]");
        }
        #endregion

        #region Misc internals
        /// <summary>
        /// Destroy controls.
        /// </summary>
        void DestroyControls()
        {
            _mgr.Kill();

            // Clean out our current elements.
            _channelControls.ForEach(c =>
            {
                Controls.Remove(c);
                c.Dispose();
            });
            _channelControls.Clear();
        }

        /// <summary>Tell me something good.</summary>
        /// <param name="s">What</param>
        void Tell(string cat, string s, [CallerFilePath] string file = "", [CallerLineNumber] int line = -1)
        {
            var fn = Path.GetFileName(file);
            txtViewer.AppendLine($"{cat} {fn}({line}) {s}");
        }
        #endregion
    }
}
