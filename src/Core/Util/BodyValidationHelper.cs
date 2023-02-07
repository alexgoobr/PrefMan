using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrefMan.Core.Util
{
    public class BodyValidationHelper
    {
        public static bool ObjectHasNullProperties(object obj)
        {
            return obj.GetType().GetProperties()
                .Where(pi => pi.PropertyType == typeof(string))
                .Select(pi => (string)pi.GetValue(obj))
                .Any(value => value == null);
        }
    }
}
