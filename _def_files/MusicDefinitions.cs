using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;


namespace Ephemera.NBagOfTricks
{
    /// <summary>Definitions for use inside scripts. For doc see MusicDefinitions.md.</summary>
    public static class MusicDefinitions
    {
        #region Fields
        /// <summary>The chord/scale note definitions. Key is chord/scale name, value is list of constituent notes.</summary>
        static readonly Dictionary<string, List<string>> _chordsScales = [];

        /// <summary>all the notes.</summary>
        const int NOTES_PER_OCTAVE = 12;
        #endregion

        /// <summary>
        /// Load chord and scale definitions.
        /// </summary>
        static MusicDefinitions()
        {
            _chordsScales.Clear();

            foreach(string sl in _chordDefs.Concat(_scaleDefs))
            {
                var parts = sl.SplitByToken("|");
                var name = parts[0];
                _chordsScales[name] = parts[1].SplitByToken(" ");
            }
        }

        /// <summary>
        /// Make markdown content from the definitions.
        /// </summary>
        /// <returns></returns>
        public static List<string> FormatDoc()
        {
            List<string> docs = [];

            docs.Add("# Chords");
            docs.Add("These are the built-in chords.");
            docs.Add("Chord   | Notes             | Description");
            docs.Add("------- | ----------------- | -----------");
            docs.AddRange(_chordDefs);
            docs.Add("# Scales");
            docs.Add("These are the built-in scales.");
            docs.Add("Scale   | Notes             | Description       | Lower tetrachord  | Upper tetrachord");
            docs.Add("------- | ----------------- | ----------------- | ----------------  | ----------------");
            docs.AddRange(_scaleDefs);

            return docs;
        }

        #region Note definitions
        /// <summary>All possible note names and aliases.</summary>
        static readonly List<string> _noteNames =
        [
            "C",  "Db", "D", "Eb", "E",  "F",  "Gb", "G", "Ab", "A",  "Bb", "B",
            "B#", "C#", "",  "D#", "Fb", "E#", "F#", "",  "G#", "",   "A#", "Cb",
            "1",  "2",  "3", "4",  "5",  "6",  "7",  "8", "9",  "10", "11", "12"
        ];

        /// <summary>Helpers.</summary>
        static readonly List<int> _naturals =
        [
            0, 2, 4, 5, 7, 9, 11
        ];

        /// <summary>Helpers.</summary>
        static readonly List<string> _intervals =
        [
            "1", "b2", "2", "b3", "3", "4",  "b5",  "5", "#5", "6",  "b7", "7",
            "",  "",   "9", "#9", "",  "11", "#11", "",  "",   "13", "",   ""
        ];

        /// <summary>All the builtin chord defs.</summary>
        static readonly string[] _chordDefs =
        [
            "M       | 1 3 5             | Named after the major 3rd interval between root and 3.",
            "m       | 1 b3 5            | Named after the minor 3rd interval between root and b3.",
            "7       | 1 3 5 b7          | Also called dominant 7th.",
            "M7      | 1 3 5 7           | Named after the major 7th interval between root and 7th major scale note.",
            "m7      | 1 b3 5 b7         |",
            "6       | 1 3 5 6           | Major chord with 6th major scale note added.",
            "m6      | 1 b3 5 6          | Minor chord with 6th major scale note added.",
            "o       | 1 b3 b5           | Diminished.",
            "o7      | 1 b3 b5 bb7       | Diminished added 7.",
            "m7b5    | 1 b3 b5 b7        | Also called minor 7b5.",
            "+       | 1 3 #5            | Augmented.",
            "7#5     | 1 3 #5 b7         |",
            "9       | 1 3 5 b7 9        |",
            "7#9     | 1 3 5 b7 #9       | The 'Hendrix' chord.",
            "M9      | 1 3 5 7 9         |",
            "Madd9   | 1 3 5 9           | Chords extended beyond the octave are called added when the 7th is not present.",
            "m9      | 1 b3 5 b7 9       |",
            "madd9   | 1 b3 5 9          |",
            "11      | 1 3 5 b7 9 11     | The 3rd is often omitted to avoid a clash with the 11th.",
            "m11     | 1 b3 5 b7 9 11    |",
            "7#11    | 1 3 5 b7 #11      | Often used in preference to 11th chords to avoid the dissonant clash between 11 and 3 .",
            "M7#11   | 1 3 5 7 9 #11     |",
            "13      | 1 3 5 b7 9 11 13  | The 11th is often omitted to avoid a clash with the 3rd.",
            "M13     | 1 3 5 7 9 11 13   | The 11th is often omitted to avoid a clash with the 3rd.",
            "m13     | 1 b3 5 b7 9 11 13 |",
            "sus4    | 1 4 5             |",
            "sus2    | 1 2 5             | Sometimes considered as an inverted sus4 (GCD).",
            "5       | 1 5               | Power chord."
        ];

