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
    /// <summary>Application level error. Above lua level.</summary>
    public class AppException(string message) : Exception(message) { }


    public class Defs
    {
        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;
    }

    #region Events
    /// <summary>Notify host of user clicks.</summary>
    public class NoteEventArgs : EventArgs
    {
        /// <summary>The note number to play.</summary>
        [Required]
        public int Note { get; set; }

        /// <summary>0 to 127.</summary>
        [Required]
        public int Velocity { get; set; }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Note:{MusicDefinitions.NoteNumberToName(Note)}({Note}):{Velocity}";
        }
    }

    /// <summary>Notify host of user clicks.</summary>
    public class ControllerEventArgs : EventArgs
    {
        /// <summary>Specific controller id.</summary>
        [Required]
        public int ControllerId { get; set; }

        /// <summary>Payload.</summary>
        [Required]
        public int Value { get; set; }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"ControllerId:{MidiDefs.TheDefs.GetControllerName(ControllerId)}({ControllerId}):{Value}";
        }
    }

    /// <summary>Notify host of user clicks.</summary>
    public class ChannelEventArgs : EventArgs
    {
        public bool PatchChange { get; set; } = false;
        public bool ChannelNumberChange { get; set; } = false;
        public bool PresetFileChange { get; set; } = false;
    }
    #endregion



    // from MidiLib
    /// <summary>
    /// Midi (real or sim) has received something. It's up to the client to make sense of it.
    /// Property value of -1 indicates invalid or not pertinent e.g a controller event doesn't have velocity.
    /// </summary>
    public class InputReceiveEventArgs : EventArgs
    {
        /// <summary>Channel number 1-based. Required.</summary>
        public int Channel { get; set; } = 0;

        /// <summary>The note number to play. NoteOn/Off only.</summary>
        public int Note { get; set; } = -1;

        /// <summary>Specific controller id.</summary>
        public int Controller { get; set; } = -1;

        /// <summary>For Note = velocity. For controller = payload.</summary>
        public int Value { get; set; } = -1;

        /// <summary>Something to tell the client.</summary>
        public string ErrorInfo { get; set; } = "";

        /// <summary>Special controller id to carry pitch info.</summary>
        public const int PITCH_CONTROL = 1000;

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            StringBuilder sb = new($"Channel:{Channel} ");

            if (ErrorInfo != "")
            {
                sb.Append($"Error:{ErrorInfo} ");
            }
            else
            {
                sb.Append($"Channel:{Channel} Note:{Note} Controller:{Controller} Value:{Value}");
            }

            return sb.ToString();
        }
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
    }
}
