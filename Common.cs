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

    #region Internal event definitions TODO2 different home?
    public class BaseMidiEvent
    {
        /// <summary>Channel 1-NUM_CHANNELS.</summary>
        [Range(1, MidiDefs.NUM_CHANNELS)]
        public int Channel { get; init; }

        /// <summary>Something to tell the client.</summary>
        public string ErrorInfo { get; set; } = "";

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"BaseMidiEvent:{Channel} {ErrorInfo}";
        }
    }

    public class NoteOn : BaseMidiEvent
    {
        /// <summary>The note number to play.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Note { get; init; }

        /// <summary>0 to 127.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Velocity { get; init; }

        public NoteOn(int channel, int note, int velocity)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(note)); }
            if (velocity is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(velocity)); }

            Channel = channel;
            Note  = note;
            Velocity = velocity;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"NoteOn:{MusicDefinitions.NoteNumberToName(Note)}({Note}):{Velocity}";
        }
    }

    public class NoteOff : BaseMidiEvent
    {
        /// <summary>The note number to play.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Note { get; init; }

        public NoteOff(int channel, int note)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(note)); }

            Channel = channel;
            Note  = note;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"NoteOff:{MusicDefinitions.NoteNumberToName(Note)}({Note})";
        }
    }

    public class Controller : BaseMidiEvent
    {
        /// <summary>Specific controller id.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int ControllerId { get; init; }

        /// <summary>Payload.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Value { get; init; }

        public Controller(int channel, int controllerId, int value)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (controllerId is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(controllerId)); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }

            Channel = channel;
            ControllerId = controllerId;
            Value = value;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"ControllerId:{MidiDefs.TheDefs.GetControllerName(ControllerId)}({ControllerId}):{Value}";
        }
    }

    public class Patch : BaseMidiEvent
    {
        /// <summary>Payload.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Value { get; init; }

        public Patch(int channel, int value)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }

            Channel = channel;
            Value = value;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Patch:{Value} TODO_defs text get from channel";
        }
    }
    #endregion


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
    }
}
