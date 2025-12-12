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
    /// <summary>Select from list supplied to cache. Value can be int or string.</summary>
    public class GenericListTypeEditor : UITypeEditor // TODO2 put in NBUI
    {
        #region Store global property options here.
        static readonly Dictionary<string, List<string>> _options = [];

        /// <summary>These get set by the client.</summary>
        public static void SetOptions(string propName, List<string> options)
        {
            _options[propName] = options;
        }

        public static List<string> GetOptions(string propName)
        {
            if (!_options.TryGetValue(propName, out var result))
            {
                throw new InvalidOperationException($"No options provided for property {propName}");
            }
            return result;
        }
        #endregion

        /// <summary>Standard property editor.</summary>
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            IWindowsFormsEditorService _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;

            var propName = context.PropertyDescriptor.Name;
            var propType = context.PropertyDescriptor.PropertyType;
            var options = GetOptions(propName);

            var lb = new ListBox
            {
                Width = 150,
                SelectionMode = SelectionMode.One
            };
            lb.Click += (_, __) => _service.CloseDropDown();
            options.ForEach(v => lb.Items.Add(v));
            _service.DropDownControl(lb);

            var ret = propType switch
            {
                var t when t == typeof(int) || t == typeof(int?) => lb.SelectedIndex,
                var t when t == typeof(string) => lb.SelectedItem,
                _ => throw new InvalidOperationException($"Property {propName} type must in or string")
            };

            return ret;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }

    /// <summary>Convert between list int and string versions.</summary>
    public class GenericConverter : Int64Converter
    {
        /// <summary>int to string</summary>
        public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
        {
            if (context is null || context.Instance is null || value is not int) { return base.ConvertTo(context, culture, value, destinationType); }
            var propName = context.PropertyDescriptor.Name;
            var options = GenericListTypeEditor.GetOptions(propName);
            return options[(int)value];
        }

        /// <summary>string to int</summary>
        public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
        {
            if (context is null || context.Instance is null || value is not int) { return base.ConvertFrom(context, culture, value); }
            var propName = context.PropertyDescriptor.Name;
            var options = GenericListTypeEditor.GetOptions(propName);
            var res = options.FirstOrDefault(ch => ch == (string)value);
            return res;
        }
    }

    #region Midi value editing
    /// <summary>Select a midi value from a range. Handles special case of channel number.</summary>
    public class MidiValueTypeEditor : UITypeEditor // TODO2 use generic?
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
}