        /// <summary>All the builtin scale defs.</summary>
        static readonly string[] _scaleDefs =
        [
            "Acoustic                      | 1 2 3 #4 5 6 b7              | Acoustic scale                           | whole tone        | minor",
            "Aeolian                       | 1 2 b3 4 5 b6 b7             | Aeolian mode or natural minor scale      | minor             | Phrygian",
            "NaturalMinor                  | 1 2 b3 4 5 b6 b7             | Aeolian mode or natural minor scale      | minor             | Phrygian",
            "Algerian                      | 1 2 b3 #4 5 b6 7             | Algerian scale                           |                   |",
            "Altered                       | 1 b2 b3 b4 b5 b6 b7          | Altered scale                            | diminished        | whole tone",
            "Augmented                     | 1 b3 3 5 #5 7                | Augmented scale                          |                   |",
            "Bebop                         | 1 2 3 4 5 6 b7 7             | Bebop dominant scale                     |                   |",
            "Blues                         | 1 b3 4 b5 5 b7               | Blues scale                              |                   |",
            "Chromatic                     | 1 #1 2 #2 3 4 #4 5 #5 6 #6 7 | Chromatic scale                          |                   |",
            "Dorian                        | 1 2 b3 4 5 6 b7              | Dorian mode                              | minor             | minor",
            "DoubleHarmonic                | 1 b2 3 4 5 b6 7              | Double harmonic scale                    | harmonic          | harmonic",
            "Enigmatic                     | 1 b2 3 #4 #5 #6 7            | Enigmatic scale                          |                   |",
            "Flamenco                      | 1 b2 3 4 5 b6 7              | Flamenco mode                            | Phrygian          | Phrygian",
            "Gypsy                         | 1 2 b3 #4 5 b6 b7            | Gypsy scale                              | Gypsy             | Phrygian",
            "HalfDiminished                | 1 2 b3 4 b5 b6 b7            | Half diminished scale                    | minor             | whole tone",
            "HarmonicMajor                 | 1 2 3 4 5 b6 7               | Harmonic major scale                     | major             | harmonic",
            "HarmonicMinor                 | 1 2 b3 4 5 b6 7              | Harmonic minor scale                     | minor             | harmonic",
            "Hirajoshi                     | 1 3 #4 5 7                   | Hirajoshi scale                          |                   |",
            "HungarianGypsy                | 1 2 b3 #4 5 b6 7             | Hungarian Gypsy scale                    | Gypsy             | harmonic",
            "HungarianMinor                | 1 2 b3 #4 5 b6 7             | Hungarian minor scale                    | Gypsy             | harmonic",
            "In                            | 1 b2 4 5 b6                  | In scale                                 |                   |",
            "Insen                         | 1 b2 4 5 b7                  | Insen scale                              |                   |",
            "Ionian                        | 1 2 3 4 5 6 7                | Ionian mode or major scale               | major             | major",
            "Istrian                       | 1 b2 b3 b4 b5 5              | Istrian scale                            |                   |",
            "Iwato                         | 1 b2 4 b5 b7                 | Iwato scale                              |                   |",
            "Locrian                       | 1 b2 b3 4 b5 b6 b7           | Locrian mode                             | Phrygian          | whole tone",
            "LydianAugmented               | 1 2 3 #4 #5 6 7              | Lydian augmented scale                   | whole tone        | diminished",
            "Lydian                        | 1 2 3 #4 5 6 7               | Lydian mode                              | whole tone        | major",
            "Major                         | 1 2 3 4 5 6 7                | Ionian mode or major scale               | major             | major",
            "MajorBebop                    | 1 2 3 4 5 #5 6 7             | Major bebop scale                        |                   |",
            "MajorLocrian                  | 1 2 3 4 b5 b6 b7             | Major Locrian scale                      | major             | whole tone",
            "MajorPentatonic               | 1 2 3 5 6                    | Major pentatonic scale                   |                   |",
            "MelodicMinorAscending         | 1 2 b3 4 5 6 7               | Melodic minor scale (ascending)          | minor             | varies",
            "MelodicMinorDescending        | 1 2 b3 4 5 b6 b7 8           | Melodic minor scale (descending)         | minor             | major",
            "MinorPentatonic               | 1 b3 4 5 b7                  | Minor pentatonic scale                   |                   |",
            "Mixolydian                    | 1 2 3 4 5 6 b7               | Mixolydian mode or Adonai malakh mode    | major             | minor",
            "NeapolitanMajor               | 1 b2 b3 4 5 6 7              | Neapolitan major scale                   | Phrygian          | major",
            "NeapolitanMinor               | 1 b2 b3 4 5 b6 7             | Neapolitan minor scale                   | Phrygian          | harmonic",
            "Octatonic                     | 1 2 b3 4 b5 b6 6 7           | Octatonic scale (or 1 b2 b3 3 #4 5 6 b7) |                   |",
            "Persian                       | 1 b2 3 4 b5 b6 7             | Persian scale                            | harmonic          | unusual",
            "PhrygianDominant              | 1 b2 3 4 5 b6 b7             | Phrygian dominant scale                  | harmonic          | Phrygian",
            "Phrygian                      | 1 b2 b3 4 5 b6 b7            | Phrygian mode                            | Phrygian          | Phrygian",
            "Prometheus                    | 1 2 3 #4 6 b7                | Prometheus scale                         |                   |",
            "Tritone                       | 1 b2 3 b5 5 b7               | Tritone scale                            |                   |",
            "UkrainianDorian               | 1 2 b3 #4 5 6 b7             | Ukrainian Dorian scale                   | Gypsy             | minor",
            "WholeTone                     | 1 2 3 #4 #5 #6               | Whole tone scale                         |                   |",
            "Yo                            | 1 b3 4 5 b7                  | Yo scale                                 |                   |"
        ];
        #endregion

