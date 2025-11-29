using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;

using Ephemera.MidiLibLite;



namespace Ephemera.MidiLibLite.Test
{
    public class ToAdd // maybe?
    {


//////////////////////////////////// from Nebulua /////////////////////////////////////
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
                    new NoteOnEvent(0, channel, noteNum, velocity, 0) :
                    new NoteEvent(0, channel, MidiCommandCode.NoteOff, noteNum, 0);
                Midi_ReceiveEvent(input, nevt);
            }
            //else do I care?
        }

        /// <summary>
        /// Stop all midi. Doesn't throw.
        /// </summary>
        void KillAll()
        {
            _outputs.ForEach(op => op.Channels.ForEach(ch => op.Send(new ControlChangeEvent(0, ch.Key, MidiController.AllNotesOff, 0))));

            // Hard reset.
            CurrentState = ExecState.Idle;
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
        /// Clean up devices. Doesn't throw.
        /// </summary>
        void ResetDevices()
        {
            _inputs.ForEach(d => d.Dispose());
            _inputs.Clear();
            _outputs.ForEach(d => d.Dispose());
            _outputs.Clear();
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
    }
}