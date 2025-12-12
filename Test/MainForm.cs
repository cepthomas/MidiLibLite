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
using System.ComponentModel.DataAnnotations;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLibLite;


//?? TODO2 generate lua versions of mididefs and musicdefs from ini files?


namespace Ephemera.MidiLibLite.Test
{
    public partial class MainForm : Form
    {
        #region Fields - app
        /// <summary>All the channel controls.</summary>
        readonly List<ChannelControl> _channelControls = [];

        /// <summary>Where to put things.</summary>
        readonly string _outPath = @"..\..\out";

        /// <summary>Cosmetics.</summ ary>
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
                //DemoScriptApp();
                //DemoStandardApp();
            }
            catch (Exception ex)
            {
                Tell(ERROR, ex.Message);
            }
            // catch (MidiLibException ex)
            // catch (AppException ex)

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

        //-------------------------------------------------------------------------------//
        /// <summary>Test property editing.</summary>
        void Edit_Click(object sender, EventArgs e)
        {
            //PropertyEdit();

            ChannelEdit();
        }

        #region Do some work

        /// <summary>TypeEditor test class</summary>
        [Serializable]
        public class TypeEditorTestData
        {
            /// <summary>Device name.</summary>
            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            public string DeviceName { get; set; } = "";

            /// <summary>Channel name - optional.</summary>
            public string ChannelName { get; set; } = "";