        #region Public note manipulation functions
        /// <summary>
        /// Convert note number into name.
        /// </summary>
        /// <param name="inote">Note number</param>
        /// <param name="octave">Include octave</param>
        /// <returns></returns>
        public static string NoteNumberToName(int inote, bool octave = true)
        {
            var split = SplitNoteNumber(inote);
            string s = octave ? $"{_noteNames[split.root]}{split.octave}" : $"{_noteNames[split.root]}";
            return s;
        }

        /// <summary>
        /// Convert note name into number.
        /// </summary>
        /// <param name="snote">The root of the note without octave.</param>
        /// <returns>The number or -1 if invalid.</returns>
        public static int NoteNameToNumber(string snote)
        {
            int inote = _noteNames.IndexOf(snote) % NOTES_PER_OCTAVE;
            return inote;
        }

        /// <summary>
        /// Parse note or notes from input value.
        /// </summary>
        /// <param name="noteString">Standard string to parse.</param>
        /// <returns>List of note numbers - empty if invalid.</returns>
        public static List<int> GetNotesFromString(string noteString)
        {
            List<int> notes = [];

            // Parse the input value.
            // Note: Need exception handling here to protect from user script errors.
            try
            {
                // Could be:
                // F4 - named note
                // F4.dim7 - named key/chord
                // F4.FOO - user defined key/chord or scale
                // F4.major - named key/scale

                // Break it up.
                var parts = noteString.SplitByToken(".");
                string snote = parts[0];

                // Start with octave.
                int octave = 4; // default is middle C
                string soct = parts[0].Last().ToString();

                if (soct.IsInteger())
                {
                    octave = int.Parse(soct);
                    snote = snote[..^1];
                }

                // Figure out the root note.
                int? noteNum = NoteNameToNumber(snote);
                if (noteNum is not null)
                {
                    // Transpose octave.
                    noteNum += (octave + 1) * NOTES_PER_OCTAVE;

                    if (parts.Count > 1)
                    {
                        // It's a chord. M, M7, m, m7, etc. Determine the constituents.
                        var chordNotes = _chordsScales[parts[1]];
                        //var chordNotes = chordParts[0].SplitByToken(" ");

                        for (int p = 0; p < chordNotes.Count; p++)
                        {
                            string interval = chordNotes[p];
                            bool down = false;

                            if (interval.StartsWith('-'))
                            {
                                down = true;
                                interval = interval.Replace("-", "");
                            }

                            int? iint = GetInterval(interval);
                            if (iint is not null)
                            {
                                iint = down ? iint - NOTES_PER_OCTAVE : iint;
                                notes.Add(noteNum.Value + iint.Value);
                            }
                        }
                    }
                    else
                    {
                        // Just the root.
                        notes.Add(noteNum.Value);
                    }
                }
                else
                {
                    notes.Clear();
                }
            }
            catch (Exception)
            {
                notes.Clear();
                //throw new InvalidOperationException("Invalid note or chord: " + noteString);
            }

            return notes;
        }

