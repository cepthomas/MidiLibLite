#define _NEW_ML

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using Ephemera.NBagOfTricks;


// TODO2 ? zoom, drag, shift

// Hook into Nebulua.
// TODO1 Look at how other clients use old MidiLib API: Nebulator, Midifrier
// X TimeDefs => use SectionInfo
// ? public event EventHandler? CurrentTimeChanged;
// X IncrementCurrent(1)
// X MidiSettings.LibSettings


namespace Ephemera.MidiLibLite
{
    /// <summary>The control.</summary>
    [DesignTimeVisible(true)]
    [Browsable(false)]
    public class TimeBar : UserControl
    {
        #region Fields
        /// <summary>For tracking mouse moves.</summary>
        int _lastXPos = 0;

        /// <summary>Metadata.</summary>
        List<(int tick, string name)> _sectionInfo = [];

        /// <summary>Tooltip for mousing.</summary>
        readonly ToolTip _toolTip = new();

        /// <summary>For drawing text.</summary>
        readonly StringFormat _format = new();

        /// <summary>For drawing lines etc.</summary>
        readonly Pen _penMarker = new(Color.Red, 2);

        /// <summary>For drawing lines etc.</summary>
        readonly Pen _penSel = new(Color.Blue, 1);

        /// <summary>Total length of the sequence.</summary>
        readonly MusicTime _length = new();

        ///// <summary>Start of marked region.</summary>
        readonly MusicTime _selStart = new();

        ///// <summary>End of marked region.</summary>
        readonly MusicTime _selEnd = new();

        /// <summary>Where we be now.</summary>
        readonly MusicTime _current = new();

        /// <summary>Convenience.</summary>
        readonly MusicTime ZERO = new();

        /// <summary>Avoid creating many transient objects.</summary>
        readonly MusicTime TRANSIENT = new();
        #endregion

        #region Properties
        /// <summary>Big font.</summary>
        public Font FontLarge { get { return _fontLarge; } set { _fontLarge = value; Invalidate(); } }
        Font _fontLarge = new("Microsoft Sans Serif", 16, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Baby font.</summary>
        public Font FontSmall { get; set; } = new("Microsoft Sans Serif", 10, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Drawing the active elements of a control.</summary>
        public Color ControlColor { get { return _controlColor; } set { _controlColor = value; Invalidate(); } }
        Color _controlColor = Color.Red;

        /// <summary>Drawing the control when selected.</summary>
        public Color SelectedColor { get { return _selectedColor; } set { _selectedColor = value; Invalidate(); } }
        Color _selectedColor = Color.Blue;

        /// <summary>How to select times.</summary>
        public SnapType Snap { get; set; } = SnapType.Beat;

        /// <summary>Keep going at end of loop.</summary> 
        public bool DoLoop { get { return _doLoop; } set { _doLoop = value; Invalidate(); } }
        bool _doLoop = false;

        /// <summary>Convenience for readability.</summary>
        public bool FreeRunning { get { return _length.Tick == 0; } }

        // public MusicTime Length { get { return _length; } }
        // public MusicTime Current { get { return _current; } }
        #endregion

        #region Events  TODO1 what does client really need?
        //public event EventHandler<string>? ValueChangeEvent;
        //public void NotifyStateChanged([CallerMemberName] string name = "")
        //{
        //    ValueChangeEvent?.Invoke(this, name);
        //}
        //
        // /// <summary>Value changed by user.</summary>
        // public event EventHandler? CurrentTimeChanged;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public TimeBar()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
                _penMarker.Dispose();
                _penSel.Dispose();
                _format.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Client supplies metadata about sections in the time range.
        /// </summary>
        /// <param name="sectInfo"></param>
        public void InitSectionInfo(Dictionary<int, string> sectInfo)
        {
            _sectionInfo.Clear();
            _length.Set(0);
            _selStart.Set(0);
            _selEnd.Set(0);
            _current.Set(0);

            if (sectInfo.Count > 0)
            {
                List<(int tick, string name)> sinfo = [];
                var spos = sectInfo.Keys.OrderBy(k => k).ToList();
                spos.ForEach(sp => _sectionInfo.Add((sp, sectInfo[sp])));
                _length.Set(_sectionInfo.Last().tick);

                ValidateTimes();
                Invalidate();
            }
        }

        /// <summary>Back up dude.</summary>
        public void Rewind()
        {
            _current.Set(DoLoop ? _selStart.Tick : 0);

            ValidateTimes();
            Invalidate();
        }

        /// <summary>Change current time programmatically.</summary>
        /// <param name="num">Subs/ticks.</param>
        public void IncrementCurrent(int num)
        {
            _current.Increment(num);

            if (FreeRunning)
            {
                // Nothing to do.
            }
            else if (DoLoop)
            {
                if (_current >= _selEnd)
                {
                    _current.Set(_selEnd.Tick, Snap);
                }
                else if (_current <= _selStart)
                {
                    _current.Set(_selStart.Tick, Snap);
                }
                // else keep going.
            }
            else // not looping
            {
                _current.Constrain(ZERO, _length);
            }

            ValidateTimes();
            Invalidate();
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draw the control.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            // Setup.
            pe.Graphics.Clear(BackColor);

            if (FreeRunning)
            {
                // Simple text only.
                _format.Alignment = StringAlignment.Center;
                _format.LineAlignment = StringAlignment.Near; // Center;
                pe.Graphics.DrawString(_current.ToString(), FontLarge, Brushes.Black, ClientRectangle, _format);

                return;
            }

            _penMarker.Color = ControlColor;
            _penSel.Color = SelectedColor;

            ///// Loop area.
            if (DoLoop)
            {
                // box:
                int lstart = GetClientFromTick(_selStart.Tick);
                int lend = GetClientFromTick(_selEnd.Tick);
                pe.Graphics.FillPolygon(_penSel.Brush, new PointF[]
                    { new(lstart, 0), new(lend, 0), new(lend, Height), new(lstart, Height) });
            }

            // Bars? vert line per bar or ?

            ///// Sections.
            var fsize = pe.Graphics.MeasureString("X", FontSmall).Height;
            foreach (var (tick, name) in _sectionInfo)
            {
                int sect = GetClientFromTick(tick);
                pe.Graphics.DrawLine(_penMarker, sect, 0, sect, Height);
                _format.Alignment = StringAlignment.Center;
                _format.LineAlignment = StringAlignment.Center;
                pe.Graphics.DrawString(name, FontSmall, Brushes.Black, sect + 2, Height - fsize - 2);
            }

            ///// Current pos.
            int cpos = GetClientFromTick(_current.Tick);
            pe.Graphics.DrawLine(_penMarker, cpos, 0, cpos, Height);
            pe.Graphics.FillPolygon(_penMarker.Brush, new PointF[] { new(cpos - 10, 0), new(cpos + 10, 0), new(cpos, 20) });

            ///// Text.
            _format.Alignment = StringAlignment.Center;
            _format.LineAlignment = StringAlignment.Near; // Center;
            pe.Graphics.DrawString(_current.ToString(), FontLarge, Brushes.Black, ClientRectangle, _format);

            _format.Alignment = StringAlignment.Near;
            _format.LineAlignment = StringAlignment.Near;
            pe.Graphics.DrawString(_selStart.ToString(), FontSmall, Brushes.Black, ClientRectangle, _format);

            _format.Alignment = StringAlignment.Far;
            _format.LineAlignment = StringAlignment.Near;
            pe.Graphics.DrawString(_selEnd.ToString(), FontSmall, Brushes.Black, ClientRectangle, _format);
        }
        #endregion

        #region UI handlers
        /// <summary>
        /// Handle selection operations.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!FreeRunning && e.KeyData == Keys.Escape)
            {
                // Reset.
                _selStart.Set(0);
                _selEnd.Set(0);

                Invalidate();
            }

            base.OnKeyDown(e);
        }

        /// <summary>
        /// Handle mouse position changes.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            // SetToolTip triggers mouse move event - infernal loop.
            if (!FreeRunning && e.X != _lastXPos)
            {
                _lastXPos = e.X;

                var tick = GetTickFromClient(e.X);
                TRANSIENT.Set(tick);
                var roundedTick = GetRounded(tick, Snap);
                var sdef = GetTimeDefString(roundedTick);
                //var tdef = new MusicTime(roundTick);

                _toolTip.SetToolTip(this, $"{TRANSIENT} {sdef}");
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Selection of time and loop points.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!FreeRunning)
            {
                int newval = GetTickFromClient(e.X);

                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    _selStart.Set(newval, Snap);
                }
                else if (ModifierKeys.HasFlag(Keys.Alt))
                {
                    _selEnd.Set(newval, Snap);
                }
                else
                {
                    _current.Set(newval, Snap);
                }

                ValidateTimes();
                Invalidate();
            }