            /// <summary>Actual 1-based midi channel number.</summary>
            [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
            [Range(1, MidiDefs.NUM_CHANNELS)]
            public int ChannelNumber { get; set; } = 1;

            /// <summary>Actual 1-based midi channel number.</summary>
            [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
            [Range(0, MidiDefs.MAX_MIDI)]
            public int SomeOtherMidi { get; set; } = 0;

            /// <summary>Override default instrument presets.</summary>
            [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            public string AliasFile { get; set; } = "";

            /// <summary>Current instrument/patch number.</summary>
            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            [TypeConverter(typeof(GenericConverter))]
            [Range(0, MidiDefs.MAX_MIDI)]
            public int Patch { get; set; } = 0;
        }


        //-------------------------------------------------------------------------------//
        /// <summary>Test property editing.</summary>
        void ChannelEdit()
        {
            // Dummy channel to satisfy designer. Will be overwritten by the real one.
            var dev = new NullOutputDevice("DUMMY_DEVICE");
            //BoundChannel = new OutputChannel(dev, 9);

            OutputChannel ch = new(dev, 3)
            {
                ChannelName = "booga-booga",
                ControllerId = 45,
                ControllerValue = 60
            };

            // should send midi
            ch.Patch = 77;
            if (dev.CollectedEvents.Count != 1) Tell(ERROR, "FAIL");

            // should change the instruments list
            var inst1 = ch.Instruments;
            if (inst1.Count != 128) Tell(ERROR, "FAIL");

            ch.AliasFile = @"C:\Dev\Libs\MidiLibLite\_def_files\exp_defs.ini";

            var inst2 = ch.Instruments;
            if (inst2.Count != 66) Tell(ERROR, "FAIL");

            Tell(INFO, "DONE");
        }

        //-------------------------------------------------------------------------------//
        /// <summary>Test property editing.</summary>
        void PropertyEdit()
        {
            TypeEditorTestData td = new()
            {
                Patch = 77,
                ChannelName = "booga-booga",
                ChannelNumber = 5,
                AliasFile = @"C:\Dev\Libs\MidiLibLite\_def_files\exp_defs.ini",
                DeviceName = "pdev1",
                SomeOtherMidi = 88
            };

            // Set up options.
            var insts = MidiDefs.TheDefs.GetDefaultInstrumentDefs();
            IEnumerable<string> orderedValues = insts.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value);
            var instsList = orderedValues.ToList();

            GenericListTypeEditor.SetOptions("DeviceName", MidiOutputDevice.GetAvailableDevices());
            GenericListTypeEditor.SetOptions("Patch", instsList);

            var changes = SettingsEditor.Edit(td, "TESTOMATIC", 300);
            changes.ForEach(s => Tell(INFO, $"Editor changed {s}"));
        }

        //-------------------------------------------------------------------------------//
        /// <summary>Test def file loading.</summary>
        void TestDefs_Click(object sender, EventArgs e)
        {
            string fn = @"C:\Dev\Libs\MidiLibLite\_def_files\gm_defs.ini"; // TODO1 these files locations

            // key is section name, value is line
            Dictionary<string, List<string>> res = [];
            var ir = new IniReader(fn);

            ir.Contents.ForEach(ic =>
            {
                Tell(INFO, $"section:{ic.Key} => {ic.Value.Values.Count}");
            });
        }

        //-------------------------------------------------------------------------------//
        /// <summary>Test dynamic UI creation.</summary>
        void Demo_Click(object sender, EventArgs e)
        {
            try
            {
                DemoScriptApp();
            }
            catch (Exception ex)
            {
                Tell(ERROR, ex.Message);
            }
        }

        //-------------------------------------------------------------------------------//
        /// <summary>A standard app where controls are defined in VS designer.</summary>
        void DemoStandardApp()
        {
            // Create channels and initialize controls.
            var chan_out1 = _mgr.OpenMidiOutput(OUTDEV1, 1, "channel 1!", 0);
            var chan_out2 = _mgr.OpenMidiOutput(OUTDEV1, 2, "channel 2!", 12);

            List<(OutputChannel, ChannelControl)> channels = [(chan_out1, ch_ctrl1), (chan_out2, ch_ctrl2)];
            channels.ForEach(ch =>
            {
                ch.Item2.BorderStyle = BorderStyle.FixedSingle;
                ch.Item2.ControlColor = _controlColor;
                ch.Item2.SelectedColor = _selectedColor;
                ch.Item2.Volume = Defs.DEFAULT_VOLUME;
                ch.Item2.ChannelChange += ChannelControl_ChannelChange;
                ch.Item2.SendMidi += ChannelControl_SendMidi;
                ch.Item2.BoundChannel = ch.Item1;

                var rend = new CustomRenderer() { ChannelHandle = ch.Item1.Handle };
                rend.SendMidi += ChannelControl_SendMidi;
                ch.Item2.UserRenderer = new CustomRenderer() { ChannelHandle = ch.Item1.Handle };
            });

            ///// 3 - do work
            // ????
        }

        //-------------------------------------------------------------------------------//
        /// <summary>App driven by a script - as Nebulua/Nebulator. Creates channels and controls dynamically.</summary>
        void DemoScriptApp()
        {
            ///// 0 - pre-steps - only for this demo
            ch_ctrl1.Hide();
            ch_ctrl2.Hide();

            ///// 1 - create all channels
            var chan_out1 = _mgr.OpenMidiOutput(OUTDEV1, 1, "keys", 0);
            var chan_out2 = _mgr.OpenMidiOutput(OUTDEV1, 10, "drums", 32);
            var chan_in1 = _mgr.OpenMidiInput(INDEV, 1, "my input");

            ///// 2 - create a control for each channel and bind object
            int x = sldMasterVolume.Left;
            int y = sldMasterVolume.Bottom + 10;

            List<OutputChannel> channels = [chan_out1, chan_out2];
            channels.ForEach(chan =>
            {
                var rend = new CustomRenderer() { ChannelHandle = chan.Handle };
                rend.SendMidi += ChannelControl_SendMidi;

                var ctrl = new ChannelControl()
                {
                    //                    Name = $"Control for {chan.Config.ChannelName}",
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
        #endregion

        #region Script api functions
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
        /// UI clicked something -> send some midi. Works for different sources.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_SendMidi(object? sender, BaseMidiEvent e)
        {
            var channel = sender switch
            {
                ChannelControl => (sender as ChannelControl)!.BoundChannel,
                CustomRenderer => _mgr.GetOutputChannel((sender as CustomRenderer)!.ChannelHandle),
                _ => null // should never happen
            };

            if (channel is not null && channel.Enable)
            {
                Tell(INFO, $"Channel send [{e}]");
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
                Tell(INFO, $"PatchChange [{channel.Patch}]");
                channel.Device.Send(new Patch(channel.ChannelNumber, channel.Patch));
            }

            if (e.AliasFileChange)
            {
                Tell(INFO, $"AliasFileChange [{channel.AliasFile}]");
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