        /// <summary>
        /// Is it a white key?
        /// </summary>
        /// <param name="notenum">Which note</param>
        /// <returns>True/false</returns>
        public static bool IsNatural(int notenum)
        {
            return _naturals.Contains(SplitNoteNumber(notenum).root % NOTES_PER_OCTAVE);
        }

        /// <summary>
        /// Split a midi note number into root note and octave.
        /// </summary>
        /// <param name="notenum">Absolute note number</param>
        /// <returns>tuple of root and octave</returns>
        public static (int root, int octave) SplitNoteNumber(int notenum)
        {
            int root = notenum % NOTES_PER_OCTAVE;
            int octave = (notenum / NOTES_PER_OCTAVE) - 1;
            return (root, octave);
        }

        /// <summary>
        /// Get interval offset from name.
        /// </summary>
        /// <param name="sinterval"></param>
        /// <returns>Offset or -1 if invalid.</returns>
        public static int GetInterval(string sinterval)
        {
            int flats = sinterval.Count(c => c == 'b');
            int sharps = sinterval.Count(c => c == '#');
            sinterval = sinterval.Replace(" ", "").Replace("b", "").Replace("#", "");

            int iinterval = _intervals.IndexOf(sinterval);
            return iinterval == -1 ? -1 : iinterval + sharps - flats;
        }

        /// <summary>
        /// Get interval name from note number offset.
        /// </summary>
        /// <param name="iint">The name or empty if invalid.</param>
        /// <returns></returns>
        public static string GetInterval(int iint)
        {
            return iint >= _intervals.Count ? "" : _intervals[iint % _intervals.Count];
        }

        /// <summary>
        /// Try to make a note and/or chord string from the param. If it can't find a chord return the individual notes.
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public static List<string> FormatNotes(List<int> notes)
        {
            List<string> snotes = [];

            // Dissect root note.
            foreach (int n in notes)
            {
                int octave = SplitNoteNumber(n).octave;
                int root = SplitNoteNumber(n).root;
                snotes.Add($"\"{NoteNumberToName(root)}{octave}\"");
            }

            return snotes;
        }

        /// <summary>
        /// Add a new chord or scale definition.
        /// </summary>
        /// <param name="name">which</param>
        /// <param name="notes">what</param>
        public static void AddChordScale(string name, string notes)
        {
            _chordsScales[name] = notes.SplitByToken(" ");
        }

        /// <summary>
        /// Get a defined chord or scale definition.
        /// </summary>
        /// <param name="name">which</param>
        /// <returns>The list of notes or empty if invalid.</returns>
        public static List<string> GetChordScale(string name)
        {
            List<string> ret = [];
            if(_chordsScales.TryGetValue(name, out List<string>? value))
            {
                ret = value;
            }
            //throw new ArgumentException($"Invalid chord or scale: {name}");
            return ret;
        }
        #endregion
    }
}
