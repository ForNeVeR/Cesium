// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Text;

namespace Cesium.Core;

public static class StringFormatExtensions
{
    extension(ReadOnlySpan<char> str)
    {
        public string FromKebabToCamel()
        {
            if (str.IsEmpty) return string.Empty;

            var sb = new StringBuilder();

            foreach (var part in str.Split('-'))
            {
                if (part.Start.Value == part.End.Value)
                    continue;

                sb.Append(char.ToUpper(str[part.Start]));

                if (part.Start.Value != part.End.Value)
                    sb.Append(str[(part.Start.Value + 1)..part.End]);
            }

            return sb.ToString();
        }

        public string FromCamelToKebab()
        {
            if (str.IsEmpty) return string.Empty;

            var sb = new StringBuilder();

            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsLower(str[i]))
                    sb.Append(str[i]);
                else if (i == 0)
                    sb.Append(char.ToLower(str[i]));
                else if (char.IsDigit(str[i]) && !char.IsDigit(str[i - 1]))
                    sb.Append('-').Append(str[i]);
                else if (char.IsDigit(str[i]))
                    sb.Append(str[i]);
                else if (char.IsLower(str[i - 1]))
                    sb.Append('-').Append(char.ToLower(str[i]));
                else if (i + 1 == str.Length || char.IsUpper(str[i + 1]))
                    sb.Append(char.ToLower(str[i]));
                else
                    sb.Append('-').Append(char.ToLower(str[i]));
            }

            return sb.ToString();
        }
    }
}
