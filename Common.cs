using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel.DataAnnotations;
using Ephemera.NBagOfTricks;
using System.Runtime.CompilerServices;


namespace Ephemera.MidiLibLite
{
    /// <summary>Library error.</summary>
    public class MidiLibException(string message) : Exception(message) { }

    public class Defs
    {
        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;
    }

    public class Utils
    {
        // Load a standard midi def file.
        public static Dictionary<int, string> LoadDefs(string fn)
        {
            Dictionary<int, string> res = [];

            var ir = new IniReader(fn);

            var defs = ir.Contents["midi_defs"];

            defs.Values.ForEach(kv =>
            {
                int index = int.Parse(kv.Key); // can throw
                if (index < 0 || index > MidiDefs.MAX_MIDI) { throw new InvalidOperationException($"Invalid def file {fn}"); }
                res[index] = kv.Value.Length > 0 ? kv.Value : "";
            });

            return res;
        }

        /// <summary>
        /// Input from internal non-midi device. Doesn't throw.
        /// </summary>
        void InjectMidiInEvent(string devName, int channel, int noteNum, int velocity)
        {
            //var input = _inputs.FirstOrDefault(o => o.DeviceName == devName);

            //if (input is not null)
            //{
            //    velocity = MathUtils.Constrain(velocity, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);
            //    NoteEvent nevt = velocity > 0 ?
            //        new NAudio.Midi.NoteOnEvent(0, channel, noteNum, velocity, 0) :
            //        new NoteEvent(0, channel, MidiCommandCode.NoteOff, noteNum, 0);
            //    Midi_ReceiveEvent(input, nevt);
            //}
            //else do I care?
        }

        /// <summary>
        /// Read the lua midi definitions for internal consumption.
        /// </summary>
        void ReadMidiDefs() // TODO_defs
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

                    //switch (parts[0])
                    //{
                    //    case "instrument": MidiDefs.Instruments.Add(int.Parse(parts[2]), parts[1]); break;
                    //    case "drum": MidiDefs.Drums.Add(int.Parse(parts[2]), parts[1]); break;
                    //    case "controller": MidiDefs.Controllers.Add(int.Parse(parts[2]), parts[1]); break;
                    //    case "kit": MidiDefs.DrumKits.Add(int.Parse(parts[2]), parts[1]); break;
                    //}
                }
            }
        }

        /// <summary>
        /// Execute a chunk of lua code. Fixes up lua path and handles errors.
        /// </summary>
        /// <param name="scode"></param>
        /// <returns></returns>
        (int ecode, string sres) ExecuteLuaChunk(List<string> scode) // TODO_defs
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
