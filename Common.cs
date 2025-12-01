using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel.DataAnnotations;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    public class Defs
    {
        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;
    }
 
    /// <summary>Library error.</summary>
    public class MidiLibException(string message) : Exception(message) { }

    #region Internal event definitions TODO2 own home?
    public class BaseEvent
    {
        /// <summary>Channel 1-NUM_CHANNELS.</summary>
        public int Channel { get; init; }

        /// <summary>Something to tell the client.</summary>
        public string ErrorInfo { get; set; } = "";
    }

    public class NoteOn : BaseEvent
    {
        /// <summary>The note number to play.</summary>
        public int Note { get; init; }

        /// <summary>0 to 127.</summary>
        public int Velocity { get; init; }

        public NoteOn(int channel, int note, int velocity)
        {
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

    public class NoteOff : BaseEvent
    {
        /// <summary>The note number to play.</summary>
        public int Note { get; init; }

        public NoteOff(int channel, int note)
        {
            Channel = channel;
            Note  = note;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"NoteOff:{MusicDefinitions.NoteNumberToName(Note)}({Note})";
        }
    }

    public class Controller : BaseEvent
    {
        /// <summary>Specific controller id.</summary>
        public int ControllerId { get; init; }

        /// <summary>Payload.</summary>
        public int Value { get; init; }

        public Controller(int channel, int controllerId, int value)
        {
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

    public class Patch : BaseEvent
    {
        /// <summary>Payload.</summary>
        public int Value { get; init; }

        public Patch(int channel, int value)
        {
            Channel = channel;
            Value = value;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Patch:{Value} TODO1 text get from channel";
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
