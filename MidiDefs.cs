using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    public class MidiDefs
    {
        #region Global collection
        /// <summary>The singleton instance.</summary>
        public static MidiDefs Instance { get { _instance ??= new MidiDefs(); return _instance; } }

        /// <summary>The singleton instance.</summary>
        static MidiDefs? _instance;
        #endregion

        #region Fields
        /// <summary>Midi constant.</summary>
        public const int MAX_MIDI = 127;

        /// <summary>Per device.</summary>
        public const int NUM_CHANNELS = 16;

        /// <summary>The normal drum channel.</summary>
        public const int DEFAULT_DRUM_CHANNEL = 10;

        /// <summary>All the GM instruments - default.</summary>
        readonly Dictionary<int, string> _instruments = [];

        /// <summary>All the GM controllers.</summary>
        readonly Dictionary<int, string> _controllerIds = [];

        /// <summary>Standard set plus unnamed ones.</summary>
        readonly Dictionary<int, string> _controllerIdsAll = [];

        /// <summary>All the GM drums.</summary>
        readonly Dictionary<int, string> _drums = [];

        /// <summary>All the GM drum kits.</summary>
        readonly Dictionary<int, string> _drumKits = [];
        #endregion

        #region Lifecycle
        /// <summary>Initialize some collections.</summary>
        MidiDefs()
        {
            string fn = Path.Combine(AppContext.BaseDirectory, "gm_defs.ini");

            if (!File.Exists(fn)) return; // sloppy assumption about DesignTime.

            // key is section name, value is line
            Dictionary<string, List<string>> res = [];
            var ir = new IniReader(fn);

            // Populate the defs.
            DoSection("instruments", _instruments);
            DoSection("controllers", _controllerIds);
            DoSection("drums", _drums);
            DoSection("drumkits", _drumKits);
            // // Special case - useful?
            // Enumerable.Range(0, MAX_MIDI).ForEach(c => _controllerIdsAll.Add(c, $"CTLR_{c}"));
            // DoSection("controllers", _controllerIdsAll);

            void DoSection(string section, Dictionary<int, string> target)
            {
                ir.Contents[section].Values.ForEach(kv =>
                {
                    int index = int.Parse(kv.Key); // can throw
                    if (index < 0 || index > MAX_MIDI) { throw new InvalidOperationException($"Invalid section {section} in file {fn}"); }
                    target[index] = kv.Value.Length > 0 ? kv.Value : "";
                });
            }
        }
        #endregion

        #region Public
        /// <summary>Default instruments.</summary>
        /// <returns></returns>
        public Dictionary<int, string> GetDefaultInstrumentDefs()
        {
            return _instruments;
        }

        /// <summary>All controllers.</summary>
        /// <param name="includeUnnamed"></param>
        /// <returns></returns>
        public Dictionary<int, string> GetControllerIdDefs(bool includeUnnamed)
        {
            return includeUnnamed ? _controllerIdsAll : _controllerIds;
        }

        /// <summary>
        /// Get drum name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The drum name or a fabricated one if unknown.</returns>
        public string GetDrumName(int which)
        {
            if (which is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(which)); }

            return _drums.TryGetValue(which, out string? value) ? value : $"DRUM_{which}";
        }

        /// <summary>
        /// Get controller name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The controller name or a fabricated one if unknown.</returns>
        public string GetControllerName(int which)
        {
            if (which is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(which)); }

            return _controllerIds.TryGetValue(which, out string? value) ? value : $"CTLR_{which}";
        }

        /// <summary>
        /// Get GM drum kit name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The drumkit name or a fabricated one if unknown.</returns>
        public string GetDrumKitName(int which)
        {
            if (which is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(which)); }

            return _drumKits.TryGetValue(which, out string? value) ? value : $"KIT_{which}";
        }

        /// <summary>
        /// Make content from the definitions.
        /// </summary>
        /// <returns>Content.</returns>
        public string GenMarkdown(string fn)
        {
            // key is section name, value is line
            Dictionary<string, List<string>> res = [];
            var ir = new IniReader(fn);

            List<string> ls = [];
            ls.Add("# Midi GM Instruments");
            ls.Add("|Instrument          | Number|");
            ls.Add("|----------          | ------|");
            ir.Contents["instruments"].Values.ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            ls.Add("# Midi GM Controllers");
            ls.Add("- Undefined: 3, 9, 14-15, 20-31, 85-90, 102-119");
            ls.Add("- For most controllers marked on/off, on=127 and off=0");
            ls.Add("|Controller          | Number|");
            ls.Add("|----------          | ------|");
            ir.Contents["controllers"].Values.ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            ls.Add("# Midi GM Drums");
            ls.Add("- These will vary depending on your Soundfont file.");
            ls.Add("|Drum                | Number|");
            ls.Add("|----                | ------|");
            ir.Contents["drums"].Values.ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            ls.Add("# Midi GM Drum Kits");
            ls.Add("- These will vary depending on your Soundfont file.");
            ls.Add("|Kit        | Number|");
            ls.Add("|---        | ------|");
            ir.Contents["drumkits"].Values.ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            return string.Join(Environment.NewLine, ls);
        }

        /// <summary>
        /// Make content from the definitions.
        /// </summary>
        /// <returns>Content.</returns>
        public string GenLua(string fn)
        {
            // key is section name, value is line
            Dictionary<string, List<string>> res = [];
            var ir = new IniReader(fn);

            List<string> ls = [];

            ls.Add("local M = {}");
            ls.Add("M.MAX_MIDI = 127");

            ls.Add("-- The GM midi instrument definitions.");
            ls.Add("M.instruments =");
            ls.Add("{");
            ir.Contents["instruments"].Values.ForEach(kv => ls.Add($"    {kv.Key} = {kv.Value},"));
            ls.Add("}");

            ls.Add("-- The GM midi controller definitions.");
            ls.Add("M.controllers =");
            ls.Add("{");
            ir.Contents["controllers"].Values.ForEach(kv => ls.Add($"    {kv.Key} = {kv.Value},"));
            ls.Add("}");

            ls.Add("-- The GM midi drum definitions.");
            ls.Add("M.drums =");
            ls.Add("{");
            ir.Contents["drums"].Values.ForEach(kv => ls.Add($"     {kv.Key} = {kv.Value},"));
            ls.Add("}");

            ls.Add("-- The GM midi drum kit definitions.");
            ls.Add("M.drum_kits =");
            ls.Add("{");
            ir.Contents["drumkits"].Values.ForEach(kv => ls.Add($"     {kv.Key} = {kv.Value},"));
            ls.Add("}");

            ls.Add("return M");

            return string.Join(Environment.NewLine, ls);
        }
        #endregion
    }
}
