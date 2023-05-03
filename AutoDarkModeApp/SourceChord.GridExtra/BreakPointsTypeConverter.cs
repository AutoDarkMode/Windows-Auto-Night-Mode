using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceChord.GridExtra
{
    public class BreakPointsTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            //return base.CanConvertFrom(context, sourceType);
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var text = (string)value;
            var list = text.Split(',')
                           .Select(o => o.Trim())
                           .Select(o => int.Parse(o))
                           .ToList();

            if (list.Count() != 3)
            {
                throw new ArgumentException($"'{value}' Invalid value. BreakPoints must contains 3 items.");
            }

            return new BreakPoints() { XS_SM = list[0], SM_MD = list[1], MD_LG = list[2] };
        }
    }
}
