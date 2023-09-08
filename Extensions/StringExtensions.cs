using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nacencom.Infrastructure.Extensions
{
    public static class StringExtensions
    {
        public static string RegexReplace(this string input, string pattern, string replacement)
        {
            return string.IsNullOrEmpty(input) ? input : Regex.Replace(input, pattern, replacement);
        }

        public static string RemoveAccents(this string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string s = text.Normalize(NormalizationForm.FormD)
                .RegexReplace(@"\p{IsCombiningDiacriticalMarks}+", string.Empty)
                .Replace('\u0111', 'd')
                .Replace('\u0110', 'D')
                .Replace("“", "\"")
                .Replace("”", "\"")
                .Replace("ø", "o")
                .RegexReplace(@"[^\u0000-\u007F]", " ")
                .RegexReplace(@"\s{2,}", " ");
            return s;
        }

        public static string RemoveHexadecimalInvalidCharacters(this string text)
        {
            return text?.RegexReplace("[\x00-\x08\x0B\x0C\x0E-\x1F\x26]", "");
        }
    }
}
