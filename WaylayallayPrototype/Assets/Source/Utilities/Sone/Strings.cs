using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sone.Strings
{
    public static class Strings
    {
        private static Regex m_whitespaceStartTrim = new Regex("^\\s+");
        private static Regex m_whitespaceEndTrim = new Regex("\\s+$");
        private static Regex m_parseTrailingNumber = new Regex("\\d+$");

        #region General Text Methods
        
        /// <summary>
        /// Print an IList.
        /// </summary>
        public static void Print<T>(this IList<T> array)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append("[");

            for (int i = 0; i < array.Count; i++)
            {
                sb.Append(array[i].ToString());

                if (i < array.Count - 1)
                    sb.Append(", ");
            }

            sb.Append("]");

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Just returns a random capital letter. That's all.
        /// </summary>
        public static char RandomLetter { get { return (char)UnityEngine.Random.Range(65, 91); } }

        /// <summary>
        /// Returns a string of random capital letters of the given length.
        /// </summary>
        public static string RandomString(int length)
        {
            char[] result = new char[length];

            for (int i = 0; i < length; i++)
                result[i] = RandomLetter;

            return new string(result);
        }

        /// <summary>
        /// Keep the given string below some character count, but preserve it along 
        /// some boundary. For example, if the delimiter is line breaks, preserve individual lines.
        /// 
        /// Prioritizes newer entries.
        /// </summary>
        public static string TrimByLines(string str, int characterCount, string delimiter)
        {
            string[] lines = str.Split(delimiter.ToCharArray()).Reverse().ToArray();
            List<string> newLines = new List<string>(lines.Length);

            int runningTotal = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == delimiter)
                    continue;

                if (runningTotal + lines[i].Length > characterCount)
                    break;

                runningTotal += lines[i].Length;

                newLines.Add(lines[i]);
            }

            StringBuilder sb = new StringBuilder();

            for (int i = newLines.Count - 1; i >= 0; --i)
            {
                sb.Append(newLines[i]);

                if (i != 0)
                    sb.Append(delimiter);
            }

            return sb.ToString();
        }

        #endregion

        #region String Extensions

        /// <summary>
        /// Looks for an integer at the end of the string. Returns -1 if failed.
        /// </summary>
        public static int ParseTrailingNumber(this string str)
        {
            Match numberMatch = m_parseTrailingNumber.Match(str);

            if (numberMatch.Captures.Count > 0)
            {
                string capture = numberMatch.Captures[0].ToString();
                int captureInt;

                if (int.TryParse(capture, out captureInt))
                    return captureInt;
            }

            return -1;
        }

        /// <summary>
        /// Remove all leading and trailing whitespace.
        /// </summary>
        public static string TrimWhitespace(this string str)
        {
            if (str == null)
                return null;

            return m_whitespaceEndTrim.Replace(m_whitespaceStartTrim.Replace(str, ""), "");
        }

        /// <summary>
        /// Ensure the string is a given length. Will pad with a custom character
        /// (default space) if under, or truncate if over.
        /// </summary>
        public static string FixLength(this string str, int length, char padder = ' ', bool addEllipsis = true)
        {
            if (str.Length > length)
            {
                str = str.Substring(0, length - (addEllipsis ? 3 : 0));

                if (addEllipsis)
                    str += "...";
            }
            else if (str.Length < length)
            {
                char[] charArray = new char[length - str.Length];

                for (int i = 0; i < charArray.Length; i++)
                    charArray[i] = padder;

                str = str + new string(charArray);
            }

            return str;
        }

        /// <summary>
        /// Add a colour hex code to the string.
        /// </summary>
        public static string AddColour(this string content, Color colour)
        {
            return "<color=\"#" + ColorUtility.ToHtmlStringRGB(colour) + "\">" + content + "</color>";
        }

        /// <summary>
        /// Remove any html style markup from the string.
        /// </summary>
        public static string RemoveMarkup(this string content)
        {
            return Regex.Replace(content, "<[^>]*>", string.Empty);
        }

        /// <summary>
        /// Remove any text in colour tags from the string.
        /// </summary>
        public static string RemoveColouredText(this string content)
        {
            return Regex.Replace(content, "<\\/*color.*>", string.Empty);
        }
        
        #endregion
        
        #region Enum Parsing

        /// <summary>
        /// Given a string of metadata, and a generic enum param, 
        /// returns the first instance where the string matches the form
        /// "ENUM_NAME:ENUM_VALUE."
        /// 
        /// If unfound, will return null.
        /// </summary>
        public static T? ParseEnumString<T>(string metaData) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type: Received " + typeof(T).ToString() + ")");

            // collapse all whitespace, set to upper case, split on whitespace
            string[] split = Regex.Replace(metaData, "\\s+", " ").ToUpperInvariant().Split(' ');

            string[] fullEnumString = typeof(T).ToString().ToUpperInvariant().Split('+');
            string enumString = fullEnumString[fullEnumString.Length - 1];

            foreach (string entry in split)
            {
                string[] entrySplit = entry.Split(':');

                if (entrySplit.Length == 2 && entrySplit[0] == enumString)
                {
                    foreach (T enumInstance in Enum.GetValues(typeof(T)))
                        if (entrySplit[1] == enumInstance.ToString())
                            return enumInstance;

                    Debug.LogWarning("Tag " + entrySplit[1] + " is not a recognized member of the " + enumString + " enum.");
                }
            }

            return null;
        }

        /// <summary>
        /// Given a string of metadata, and a generic enum param, 
        /// returns the first instance where the string matches the form
        /// "ENUM_NAME:ENUM_VALUE:ARGUMENT."
        /// 
        /// If unfound, will return null.
        /// </summary>
        public static T? ParseEnumString<T>(string metaData, out int? arg) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type: Received " + typeof(T).ToString() + ")");

            arg = null;

            // collapse all whitespace, set to upper case, split on whitespace
            string[] split = Regex.Replace(metaData, "\\s+", " ").ToUpperInvariant().Split(' ');

            string[] fullEnumString = typeof(T).ToString().ToUpperInvariant().Split('+');
            string enumString = fullEnumString[fullEnumString.Length - 1];

            foreach (string entry in split)
            {
                string[] entrySplit = entry.Split(':');

                if (entrySplit.Length != 2 && entrySplit.Length != 3)
                    return null;

                if (entrySplit[0] == enumString)
                {
                    if (entrySplit.Length == 2)
                    {
                        foreach (T enumInstance in Enum.GetValues(typeof(T)))
                            if (entrySplit[1] == enumInstance.ToString())
                                return enumInstance;

                        Debug.LogWarning("Tag " + entrySplit[1] + " is not a recognized member of the " + enumString + " enum.");
                    }
                    else if (entrySplit.Length == 3)
                    {
                        foreach (T enumInstance in Enum.GetValues(typeof(T)))
                        {
                            if (entrySplit[1] == enumInstance.ToString())
                            {
                                int argInt;
                                if (!int.TryParse(entrySplit[2], out argInt))
                                    Debug.LogWarning("Couldn't parse integer argument for " + entrySplit[0] + ":" + entrySplit[1] + "!");

                                arg = argInt;
                                return enumInstance;
                            }
                        }

                        Debug.LogWarning("Tag " + entrySplit[1] + " is not a recognized member of the " + enumString + " enum.");
                    }
                }
            }

            return null;
        }

        #endregion
    }
}
