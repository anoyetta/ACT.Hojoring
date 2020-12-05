/*
 * Simple CSV Parser for C# without any dependency.
 *
 * These codes are licensed under CC0.
 * https://github.com/yutokun/CSV-Parser
 */

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FFXIV.Framework.Common
{
    public static class CSVParser
    {
        public enum Delimiter
        {
            Comma,
            Tab
        }

        private static readonly Dictionary<Delimiter, char> Delimiters = new Dictionary<Delimiter, char>() { { Delimiter.Comma, ',' }, { Delimiter.Tab, '\t' } };

        /// <summary>
        /// Load CSV data from specified path.
        /// </summary>
        /// <param name="path">CSV file path.</param>
        /// <param name="delimiter">Delimiter.</param>
        /// <param name="encoding">Type of text encoding. (default UTF-8)</param>
        /// <returns>Nested list that CSV parsed.</returns>
        public static List<List<string>> LoadFromPath(string path, Delimiter delimiter = Delimiter.Comma, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            var data = File.ReadAllText(path, encoding);
            return Parse(data, delimiter);
        }

        /// <summary>
        /// Load CSV data from string.
        /// </summary>
        /// <param name="data">CSV string</param>
        /// <param name="delimiter">Delimiter.</param>
        /// <returns>Nested list that CSV parsed.</returns>
        public static List<List<string>> LoadFromString(string data, Delimiter delimiter = Delimiter.Comma)
        {
            return Parse(data, delimiter);
        }

        private static List<List<string>> Parse(string data, Delimiter delimiter)
        {
            var sheet = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            var afterQuote = false;
            var insideQuoteCell = false;
            var readyToEndQuote = false;
            var delimiterChar = Delimiters[delimiter];

            // TODO : コードパスがひじょーにアレなので見やすく改良

            ConvertToCrlf(ref data);

            foreach (var character in data)
            {
                // Inside the quotation marks.
                if (insideQuoteCell)
                {
                    if (afterQuote)
                    {
                        if (character == '"')
                        {
                            // Consecutive quotes : A quotation mark.
                            cell.Append("\"");
                            afterQuote = false;
                        }
                        else if (readyToEndQuote && character != '"')
                        {
                            // Non-consecutive quotes : End of the quotation.
                            afterQuote = false;
                            insideQuoteCell = false;

                            if (character == delimiterChar)
                            {
                                AddCell(row, cell);
                            }
                        }
                        else
                        {
                            cell.Append(character);
                            afterQuote = false;
                        }

                        readyToEndQuote = false;
                    }
                    else
                    {
                        if (character == '"')
                        {
                            // A quot mark inside the quotation.
                            // Determine by the next character.
                            afterQuote = true;
                            readyToEndQuote = true;
                        }
                        else
                        {
                            cell.Append(character);
                        }
                    }
                }
                else
                {
                    // Outside the quotation marks.
                    if (character == delimiterChar)
                    {
                        AddCell(row, cell);
                    }
                    else if (character == '\n')
                    {
                        AddCell(row, cell);
                        AddRow(sheet, ref row);
                    }
                    else if (character == '"')
                    {
                        insideQuoteCell = true;
                    }
                    else
                    {
                        cell.Append(character);
                    }
                }
            }

            // Add last line except blank line
            if (row.Count != 0 || cell.Length != 0)
            {
                AddCell(row, cell);
                AddRow(sheet, ref row);
            }

            return sheet;
        }

        private static void AddCell(List<string> row, StringBuilder cell)
        {
            row.Add(cell.ToString());
            cell.Length = 0; // Old C#.
        }

        private static void AddRow(List<List<string>> sheet, ref List<string> row)
        {
            sheet.Add(row);
            row = new List<string>();
        }

        private static void ConvertToCrlf(ref string data)
        {
            data = Regex.Replace(data, @"\r\n|\r|\n", "\r\n");
        }
    }
}
