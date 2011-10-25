using UnityEngine;
using System;
using System.Collections;

public static class TextTools {

    public static string Truncate(string original, int length)
    {
        if (original.Length <= length) return original;
        string s = original.Substring(0, length);
        s += "...";
        return s;
    }
    public static string TruncateAndTrim(string original, int length)
    {
        string s = Truncate(original, length);
        s = s.Replace("\r\n"," ");
        s = s.Replace("\n"," ");
        return s;
    }

    private static string _newline = "\n";

    /// <summary>
    /// Word wraps the given text to fit within the specified width.
    /// </summary>
    /// <param name="text">Text to be word wrapped</param>
    /// <param name="width">Width, in characters, to which the text
    /// should be word wrapped</param>
    /// <returns>The modified text</returns>
    public static string WordWrap(string text, int width)
    {
        int pos, next;
        string res = "";

        // Lucidity check
        if (width < 1)
            return text;

        // Parse each line of text
        for (pos = 0; pos < text.Length; pos = next) {
            // Find end of line
            int eol = text.IndexOf(_newline, pos);
            if (eol == -1)
                next = eol = text.Length;
            else
                next = eol + _newline.Length;

            // Copy this line of text, breaking into smaller lines as needed
            if (eol > pos) {
                do {
                    int len = eol - pos;
                    if (len > width)
                        len = BreakLine(text, pos, width);

                    res += text.Substring(pos, len);
                    res += _newline;
                    // Trim whitespace following break
                    pos += len;
                    while (pos < eol && Char.IsWhiteSpace(text[pos]))
                        pos++;
                } while (eol > pos);
            }
            else res += _newline; // Empty line
        }
        return res;
    }

    /// <summary>
    /// Locates position to break the given line so as to avoid
    /// breaking words.
    /// </summary>
    /// <param name="text">String that contains line of text</param>
    /// <param name="pos">Index where line of text starts</param>
    /// <param name="max">Maximum line length</param>
    /// <returns>The modified line length</returns>
    public static int BreakLine(string text, int pos, int max)
    {
        // Find last whitespace in line
        int i = max - 1;
        while (i >= 0 && !Char.IsWhiteSpace(text[pos + i]))
            i--;
        if (i < 0)
            return max; // No whitespace found; break at maximum length

        // Find start of whitespace
        while (i >= 0 && Char.IsWhiteSpace(text[pos + i]))
            i--;

        // Return length of text before whitespace
        return i + 1;
    }
}
