using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Reflection;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using NAudio.Midi;


// NEW ADDED


// namespace Ephemera.MidiLib
namespace Ephemera.MidiLibLite
{
    /// <summary>Select a patch from list.</summary>
    public class PatchTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            IWindowsFormsEditorService? _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;

            // Dig out from context.
            string[] vals;// = new string[MidiDefs.MAX_MIDI];

            Type t = context!.Instance!.GetType();
            PropertyInfo? prop = t.GetProperty("Instruments");
            vals = (string[])prop.GetValue(context.Instance, null);

            // Fill the selector.
            int sel = (int)value!; // default
            var lb = new ListBox
            {
                Width = 150,
                SelectionMode = SelectionMode.One
            };
            lb.Click += (_, __) => _service!.CloseDropDown();
            vals.ForEach(v => lb.Items.Add(v));
            _service!.DropDownControl(lb);

            return lb.SelectedItem is null ? value : lb.SelectedIndex;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        {
            return UITypeEditorEditStyle.DropDown;
        }
    }

    /// <summary>Select a channel from list.</summary>
    public class ChannelSelectorTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            IWindowsFormsEditorService? _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;

            // Fill the selector.
            var lb = new ListBox
            {
                Width = 50,
                SelectionMode = SelectionMode.One
            };
            lb.Click += (_, __) => _service!.CloseDropDown();

            Enumerable.Range(1, MidiDefs.NUM_CHANNELS).ForEach(v => lb.Items.Add(v.ToString()));

            _service!.DropDownControl(lb);

            return lb.SelectedItem is null ? value : int.Parse((string)lb.SelectedItem);
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        {
            return UITypeEditorEditStyle.DropDown;
        }
    }

    /// <summary>Select a device from list.</summary>
    public class DeviceTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            IWindowsFormsEditorService? _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;

            // Fill the selector.
            var lb = new ListBox
            {
                Width = 100,
                SelectionMode = SelectionMode.One
            };
            lb.Click += (_, __) => _service!.CloseDropDown();

            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                lb.Items.Add(MidiOut.DeviceInfo(i).ProductName);
            }

            _service!.DropDownControl(lb);

            return lb.SelectedItem is null ? value : lb.SelectedItem.ToString();
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        {
            return UITypeEditorEditStyle.DropDown;
        }
    }

    /// <summary>
    /// xxxxx
    /// </summary>
    public class PatchConverter : Int64Converter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
        {
            //return MidiDefs.GetInstrumentName((int)value!);

            if (value is int && destinationType == typeof(string))
            {
               //return MidiDefs.GetInstrumentName((int)value);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
        {
            string txt = value.ToString();

            //return MidiDefs.GetInstrumentNumber(txt);

            return base.ConvertFrom(context, culture, value);
        }
    }
}