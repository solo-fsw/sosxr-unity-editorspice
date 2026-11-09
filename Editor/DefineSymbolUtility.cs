using System;
using System.Collections.Generic;
using System.Linq;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Pure string helpers for the semicolon-separated scripting define symbol list.
    ///     Matches whole symbols only, so "FOO" never matches "FOO_BAR".
    /// </summary>
    public static class DefineSymbolUtility
    {
        private static readonly char[] Separators = {';', ','};


        /// <summary>Splits a define list into trimmed, non-empty symbols.</summary>
        public static List<string> Split(string defines)
        {
            if (string.IsNullOrEmpty(defines))
            {
                return new List<string>();
            }

            return defines
                .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }


        /// <summary>True when the exact symbol is present in the define list.</summary>
        public static bool Contains(string defines, string symbol)
        {
            return !string.IsNullOrWhiteSpace(symbol) && Split(defines).Contains(symbol.Trim());
        }


        /// <summary>Returns the define list with the symbol appended. Existing symbols are kept.</summary>
        public static string Add(string defines, string symbol)
        {
            var list = Split(defines);

            if (string.IsNullOrWhiteSpace(symbol) || list.Contains(symbol.Trim()))
            {
                return string.Join(";", list);
            }

            list.Add(symbol.Trim());

            return string.Join(";", list);
        }


        /// <summary>Returns the define list without the symbol. Other symbols are kept intact.</summary>
        public static string Remove(string defines, string symbol)
        {
            var trimmed = symbol?.Trim();

            return string.Join(";", Split(defines).Where(s => s != trimmed));
        }
    }
}
