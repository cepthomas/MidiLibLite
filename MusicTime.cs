using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    /// <summary>Sort of like DateTime but for musical terminology.</summary>
    public class MusicTime : IEquatable<MusicTime>
    {
        #region Fields
        /// <summary>For hashing comparable.</summary>
        readonly int _id;

        /// <summary>Increment for unique value.</summary>
        static int _allIds = 1;
        #endregion

        #region Static Properties
        /// <summary>Only 4/4 time supported currently.</summary>
        public static int BeatsPerBar { get { return 4; } }

        /// <summary>Our resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.</summary>
        public static int SubsPerBeat { get; set; } = 32; // { return InternalPPQ; } }

        /// <summary>Convenience.</summary>
        public static int SubsPerBar { get { return SubsPerBeat * BeatsPerBar; } }
        #endregion

        #region Properties
        /// <summary>The total time in ticks. Zero-based.</summary>
        public int Tick { get; set; }

        // /// <summary>The total time in beats. Zero-based.</summary>
        // public int TotalBeats { get { return TotalSubs / SubsPerBeat; } }

        // /// <summary>The time in music form.</summary>
        public (int bar, int beat, int sub) Parts
        {
            get { return (Tick / SubsPerBar, Tick / SubsPerBeat % BeatsPerBar, Tick % SubsPerBeat); }
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default constructor.
        /// </summary>
        public MusicTime()
        {
            Tick = 0;
            _id = _allIds++;
        }

        /// <summary>
        /// Constructor from tick.
        /// </summary>
        /// <param name="tick">Number of tick.</param>
        public MusicTime(int tick)
        {
            if (tick < 0)
            {
                throw new ArgumentException("Negative value is invalid");
            }

            Tick = tick;
            _id = _allIds++;
        }

        /// <summary>
        /// Constructor from bar/beat/sub.
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="sub"></param>
        public MusicTime(int bar, int beat, int sub)
        {
            Tick = (bar * SubsPerBar) + (beat * SubsPerBeat) + sub;
            _id = _allIds++;
        }

        ///// <summary>
        ///// Construct a MusicTime from Beat.Sub representation as a double. Sub is 0-7.
        ///// </summary>
        ///// <param name="beat"></param>
        ///// <returns>New MusicTime.</returns>
        //public MusicTime(double beat)
        //{
        //    var (integral, fractional) = MathUtils.SplitDouble(beat);
        //    var beats = (int)integral;
        //    var subs = (int)Math.Round(fractional * 10.0);

        //    if (subs >= 8)
        //    {
        //        throw new ArgumentException($"Invalid sub value: {beat}");
        //    }

        //    // Scale subs to native.
        //    subs = subs * InternalPPQ / 8;
        //    Tick = beats * SubsPerBeat + subs;
        //}

        /// <summary>
        /// Construct a MusicTime from a string repr.
        /// </summary>
        /// <param name="s">time string can be "1.2.3" or "1.2" or "1".</param>
        public MusicTime(string s)
        {
            int tick = 0;
            var parts = StringUtils.SplitByToken(s, ".");

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

            Tick = tick;
        }
        #endregion

        // TODO1 these operators instead of copy values?
        //public static explicit operator MusicTime(int value)
        //{
        //    return new MusicTime(value);
        //}

        //public static implicit operator int(MusicTime me)
        //{
        //    return me.Tick;
        //}


        #region Public functions
        ///// <summary>
        ///// Hard reset.
        ///// </summary>
        //public void Reset()
        //{
        //    Tick = 0;
        //    //_valid = false;
        //}

        /// <summary>
        /// Utility helper function.
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        public void Constrain(MusicTime lower, MusicTime upper)
        {
            Tick = MathUtils.Constrain(Tick, lower.Tick, upper.Tick);
        }

        /// <summary>
        /// Update current value.
        /// </summary>
        /// <param name="subs">By this number of subs. Can be negative aka decrement.</param>
        public void Increment(int subs)
        {
            Tick += subs;
            if (Tick < 0)
            {
                Tick = 0;
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
            if (sub > 0 && snapType != SnapType.Sub)
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

            Tick = sub;
        }

        /// <summary>
        /// Format a readable string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var p = Parts;
            return $"{p.bar}.{p.beat}.{p.sub:00} [{_id}:{Tick}]";
        }
        #endregion

        #region Operator implementations
        public override int GetHashCode() { return _id; }

        public static bool operator ==(MusicTime a, MusicTime b) { return a.Tick == b.Tick; }

        public static bool operator !=(MusicTime a, MusicTime b) { return !(a == b); }

        public static MusicTime operator +(MusicTime a, MusicTime b) { return new MusicTime(a.Tick + b.Tick); }

        public static MusicTime operator -(MusicTime a, MusicTime b) { return new MusicTime(a.Tick - b.Tick); }

        public static bool operator <(MusicTime a, MusicTime b) { return a.Tick < b.Tick; }

        public static bool operator >(MusicTime a, MusicTime b) { return a.Tick > b.Tick; }

        public static bool operator <=(MusicTime a, MusicTime b) { return a.Tick <= b.Tick; }

        public static bool operator >=(MusicTime a, MusicTime b) { return a.Tick >= b.Tick; }
        #endregion

        #region IEquatable
        public bool Equals(MusicTime? other) { return other is MusicTime tm && tm.Tick == Tick; }

        public override bool Equals(object? obj) { return obj is MusicTime time && Equals(time); }
        #endregion
    }
}
