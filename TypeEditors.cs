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
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


// Shut up warnings in this file. Using manual checking where needed.
#pragma warning disable CS8602

namespace Ephemera.MidiLibLite
{
    /// <summary>Select a patch from list.</summary>
    public class PatchTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            var instList = (context.Instance as Channel).Instruments;
            var lb = new ListBox
            {
                Width = 150,
                SelectionMode = SelectionMode.One
            };
            lb.Click += (_, __) => _service.CloseDropDown();
            instList.ForEach(v => lb.Items.Add($"{v.Value}({v.Key})"));
            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : lb.SelectedIndex;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }

    /// <summary>Convert between patch int and string versions.</summary>
    public class PatchConverter : Int64Converter
    {
        public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
        {
            if (context is null || context.Instance is null || value is not int) { return base.ConvertTo(context, culture, value, destinationType); }
            var instList = (context.Instance as Channel).Instruments;
            return instList[(int)value];
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
        {
            if (context is null || context.Instance is null || value is not int) { return base.ConvertFrom(context, culture, value); }
            var instList = (context.Instance as Channel).Instruments;
            var res = instList.FirstOrDefault(ch => ch.Value == (string)value);
            return res.Value;
        }
    }

    /// <summary>Select a midi value from a range. Handles special case of channel number.</summary>
    public class MidiValueTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            var isChan = context.PropertyDescriptor.Name == "ChannelNumber";
            int start = isChan ? 1 : 0;
            int end = isChan ? MidiDefs.NUM_CHANNELS : MidiDefs.MAX_MIDI;

            var lb = new ListBox
            {
                Width = 50,
                SelectionMode = SelectionMode.One
            };
            lb.Click += (_, __) => _service.CloseDropDown();

            Enumerable.Range(start, end).ForEach(v => lb.Items.Add(v.ToString()));

            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : int.Parse((string)lb.SelectedItem);
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }

    /// <summary>Select a device from list.</summary>
    public class DeviceTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            // Fill the selector.
            var lb = new ListBox
            {
                Width = 100,
                SelectionMode = SelectionMode.One
            };
            lb.Click += (_, __) => _service.CloseDropDown();

            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                lb.Items.Add(MidiOut.DeviceInfo(i).ProductName);
            }

            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : lb.SelectedItem.ToString();
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }
}