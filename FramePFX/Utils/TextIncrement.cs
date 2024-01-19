using System;
using System.Collections.Generic;
using System.IO;

namespace FramePFX.Utils {
    public static class TextIncrement {
        public static int GetChars(ulong value, char[] dst, int offset) {
            string str = value.ToString();
            str.CopyTo(0, dst, offset, str.Length);
            return str.Length;
        }

        public static string GetNextText(string input) {
            if (string.IsNullOrEmpty(input)) {
                return " (1)";
            }
            else if (GetNumbered(input, out string left, out long number)) {
                return $"{left} ({number + 1})";
            }
            else {
                return $"{input} (1)";
            }
        }

        public static string GetNextText(IEnumerable<string> inputs, string text) {
            if (string.IsNullOrEmpty(text)) {
                return text;
            }

            HashSet<string> available = new HashSet<string>(inputs);
            if (!GetIncrementableString((x) => !available.Contains(x), text, out string output))
                output = text;
            return output;
        }

        public static bool GetNumbered(string input, out string left, out long number) {
            if (GetNumberedRaw(input, out left, out string bracketed) && long.TryParse(bracketed, out number)) {
                return true;
            }

            number = default;
            return false;
        }

        public static bool GetNumberedRaw(string input, out string left, out string bracketed) {
            int indexA = input.LastIndexOf('(');
            if (indexA < 0 || (indexA != 0 && input[indexA - 1] != ' ')) {
                goto fail;
            }

            int indexB = input.LastIndexOf(')');
            if (indexB < 0 || indexB <= indexA || indexB != (input.Length - 1)) {
                goto fail;
            }

            if (indexA == 0) {
                left = "";
                bracketed = input.Substring(1, input.Length - 2);
            }
            else {
                left = input.Substring(0, indexA - 1);
                bracketed = input.JSubstring(indexA + 1, indexB);
            }

            return true;

            fail:
            left = bracketed = null;
            return false;
        }

        /// <summary>
        /// Generates a string, where a bracketed number is added after the given text. That number
        /// is incremented a maximum of <see cref="count"/> times (if there is no original bracket or it is
        /// currently at 0, it would end at 100 (inclusive) when <see cref="count"/> is 100). This is done
        /// repeatedly until the given predicate accepts the output string
        /// </summary>
        /// <param name="accept">Whether the output parameter can be accepted or not</param>
        /// <param name="input">Original text</param>
        /// <param name="output">A string that the <see cref="accept"/> predicate accepted</param>
        /// <param name="count">Max number of times to increment until the entry does not exist. <see cref="ulong.MaxValue"/> by default</param>
        /// <returns>True if the <see cref="accept"/> predicate accepted the output string before the loop counter reached 0</returns>
        /// <exception cref="ArgumentOutOfRangeException">The <see cref="count"/> parameter is zero</exception>
        /// <exception cref="ArgumentException">The <see cref="input"/> parameter is null or empty</exception>
        public static bool GetIncrementableString(Predicate<string> accept, string input, out string output, ulong count = ulong.MaxValue) {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must not be zero");
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));
            if (accept(input))
                return (output = input) != null; // one liner ;) always returns true

            if (!GetNumbered(input, out string content, out long textNumber) || textNumber < 1)
                textNumber = 1;

            ulong num = (ulong) textNumber;
            ulong max = Maths.WillOverflow(num, count) ? ulong.MaxValue : num + count;

            // This is probably over-optimised... this is just for concatenating a string and ulong
            // in the most efficient way possible. 23 = 3 (for ' ' + '(' + ')' chars) + 20 (for ulong.MaxValue representation)
            // hello (69) | len = 10, index = 7, j = 9, j+1 = 10 (passed to new string())
            if (content == null) {
                content = input;
            }

            int index = content.Length;
            char[] chars = new char[index + 23];
            content.CopyTo(0, chars, 0, index);
            chars[index] = ' ';
            chars[index + 1] = '(';
            index += 2;
            for (ulong i = num; i < max; i++) {
                // int len = TextIncrement.GetChars(i, chars, index);
                // int j = index + len; // val.Length
                string val = i.ToString();
                val.CopyTo(0, chars, index, val.Length);
                int j = index + val.Length;
                chars[j] = ')';
                // TODO: stack allocate string instead of heap allocate? probably not in NS2.0 :(
                // or maybe use some really really unsafe reflection/pointer manipulation
                output = new string(chars, 0, j + 1);
                if (accept(output)) {
                    return true;
                }
            }

            output = null;
            return false;
        }
    }
}