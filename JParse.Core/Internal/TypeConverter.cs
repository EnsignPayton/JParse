using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace JParse.Core.Internal
{
    internal class TypeConverter
    {
        // TODO: Write
        // Purpose: Take parser output and make it GENERIC

        public T Convert<T>(ExpandoObject input)
        {
            T result = default;

            var source = input as IDictionary<string, object>;
            var type = typeof(T);

            foreach (var prop in type.GetProperties())
            {
                var key = source.Keys.FirstOrDefault(k =>
                    string.Equals(k, prop.Name, StringComparison.OrdinalIgnoreCase));

                if (key != null)
                {
                    prop.SetValue(result, source[key]);
                }
            }

            return result;
        }
    }
}
