using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;
using System.Reflection;


// Shut up warnings in this file. Using manual checking where needed.
#pragma warning disable CS8602

namespace Ephemera.MidiLibLite
{


    /// <summary>Select a XXX from list. Borrowed from IDAS_1_0_4\Source\CommonUi\ColumnSelector.cs</summary>
    public class GenericListTypeEditor : UITypeEditor // TODO1 put in NBUI
    {

        // <summary>
        // This gets set by the client.
        // </summary>


        /// <summary>
        /// This gets set by the client.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="options"></param>
        public static void SetOptions(string propertyName, List<string> options)
        {
            _options[propertyName] = options;
        }
        static readonly Dictionary<string, List<string>> _options = [];


        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            // test:
            //return null; // clears UI, can't re-select
            //return value; // disables dropdown, UI unchanged
            //throw new Exception("----------------------------"); // message box

            IWindowsFormsEditorService _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;


            //if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service ||
            //    context is null || context.Instance is null) { return null; }

            //var options = CommonXXX.GetOptions(context);

            var propertyName = context.PropertyDescriptor.Name; // "XXXPatch"

            if (!_options.TryGetValue(propertyName, out List<string>? options))
            {
                //TODO1 error - missing options list - throw?
                throw new InvalidOperationException($"No options provided for property {propertyName}");
                //return value;
            }

            var lb = new ListBox
            {
                Width = 150,
                SelectionMode = SelectionMode.One
            };
            lb.Click += (_, __) => _service.CloseDropDown();
            options.ForEach(v => lb.Items.Add(v));
            _service.DropDownControl(lb);


            switch (value)
            {
                case string s: return lb.SelectedItem;
                case int i: return lb.SelectedIndex;
                _: throw new InvalidOperationException($"Property {propertyName} type must in or string");
            }

            return null;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }



    #region Midi value editing
    /// <summary>Select a midi value from a range. Handles special case of channel number.</summary>
    public class MidiValueTypeEditor : UITypeEditor // TODO1 use generic?
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            var isChan = context.PropertyDescriptor.Name == "ChannelNumber";
            int start = isChan ? 1 : 0;
            int end = isChan ? MidiDefs.NUM_CHANNELS : MidiDefs.MAX_MIDI;

            var lb = new ListBox(); // {Width = 50,SelectionMode = SelectionMode.One};
            Enumerable.Range(start, end).ForEach(v => lb.Items.Add(v.ToString()));
            lb.Click += (_, __) => _service.CloseDropDown();
            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : int.Parse((string)lb.SelectedItem);
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }
    #endregion


#if OLD_EDS
    #region Patch editing
    /// <summary>Select a patch from list.</summary>
    public class PatchTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            var instList = (context.Instance as OutputChannel).Instruments;
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

            var instList = (context.Instance as OutputChannel).Instruments;
            return instList[(int)value];
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
        {
            if (context is null || context.Instance is null || value is not int) { return base.ConvertFrom(context, culture, value); }
            var instList = (context.Instance as OutputChannel).Instruments;
            var res = instList.FirstOrDefault(ch => ch.Value == (string)value);
            return res.Value;
        }
    }
    #endregion

    #region ControllerId editing
    /// <summary>Select a controller from list.</summary>
    public class ControllerIdTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            var ctlrList = MidiDefs.TheDefs.GetControllerIdDefs(true);
            var lb = new ListBox
            {
                Width = 150,
                SelectionMode = SelectionMode.One
            };
            lb.Click += (_, __) => _service.CloseDropDown();
            ctlrList.ForEach(v => lb.Items.Add($"{v.Value}({v.Key})"));
            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : lb.SelectedIndex;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }

    /// <summary>Convert between controller int and string versions.</summary>
    public class ControllerIdConverter : Int64Converter
    {
        public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
        {
            if (context is null || context.Instance is null || value is not int) { return base.ConvertTo(context, culture, value, destinationType); }
            var ctlrList = MidiDefs.TheDefs.GetControllerIdDefs(true);
            return ctlrList[(int)value];
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
        {
            if (context is null || context.Instance is null || value is not int) { return base.ConvertFrom(context, culture, value); }
            var ctlrList = MidiDefs.TheDefs.GetControllerIdDefs(true);
            var res = ctlrList.FirstOrDefault(ch => ch.Value == (string)value);
            return res.Value;
        }
    }
    #endregion

    #region Device editing
    /// <summary>Select a device from list.</summary>
    public class DeviceTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            var isOut = context.PropertyDescriptor.Name.Contains("Output");
            var devs = isOut ? MidiOutputDevice.AvailableDevices() : MidiInputDevice.AvailableDevices();

            // Fill the selector.
            var lb = new ListBox(); // {Width = 100,SelectionMode = SelectionMode.One};
            devs.ForEach(d => lb.Items.Add(d));
            lb.Click += (_, __) => _service.CloseDropDown();
            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : lb.SelectedItem.ToString();
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }
    #endregion
#endif
}