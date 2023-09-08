using System.Dynamic;
using System.Globalization;
using System.Security;

namespace Nacencom.Infrastructure.Extensions
{
    public static class CollectionExtensions
    {
        private static readonly CultureInfo _cultureInfo;

        static CollectionExtensions()
        {
            _cultureInfo = new CultureInfo("en-US");
            _cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        }

        public static string JoinToString<T>(this IEnumerable<T> source, string separator, bool appendSeparator = false)
        {
            if (source == null) return null;
            return $"{string.Join(separator, source)}{(appendSeparator ? separator : string.Empty)}";
        }

        public static string SerializerToXml<T>(this IEnumerable<T> source)
        {
            if (source?.Any() != true) return string.Empty;
            var xml = "";
            foreach (var item in source.Where(t => t is not null))
            {
                xml += item switch
                {
                    ExpandoObject exp => $"<Data>{SerializerExpandoObject(exp)}</Data>",
                    _ => $"<Data>{SerializerObject(item)}</Data>"
                };
            }
            return $"<Root>{xml}</Root>";

            string SerializerObject(object obj)
            {
                var props = obj.GetType().GetProperties();
                return props?.Select(prop =>
                {
                    return $"<{prop.Name}>{GetValue(prop.GetValue(obj)) ?? ""}</{prop.Name}>";
                })
                .JoinToString("", false) ?? string.Empty;
            }

            string SerializerExpandoObject(ExpandoObject expObj)
            {
                return expObj.Select(t =>
                {
                    return $"<{t.Key}>{GetValue(t.Value) ?? ""}</{t.Key}>";
                })
                .JoinToString("", false);
            }

            static object GetValue(object value)
            {
                if (value is null)
                    return string.Empty;
                if (value is DateTime dt)
                {
                    if (DateTime.MinValue.Equals(dt))
                        return string.Empty;
                    return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
                if (value is DateTimeOffset dto)
                {
                    if (DateTime.MinValue.Equals(dto.DateTime) || DateTime.MinValue.Equals(dto.UtcDateTime))
                        return string.Empty;
                    return dto.ToString("yyyy-MM-dd HH:mm:ss.fff");

                }
                if (value is string str)
                {
                    return SecurityElement.Escape(str);
                }
                if (value is decimal d)
                {
                    return d.ToString(_cultureInfo);
                }
                if (value is float f)
                {
                    return f.ToString(_cultureInfo);
                }
                if (value is double dd)
                {
                    return dd.ToString(_cultureInfo);
                }
                return value.ToString();
            }
        }
    }
}
