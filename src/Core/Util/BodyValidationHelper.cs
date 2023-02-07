using System.Linq;

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
