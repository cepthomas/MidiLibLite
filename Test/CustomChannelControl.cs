 using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite.Test
{
    public class CustomChannelControl : ChannelControl
    {
        int _lastNote = 0;

        //readonly ToolTip toolTip;


        /// <summary>Paint the surface.</summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            Graphics g = pe.Graphics;
            var r = DrawRect;

            //g.Clear(Color.LightCoral);

            g.FillRectangle(Brushes.LightCoral, r);

            // Border.
            g.DrawLine(Pens.Red, r.Left, r.Top, r.Right, r.Top);
            g.DrawLine(Pens.Red, r.Left, r.Bottom, r.Right, r.Bottom);
            g.DrawLine(Pens.Red, r.Left, r.Top, r.Left, r.Bottom);
            g.DrawLine(Pens.Red, r.Right, r.Top, r.Right, r.Bottom);

            // Grid.
            for (int x = r.Left; x < r.Right; x += 25)
            {
                g.DrawLine(Pens.White, x, r.Top, x, r.Bottom);
            }

            base.OnPaint(pe);
        }

        /// <summary>
        /// Show the pixel info.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var res = MouseToUser();

            if (res is not null)
            {
                toolTip.SetToolTip(this, $"X:{res.Value.ux} Y:{res.Value.uy}");

                // Also gen click?
                if (e.Button == MouseButtons.Left)
                {
                    // Dragging. Did it change?
                    if (_lastNote != res.Value.ux)
                    {
                        if (_lastNote != -1)
                        {
                            // Turn off last note.
                            OnSendMidi(new NoteOff(BoundChannel.ChannelNumber, _lastNote));
                        }

                        // Start the new note.
                        _lastNote = res.Value.ux;
                        OnSendMidi(new NoteOn(BoundChannel.ChannelNumber, res.Value.ux, res.Value.uy));
                    }
                }
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Send info to client.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            var res = MouseToUser();
            if (res is not null)
            {
                _lastNote = res.Value.ux;
                OnSendMidi(new NoteOn(BoundChannel.ChannelNumber, res.Value.ux, res.Value.uy));
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_lastNote != -1)
            {
                OnSendMidi(new NoteOff(BoundChannel.ChannelNumber, _lastNote));
                _lastNote = -1;
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// Disable control
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            // Turn off last click.
            if (_lastNote != -1)
            {
                OnSendMidi(new NoteOff(BoundChannel.ChannelNumber, _lastNote));
            }

            // Reset and tell client.
            _lastNote = -1;

            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Get mouse x and y mapped to user coordinates.
        /// </summary>
        /// <returns>Tuple of x and y.</returns>
        (int ux, int uy)? MouseToUser()
        {
            var mp = PointToClient(MousePosition);
            var r = DrawRect;

            // Map and check.
            int x = MathUtils.Map(mp.X, 0, r.Width, 0, MidiDefs.MAX_MIDI);
            int y = MathUtils.Map(mp.Y, r.Bottom, r.Top, 0, MidiDefs.MAX_MIDI);

            return (x >= 0 && x < MidiDefs.MAX_MIDI && y >= 0 && y < MidiDefs.MAX_MIDI) ?
                (x, y) :
                null;
        }
    }
}
