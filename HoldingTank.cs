namespace HoldingTank
{
    public class GenericListTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            // test:
            //return null; // clears UI, can't re-select
            //return value; // disables dropdown, UI unchanged
            //throw new Exception("----------------------------"); // message box

            var propName = context.PropertyDescriptor.Name; // "XXXPatch"
            var propType = context.PropertyDescriptor.PropertyType;

            //string typeDescription = propType switch
            //{
            //    var t when t == typeof(int) || t == typeof(int?) => "Integer property",
            //    var t when t == typeof(string) => "String property",
            //    var t when t == typeof(DateTime) || t == typeof(DateTime?) => "DateTime property",
            //    var t when t.IsEnum => "Enum property",
            //    _ => "Other type" // Default case
            //};

            //switch (Type.GetTypeCode(propType))
            //{
            //    case TypeCode.Int32: return lb.SelectedIndex;
            //    case TypeCode.String: return lb.SelectedItem;
            //    _: throw new InvalidOperationException($"Property {propName} type must be int or string");
            //}

            var ret = propType switch
            {
                var t when t == typeof(int) || t == typeof(int?) => lb.SelectedIndex,
                var t when t == typeof(string) => lb.SelectedItem,
                _ => throw new InvalidOperationException($"Property {propName} type must in or string")
            };

            return ret;


            // old way:
            //switch (propType.ToString())
            //{
            //    case "string": return lb.SelectedItem;
            //    case "int": return lb.SelectedIndex;
            //    _: throw new InvalidOperationException($"Property {propName} type must in or string");
            //}
            //return null;
        }


        void ReadMidiDefs()
        {
           //var srcDir = MiscUtils.GetSourcePath().Replace("\\", "/");

           List<string> s = [
               "local mid = require('midi_defs')",
               "for _,v in ipairs(mid.gen_list()) do print(v) end"
               ];

           var (ecode, sres) = ExecuteLuaChunk(s);

           if (ecode == 0)
           {
               foreach (var line in sres.SplitByToken(Environment.NewLine))
               {
                   var parts = line.SplitByToken(",");

                   switch (parts[0])
                   {
                       case "instrument": MidiDefs.Instruments.Add(int.Parse(parts[2]), parts[1]); break;
                       case "drum": MidiDefs.Drums.Add(int.Parse(parts[2]), parts[1]); break;
                       case "controller": MidiDefs.Controllers.Add(int.Parse(parts[2]), parts[1]); break;
                       case "kit": MidiDefs.DrumKits.Add(int.Parse(parts[2]), parts[1]); break;
                   }
               }
           }
        }
    }

    public class LuaStuff
    {
        public Stuff()
        {
            List<string> s = [
                "local mid = require('midi_defs')",
                "local mus = require('music_defs')",
                "for _,v in ipairs(mid.gen_md()) do print(v) end",
                "for _,v in ipairs(mus.gen_md()) do print(v) end",
                ];

            var (_, sres) = ExecuteLuaChunk(s);


            // --- Make csv list of the definitions for consumption by code.
            // -- @return list of strings - type,name,number
            // -- function M.gen_list()
            // --     local docs = {}
            // --     for k, v in pairs(M.instruments) do table.insert(docs, 'instrument,'..k..','..v) end
            // --     for k, v in pairs(M.drums) do table.insert(docs, 'drum,'..k..','..v) end
            // --     for k, v in pairs(M.controllers) do table.insert(docs, 'controller,'..k..','..v) end
            // --     for k, v in pairs(M.drum_kits) do table.insert(docs, 'kit,'..k..','..v) end
            // --     return docs;
            // -- end

        }

        /// <summary>
        /// Execute a chunk of lua code. Fixes up lua path and handles errors.
        /// </summary>
        /// <param name="scode"></param>
        /// <returns></returns>
        (int ecode, string sres) ExecuteLuaChunk(List<string> scode) //TODO2 put in LBOT?
        {
            var srcDir = MiscUtils.GetSourcePath().Replace("\\", "/");
            var luaPath = $"{srcDir}/LBOT/?.lua;{srcDir}/lua/?.lua;;";
            scode.Insert(0, $"package.path = '{luaPath}' .. package.path");

            var (ecode, sret) = Tools.ExecuteLuaCode(string.Join(Environment.NewLine, scode));

            if (ecode != 0)
            {
                // Command failed. Capture everything useful.
                List<string> lserr = [];
                lserr.Add($"=== code: {ecode}");
                lserr.Add($"=== stderr:");
                lserr.Add($"{sret}");

                _loggerApp.Warn(string.Join(Environment.NewLine, lserr));
            }
            return (ecode, sret);
        }
    }
}
