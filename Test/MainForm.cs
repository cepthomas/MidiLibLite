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
            sldVolume.DrawColor = _controlColor;
            sldVolume.Minimum = 0.0;
            sldVolume.Maximum = Defs.MAX_VOLUME;
            sldVolume.Resolution = Defs.MAX_VOLUME / 50;
            sldVolume.Value = Defs.DEFAULT_VOLUME;
            sldVolume.Label = "master volume";

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
                //DemoScriptApp();
                DemoStandardApp();
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
            /// 1 - create channels
            var chanout1 = _mgr.OpenMidiOutput(OUTDEV1, 1, "channel 1!", 0);  // TODO_defs patch by name => "AcousticGrandPiano"
            var chanout2 = _mgr.OpenMidiOutput(OUTDEV1, 2, "channel 2!", 12); // => ???);
            chanout1.UpdatePresets();
            chanout2.UpdatePresets();

            /// 2 - configure controls and bind channels
            var rend = new CustomRenderer() { ChannelHandle = chanout1.Handle };
            rend.SendMidi += Rend_SendMidi;
            cch1.UserRenderer = rend;
            InitControl(cch1);
            cch1.BoundChannel = chanout1;

            rend = new CustomRenderer() { ChannelHandle = chanout2.Handle };
            rend.SendMidi += Rend_SendMidi;
            cch2.UserRenderer = rend;
            InitControl(cch2);
            cch2.BoundChannel = chanout2;

            /// 3 - do work
            // ????
        }

        /// <summary>
        /// App driven by a script - as Nebulua/Nebulator. Creates channels and controls dynamically.
        /// </summary>
        void DemoScriptApp() // TODO1
        {
            ///// 0 - pre-steps - only for this demo
            cch1.Hide();
            cch2.Hide();
            //cctrl1.Hide();
            //cctrl2.Hide();

            ///// 1 - create all devices
            //_mgr.CreateDevices();

            ///// 2 - create all channels - script api calls like:
            // local hnd_ccin = api.open_midi_input("loopMIDI Port 1", 1, "my input")
            // local hnd_keys = api.open_midi_output("loopMIDI Port 2", 1, "keys", inst.AcousticGrandPiano)
            // local hnd_synth = api.open_midi_output("loopMIDI Port 2", 3, "synth", inst.Lead1Square)
            //List<OutputChannel> channels = [];
            var ch1 = _mgr.OpenMidiInput(INDEV, 1, "my input");
            var chan1 = _mgr.OpenMidiOutput(OUTDEV1, 1, "keys", 0); // => AcousticGrandPiano);
            //channels.Add(chan1);
            var chan2 = _mgr.OpenMidiOutput(OUTDEV1, 10, "drums", 32); // => kit.Jazz);
            //channels.Add(chan2);

            List<OutputChannel> channels = new() { chan1, chan2 };
            List<ChannelControl> controls = new();// { cch1, cch2 };

            ///// 3 - create a control for each channel and bind object
            //DestroyControls();
            int x = sldVolume.Left;
            int y = sldVolume.Bottom + 10;

            channels.ForEach(chan =>
            {
                //var cc = new ChannelControl(chan);// { BoundChannel = chan };
                //var cc = new ChannelControl() { BoundChannel = chan };
                var cc = new ChannelControl();
                InitControl(cc);
                cc.Location = new(x, y);
                cc.Size = new(320, 175);
                y += cc.Size.Height + 8;


                Controls.Add(cc);
                controls.Add(cc);


                chan.UpdatePresets();
            });

            ///// 4 - do work
            // call script api functions
            // api.send_midi_note(hnd_strings, note_num, volume)
            // api.send_midi_controller(hnd_synth, ctrl.Pan, 90)
            //void SendMidiNote(ChannelHandle ch, int note_num, double volume)
            //{
            //}
            //void SendMidiController(ChannelHandle ch, int controller_id, int value)
            //{
            //}

            // callbacks from script
            // function receive_midi_note(chan_hnd, note_num, volume)
            // function receive_midi_controller(chan_hnd, controller, value)
            //void ReceiveMidiNote(ChannelHandle ch, int note_num, double volume)
            //{
            //}
            //void ReceiveMidiController(ChannelHandle ch, int controller_id, int value)
            //{
            //}
        }



        //////////////////////////////// script api functions //////////////////////////////////
        // api.send_midi_note(hnd_strings, note_num, volume)
        // api.send_midi_controller(hnd_synth, ctrl.Pan, 90)
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

        void SendMidiController(ChannelHandle chnd, int controller_id, int value)
        {
            if (controller_id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(controller_id)); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }

            var ch = _mgr.GetOutputChannel(chnd);
            ch.Device.Send(new Controller(chnd.ChannelNumber, controller_id, value));
        }

        // TODO1 callbacks from script
        // function receive_midi_note(chan_hnd, note_num, volume)
        // function receive_midi_controller(chan_hnd, controller, value)
        void ReceiveMidiNote(ChannelHandle chnd, int note_num, double volume)
        {
        }
        void ReceiveMidiController(ChannelHandle chnd, int controller_id, int value)
        {
        }





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
        /// Common init control.
        /// </summary>
        void InitControl(ChannelControl cc)
        {
            cc.BorderStyle = BorderStyle.FixedSingle;
            cc.ControlColor = _controlColor;
            cc.SelectedColor = _selectedColor;
            cc.Volume = Defs.DEFAULT_VOLUME;
            cc.ChannelChange += ChannelControl_ChannelChange;
            cc.SendMidi += ChannelControl_SendMidi
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
