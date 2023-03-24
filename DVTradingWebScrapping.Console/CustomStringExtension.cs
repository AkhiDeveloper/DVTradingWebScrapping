using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DVTradingWebScrapping.Console
{
    public static class CustomStringExtension
    {
        public static string GetNumber(this string str)
        {
            string pattern = @"\b\d+\b";
            return Regex.Match(str, pattern).ToString();
        }
    }
}
