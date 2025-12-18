using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    /// <summary>Misc musical timing functions.</summary>
    /// <summary>Sort of like DateTime but for musical terminology.</summary>
    //    public class MusicTime : IComparable
    public struct MusicTime : IEquatable<MusicTime> //IComparable
    {
        // https://stackoverflow.com/a/4537848

        // For a value type, you should always implement IEquatable<T> and override Equals(Object) for better performance.
        // Equals(Object) boxes value types and relies on reflection to compare two values for equality. Both your
        // implementation of Equals(T) and your override of Equals(Object) should return consistent results.

        // If you implement IEquatable<T>, you should also implement IComparable<T> if instances of your type can be
        // ordered or sorted. If your type implements IComparable<T>, you almost always also implement IEquatable<T>.


        #region Fields
        /// <summary>For hashing comparable.</summary>
        readonly int _id;

        /// <summary>Increment for unique value.</summary>
        static int _all_ids = 1;

        /// <summary>Some features are at a lower resolution.</summary>
        public const int LOW_RES_PPQ = 8;
        #endregion

        #region Constants
        // MidiLib version:
        public static int InternalPPQ { get; set; } = 32;
        // Properties - internal
        /// <summary>Only 4/4 time supported.</summary>
        public static int BeatsPerBar { get { return 4; } }
        /// <summary>Our resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.</summary>
        public static int SubsPerBeat { get { return InternalPPQ; } }
        public static int SubsPerBar { get { return InternalPPQ * BeatsPerBar; } }
        // or?? public const int SubsPerBar = SubsPerBeat * BeatsPerBar;
        // TotalSubs = beats * MidiSettings.

        // nebulua version:
        // /// <summary>Only 4/4 time supported.</summary>
        // public const int BEATS_PER_BAR = 4;
        // /// <summary>GOur resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.</summary>
        // public const int SUBS_PER_BEAT = 8;
        // /// <summary>Convenience.</summary>
        // public const int SUBS_PER_BAR = SUBS_PER_BEAT * BEATS_PER_BAR;
        #endregion


bool _valid = false;

       // public const MusicTime ZERO = new MusicTime(0);

        #region Properties
        /// <summary>The time in subs. Always zero-based.</summary>
        public int TotalSubs { get; private set; }

        /// <summary>The time in beats. Always zero-based.</summary>
        public readonly int TotalBeats { get { return TotalSubs / SubsPerBeat; } }

        /// <summary>The bar number.</summary>
        public readonly int Bar { get { return TotalSubs / SubsPerBar; } }

        /// <summary>The beat number in the bar.</summary>
        public readonly int Beat { get { return TotalSubs / SubsPerBeat % BeatsPerBar; } }

        /// <summary>The sub in the beat.</summary>
        public readonly int Sub { get { return TotalSubs % SubsPerBeat; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default constructor.
        /// </summary>
        public MusicTime()
        {
            TotalSubs = 0;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from bar/beat/sub.
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="sub"></param>
        public MusicTime(int bar, int beat, int sub)
        {
            TotalSubs = (bar * SubsPerBar) + (beat * SubsPerBeat) + sub;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from subs.
        /// </summary>
        /// <param name="subs">Number of subs.</param>
        public MusicTime(int subs)
        {
            if (subs < 0)
            {
                throw new ArgumentException("Negative value is invalid");
            }

            TotalSubs = subs;
            _id = _all_ids++;
        }

        /// <summary>
        /// Construct a MusicTime from Beat.Sub representation as a double. Sub is LOW_RES_PPQ.
        /// </summary>
        /// <param name="beat"></param>
        /// <returns>New MusicTime.</returns>
        public MusicTime(double beat)
        {
            var (integral, fractional) = MathUtils.SplitDouble(beat);
            var beats = (int)integral;
            var subs = (int)Math.Round(fractional * 10.0);

            if (subs >= LOW_RES_PPQ)
            {
                throw new ArgumentException($"Invalid sub value: {beat}");
            }

            // Scale subs to native.
            subs = subs * InternalPPQ / LOW_RES_PPQ;
            TotalSubs = beats * SubsPerBeat + subs;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Hard reset.
        /// </summary>
        public void Reset()
        {
            TotalSubs = 0;
            _valid = false;
        }

        /// <summary>
        /// Utility helper function.
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        public void Constrain(MusicTime lower, MusicTime upper)
        {
            TotalSubs = MathUtils.Constrain(TotalSubs, lower.TotalSubs, upper.TotalSubs);
        }

        /// <summary>
        /// Update current value.
        /// </summary>
        /// <param name="subs">By this number of subs.</param>
        public void Increment(int subs)
        {
            TotalSubs += subs;
            if (TotalSubs < 0)
            {
                TotalSubs = 0;
            }
        }

        /// <summary>
        /// Set to sub using specified rounding.
        /// </summary>
        /// <param name="sub"></param>
        /// <param name="snapType"></param>
        /// <param name="up">To ceiling otherwise closest.</param>
        public void SetRounded(int sub, SnapType snapType, bool up = false)
        {
            if(sub > 0 && snapType != SnapType.Sub)
            {
                // res:32 in:27 floor=(in%aim)*aim  ceiling=floor+aim
                int res = snapType == SnapType.Bar ? SubsPerBar : SubsPerBeat;
                int floor = (sub / res) * res;
                int ceiling = floor + res;

                if (up || (ceiling - sub) >= res / 2)
                {
                    sub = ceiling;
                }
                else
                {
                    sub = floor;
                }
            }

            TotalSubs = sub;
        }

        /// <summary>
        /// Format a readable string.
        /// </summary>
        /// <returns></returns>
        public readonly string Format()
        {
           return $"{Bar}.{Beat}.{Sub:00}";
        }

        /// <summary>
        /// Format a readable string.
        /// </summary>
        /// <returns></returns>
        public override readonly string ToString()
        {
            return Format();
        }
        #endregion

        #region Conversions between music time and absolute
        /// <summary>Get the bar number.</summary>
        public static int BAR(int tick) { return tick / SubsPerBar; }

        /// <summary>Get the beat number in the bar.</summary>
        public static int BEAT(int tick) { return tick / SubsPerBeat % BeatsPerBar; }

        /// <summary>Get the sub in the beat.</summary>
        public static int SUB(int tick) { return tick % SubsPerBeat; }

        /// <summary>Convert a string bar time to absolute position/tick.</summary>
        /// <param name="sbt">time string can be "1.2.3" or "1.2" or "1".</param>
        /// <returns>Ticks or -1 if invalid input</returns>
        public static int Parse(string sbt)
        {
            int tick = 0;
            var parts = StringUtils.SplitByToken(sbt, ".");

            if (tick >= 0 && parts.Count > 0)
            {
                tick = (int.TryParse(parts[0], out int v) && v >= 0 && v <= 9999) ? tick + v * SubsPerBar : -1;
            }

            if (tick >= 0 && parts.Count > 1)
            {
                tick = (int.TryParse(parts[1], out int v) && v >= 0 && v <= BeatsPerBar - 1) ? tick + v * SubsPerBeat : -1;
            }

            if (tick >= 0 && parts.Count > 2)
            {
                tick = (int.TryParse(parts[2], out int v) && v >= 0 && v <= SubsPerBeat - 1) ? tick + v : -1;
            }

            return tick;
        }

        /// <summary>Convert a position/tick to string bar time.</summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        public static string Format(int tick)
        {
            if (tick >= 0)
            {
                int bar = BAR(tick);
                int beat = BEAT(tick);
                int sub = SUB(tick);
                return $"{bar}.{beat}.{sub}";
            }
            else
            {
                return "Invalid";
            }
        }
        #endregion



        public static implicit operator MusicTime(int value)
        {
            return new MusicTime(value);
        }
        
        public static bool operator ==(MusicTime a, MusicTime b)
        {
            return a.TotalSubs == b.TotalSubs;
        }

        public static bool operator !=(MusicTime a, MusicTime b)
        {
            return !(a == b);
        }

        public static MusicTime operator +(MusicTime a, MusicTime b)
        {
            return new MusicTime(a.TotalSubs + b.TotalSubs);
        }

        public static MusicTime operator -(MusicTime a, MusicTime b)
        {
            return new MusicTime(a.TotalSubs - b.TotalSubs);
        }

        public static bool operator <(MusicTime a, MusicTime b)
        {
            return a.TotalSubs < b.TotalSubs;
        }

        public static bool operator >(MusicTime a, MusicTime b)
        {
            return a.TotalSubs > b.TotalSubs;
        }

        public static bool operator <=(MusicTime a, MusicTime b)
        {
            return a.TotalSubs <= b.TotalSubs;
        }

        public static bool operator >=(MusicTime a, MusicTime b)
        {
            return a.TotalSubs >= b.TotalSubs;
        }



        public override readonly int GetHashCode()
        {
            return _id;
        }

        ////////////////// IEquatable
        public readonly bool Equals(MusicTime other)
        {
            return other is MusicTime tm && tm.TotalSubs == TotalSubs;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is MusicTime time && Equals(time);
        }



        // public int CompareTo(object? obj)
        // {
        //     if (obj is null)
        //     {
        //         throw new ArgumentException("Object is null");
        //     }

        //     MusicTime? other = obj as MusicTime;
        //     if (other is not null)
        //     {
        //         return TotalSubs.CompareTo(other.TotalSubs);
        //     }
        //     else
        //     {
        //         throw new ArgumentException("Object is not a MusicTime");
        //     }
        // }

    }

}
