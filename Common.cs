using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    /// <summary>Library error.</summary>
    public class MidiLibException(string message) : Exception(message) { }

    /// <summary>User selection options.</summary>
    public enum SnapType { Bar, Beat, Sub }

    public class Stuff
    {
        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;


        // from MidiSettings TODO1 somewhere else?
        public static int DefaultTempo { get; set; } = 100;
        public static SnapType Snap { get; set; } = SnapType.Beat;

    }




    public class Utils // TODO2 home?
    {
        /// <summary>
        /// Convert a midi dictionary into ordered list of strings.
        /// </summary>
        /// <param name="source">The dictionary to process</param>
        /// <param name="addKey">Add the index number to the entry</param>
        /// <param name="fill">Add mising midi values</param>
        /// <returns></returns>
        public static List<string> CreateOrderedMidiList(Dictionary<int, string> source, bool addKey, bool fill)
        {
            List<string> res = [];

            for (int i = 0; i < MidiDefs.MAX_MIDI; i++)
            {
                if (source.ContainsKey(i))
                {
                    res.Add(addKey ? $"{i:000} {source[i]}" : $"{source[i]}");
                }
                else if (fill)
                {
                    res.Add($"{i:000}");
                }
            }


            //if (fill)
            //{
            //    for (int i = 0; i < MidiDefs.MAX_MIDI; i++)
            //    {
            //        if (source.ContainsKey(i))
            //        {
            //            if (addKey)
            //            {
            //                res.Add($"{i:000} {source[i]}");
            //            }
            //            else
            //            {
            //                res.Add($"{source[i]}");
            //            }
            //        }
            //        else
            //        {
            //            res.Add($"{i:000}");
            //        }
            //    }
            //}
            //else
            //{
            //    var orderedKeys = source.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
            //    for (int i = 0; i < orderedKeys.Count; i++)
            //    {
            //        if (addKey)
            //        {
            //            res.Add($"{i:000} {orderedKeys[i]}");
            //        }
            //        else
            //        {
            //            res.Add($"{orderedKeys[i]}");
            //        }
            //    }
            //}

            //IEnumerable<string> orderedValues = source.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value);
            //var instsList = orderedValues.ToList();


            return res;
        }
    }
}