            base.OnMouseDown(e);
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Validate and correct all times. 0 -> sel-start -> sel-end -> length
        /// </summary>
        void ValidateTimes()
        {
            if (FreeRunning)
            {
                // Reset.
                _selStart.Set(0);
                _selEnd.Set(0);
                _current.Set(0);
            }
            else if (DoLoop)
            {
                // Fix loop points.
                _selEnd.Constrain(ZERO, _length);
                _selStart.Constrain(ZERO, _selEnd);
                _current.Constrain(_selStart, _selEnd);
            }
            // else do nothing
        }

        /// <summary>
        /// Convert x pos to sub.
        /// </summary>
        /// <param name="x"></param>
        int GetSubFromMouse(int x)
        {
            int sub = 0;

            if(_current < _length)
            {
                sub = x * _length.Tick / Width;
                sub = MathUtils.Constrain(sub, 0, _length.Tick);
            }

            return sub;
        }

        /// <summary>
        /// Gets the time def string associated with val.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        string GetTimeDefString(int val)
        {
            string s = "";
            _sectionInfo.TakeWhile(si => si.tick <= val).ForEach(si => s = si.name);
            return s;
        }

        /// <summary>
        /// Convert x pos to tick.
        /// </summary>
        /// <param name="x"></param>
        int GetTickFromClient(int x)
        {
            int tick = 0;

            if (_current < _length)
            {
                tick = x * _length.Tick / Width;
                tick = MathUtils.Constrain(tick, 0, _length.Tick);
            }

            return tick;
        }

        /// <summary>
        /// Map from time to UI pixels.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        int GetClientFromTick(int tick)
        {
            return _length.Tick > 0 ? tick * Width / _length.Tick : 0;
        }

        /// <summary>
        /// Set to sub using specified rounding.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="snapType"></param>
        /// <param name="up">To ceiling otherwise closest.</param>
        static int GetRounded(int tick, SnapType snapType, bool up = false)
        {
            if (tick > 0 && snapType != SnapType.Subbeat)
            {
                int res = snapType == SnapType.Bar ? MusicTime.SubbeatsPerBar : MusicTime.SubbeatsPerBeat;

                double dtick = Math.Floor((double)tick);
                int floor = (int)(dtick / res) * res;
                int ceiling = floor + res;

                tick = (up || (ceiling - tick) < res / 2) ? ceiling : floor;
            }

            return tick;
        }
        #endregion
    }
}
