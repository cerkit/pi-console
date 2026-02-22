using System.Text.RegularExpressions;

namespace pi_wasm.Helpers
{
    public static class SpectreConsoleParser
    {
        // Matches [color]text[/] or [color on bg]text[/] or [invert]text[/]
        private static readonly Regex _markupRegex = new Regex(@"\[([a-zA-Z0-9_#\s]+)\](.*?)\[/\]", RegexOptions.Compiled);

        public static string ParseToHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // HTML escape the entire input first, then we'll regex replace the markup brackets so the span tags aren't escaped.
            var encoded = System.Net.WebUtility.HtmlEncode(input);

            // Replace line endings with HTML breaks
            encoded = encoded.Replace("\r\n", "<br/>").Replace("\n", "<br/>");

            // Look for non-escaped brackets
            var html = _markupRegex.Replace(encoded, match =>
            {
                var styleStr = match.Groups[1].Value.Trim().ToLowerInvariant();
                var text = match.Groups[2].Value;

                string fg = "";
                string bg = "";

                if (styleStr.Contains(" on "))
                {
                    var parts = styleStr.Split(" on ");
                    fg = CleanColor(parts[0].Trim());
                    bg = CleanColor(parts[1].Trim());
                }
                else
                {
                    if (styleStr == "invert")
                    {
                        fg = "black";
                        bg = "white";
                    }
                    else
                    {
                        fg = CleanColor(styleStr);
                    }
                }

                string styleAttr = "";
                if (!string.IsNullOrEmpty(fg)) styleAttr += $"color: {fg}; ";
                if (!string.IsNullOrEmpty(bg)) styleAttr += $"background-color: {bg}; ";

                if (!string.IsNullOrEmpty(styleAttr))
                {
                    return $"<span style=\"{styleAttr.Trim()}\">{text}</span>";
                }

                return text;
            });

            // Additionally, undo Markup.Escape (converting "[[ " back to "[ " if needed) 
            html = html.Replace("[[", "[").Replace("]]", "]");

            return html;
        }

        private static string CleanColor(string color)
        {
            if (string.IsNullOrEmpty(color)) return string.Empty;
            
            // If the color ends in a number (e.g., orange1, dodgerblue1), strip the number so standard CSS can parse it.
            if (!color.StartsWith("#"))
            {
                color = Regex.Replace(color, @"\d+$", "");
            }
            return color;
        }
    }
}
