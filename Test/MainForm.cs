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
using System.IO;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLibLite;
using System.Runtime.CompilerServices;


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
        readonly Color _drawColor = Color.Aquamarine;

        /// <summary>Cosmetics.</summary>
        readonly Color _selectedColor = Color.Yellow;

        /// <summary>The boss.</summary>
        readonly Manager _mgr = new();

        const string ERROR = "ERR";
        const string WARN = "WRN";
        const string INFO = "INF";
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
            sldVolume.DrawColor = _drawColor;
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
        /// A standard app where controls/channels are defined in VS designer.
        /// </summary>
        void DemoStandardApp()
        {
            ///// 1 - configure controls
            InitControl(cch1);
            InitControl(cch2);
            InitControl(cctrl1);
            InitControl(cctrl2);

            ///// 2 - create and bind all channels - explicit
            MidiOutputDevice device = new("VirtualMIDISynth #1"); // "Microsoft GS Wavetable Synth"
            var chan1 = new OutputChannel(device, 1, "cch1 !!!");
            var chan2 = new OutputChannel(device, 2, "cch2 !!!");
            cch1.BoundChannel = chan1;
            cch2.BoundChannel = chan2;

            ///// 3 - configure other stuff
            cctrl1.Info = new() { DeviceId = device.Id, ChannelNumber = 1, ControllerId = 77, ControllerValue = 50 };
            cctrl2.Info = new() { DeviceId = device.Id, ChannelNumber = 2, ControllerId = 88, ControllerValue = 60 };

            ///// 4 - do work
            // ????
        }

        /// <summary>
        /// App driven by a script - as Nebulua/Nebulator.
        /// </summary>
        void DemoScriptApp()
        {
            ///// 0 - pre-steps
            cch1.Hide();
            cch2.Hide();
            cctrl1.Hide();
            cctrl2.Hide();

            ///// 1 - create all devices
            _mgr.CreateDevices();

            ///// 2 - create all channels - script api calls like:
            // local hnd_ccin = api.open_midi_input("loopMIDI Port 1", 1, "my input")
            // local hnd_keys = api.open_midi_output("loopMIDI Port 2", 1, "keys", inst.AcousticGrandPiano)
            // local hnd_synth = api.open_midi_output("loopMIDI Port 2", 3, "synth", inst.Lead1Square)
            var ch1 = _mgr.OpenMidiInput("loopMIDI Port 1", 1, "my input");
            var ch2 = _mgr.OpenMidiOutput("Microsoft GS Wavetable Synth", 1, "keys", 0); // => AcousticGrandPiano);
            var ch3 = _mgr.OpenMidiOutput("Microsoft GS Wavetable Synth", 10, "drums", 32); // => kit.Jazz);

            ///// 3 - create a channel control for each output channel and bind object
            DestroyControls();
            int x = sldVolume.Left;
            int y = sldVolume.Bottom + 10;

            OutputChannel[] chs = [ ch2, ch3 ];
            chs.ForEach(ch =>
            {
                var cc = new CustomChannelControl() { BoundChannel = ch };
                InitControl(cc);
                cc.Location = new(x, y);
                cc.Size = new(320, 175);
                y += cc.Size.Height + 8;

                ch.UpdatePresets();
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
        /// Init control.
        /// </summary>
        void InitControl(CustomChannelControl cc)
        {
            cc.BorderStyle = BorderStyle.FixedSingle;
            cc.DrawColor = _drawColor;
            cc.SelectedColor = _selectedColor;
            cc.Volume = Defs.DEFAULT_VOLUME;
            cc.ChannelChange += ChannelControl_ChannelChange;
            cc.SendMidi += ChannelControl_MidiSend;

            if (!Controls.Contains(cc))
            {
                Controls.Add(cc);
            }
        }

        /// <summary>
        /// Init control.
        /// </summary>
        void InitControl(ControllerControl cc)
        {
            cc.BorderStyle = BorderStyle.FixedSingle;
            cc.DrawColor = _drawColor;
            cc.SelectedColor = _selectedColor;
            cc.SendMidi += ControllerControl_MidiSend;

            if (!Controls.Contains(cc))
            {
                Controls.Add(cc);
            }
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
        /// UI clicked something -> send some midi.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_MidiSend(object? sender, BaseMidiEvent e)
        {
            var cc = sender as ChannelControl;
            var channel = cc!.BoundChannel!;

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
                Tell(INFO, $"PatchChange");
                channel.Device.Send(new Patch(channel.ChannelNumber, channel.Patch));
            }

            if (e.PresetFileChange)
            {
                Tell(INFO, $"PresetFileChange");
                channel.UpdatePresets();
            }
        }

        /// <summary>
        /// UI clicked something -> send some midi.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ControllerControl_MidiSend(object? sender, BaseMidiEvent e)
        {
            var cc = sender as ControllerControl;
            var dev = _mgr.GetOutputDevice(cc.Info.DeviceId);

            //if (channel.Enable)
            {
                dev.Send(e);
            }
        }

        /// <summary>
        /// Something arrived from a midi device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_InputReceive(object? sender, BaseMidiEvent e)
        {
            Tell(INFO, $"Received: {e}");
        }
        #endregion

        #region Misc internals
        /// <summary>Tell me something good.</summary>
        /// <param name="s">What</param>
        void Tell(string cat, string s, [CallerFilePath] string file = "", [CallerLineNumber] int line = -1)
        {
            var fn = Path.GetFileName(file);
            txtViewer.AppendLine($"{cat} {fn}({line}) {s}");
        }
        #endregion




#if LEFTOVERS_TODO1
        /// <summary>
        /// Input from internal non-midi device. Doesn't throw.
        /// </summary>
        void InjectMidiInEvent(string devName, int channel, int noteNum, int velocity)
        {
            var input = _inputs.FirstOrDefault(o => o.DeviceName == devName);

            if (input is not null)
            {
                velocity = MathUtils.Constrain(velocity, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);
                NoteEvent nevt = velocity > 0 ?
                    new NAudio.Midi.NoteOnEvent(0, channel, noteNum, velocity, 0) :
                    new NoteEvent(0, channel, MidiCommandCode.NoteOff, noteNum, 0);
                Midi_ReceiveEvent(input, nevt);
            }
            //else do I care?
        }

        /// <summary>
        /// Create string suitable for logging. Doesn't throw.
        /// </summary>
        /// <param name="evt">Midi event to format.</param>
        /// <param name="tick">Current tick.</param>
        /// <param name="chanHnd">Channel info.</param>
        /// <returns>Suitable string.</returns>
        string FormatMidiEvent(MidiEvent evt, int tick, int chanHnd)
        {
            // Common part.
            ChannelHandle ch = new(chanHnd);

            string s = $"{tick:00000} {MusicTime.Format(tick)} {evt.CommandCode} Dev:{ch.DeviceId} Ch:{ch.ChannelNumber} ";

            switch (evt)
            {
                case NoteEvent e:
                    var snote = ch.ChannelNumber == 10 || ch.ChannelNumber == 16 ?
                        $"DRUM_{e.NoteNumber}" :
                        MusicDefinitions.NoteNumberToName(e.NoteNumber);
                    s = $"{s} {e.NoteNumber}:{snote} Vel:{e.Velocity}";
                    break;

                case ControlChangeEvent e:
                    var sctl = Enum.IsDefined(e.Controller) ? e.Controller.ToString() : $"CTLR_{e.Controller}";
                    s = $"{s} {(int)e.Controller}:{sctl} Val:{e.ControllerValue}";
                    break;

                default: // Ignore others for now.
                    break;
            }

            return s;
        }


        /// <summary>
        /// Read the lua midi definitions for internal consumption.
        /// </summary>
        void ReadMidiDefs()
        {
            //var srcDir = MiscUtils.GetSourcePath().Replace("\\", "/");

            List<string> s = [
                "local mid = require('midi_defs')",
                "for _,v in ipairs(mid.gen_list()) do print(v) end"
                ];

            var (ecode, sres) = ExecuteLuaChunk(s);

            if (ecode == 0)
            {
                foreach (var line in sres.SplitByToken(Environment.NewLine))
                {
                    var parts = line.SplitByToken(",");

                    switch (parts[0])
                    {
                        case "instrument": MidiDefs.Instruments.Add(int.Parse(parts[2]), parts[1]); break;
                        case "drum": MidiDefs.Drums.Add(int.Parse(parts[2]), parts[1]); break;
                        case "controller": MidiDefs.Controllers.Add(int.Parse(parts[2]), parts[1]); break;
                        case "kit": MidiDefs.DrumKits.Add(int.Parse(parts[2]), parts[1]); break;
                    }
                }
            }
        }


        /// <summary>
        /// Execute a chunk of lua code. Fixes up lua path and handles errors.
        /// </summary>
        /// <param name="scode"></param>
        /// <returns></returns>
        (int ecode, string sres) ExecuteLuaChunk(List<string> scode)
        {
            var srcDir = MiscUtils.GetSourcePath().Replace("\\", "/");
            var luaPath = $"{srcDir}/LBOT/?.lua;{srcDir}/lua/?.lua;;";
            scode.Insert(0, $"package.path = '{luaPath}' .. package.path");

            var (ecode, sret) = Tools.ExecuteLuaCode(string.Join(Environment.NewLine, scode));

            if (ecode != 0)
            {
                // Command failed. Capture everything useful.
                List<string> lserr = [];
                lserr.Add($"=== code: {ecode}");
                lserr.Add($"=== stderr:");
                lserr.Add($"{sret}");

                // _loggerApp.Warn(string.Join(Environment.NewLine, lserr));
            }
            return (ecode, sret);
        }
#endif

    }
}
