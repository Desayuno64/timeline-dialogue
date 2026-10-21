using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Desayuno64.TimelineDialogue
{
    [Flags]
    public enum DialogueGlyphFlags
    {
        None = 0,
        Bold = 1,
        Jitter = 2,
        Wave = 4
    }

    public sealed class DialogueMarkup
    {
        public string Plain { get; }
        public DialogueGlyphFlags[] Flags { get; }
        public bool[] Colored { get; }
        public Color32[] Colors { get; }
        public string[] Sizes { get; }

        DialogueMarkup(string plain, DialogueGlyphFlags[] flags, bool[] colored, Color32[] colors, string[] sizes)
        {
            Plain = plain;
            Flags = flags;
            Colored = colored;
            Colors = colors;
            Sizes = sizes;
        }

        public static DialogueMarkup Parse(string authored)
        {
            if (string.IsNullOrEmpty(authored))
                return new DialogueMarkup(string.Empty, Array.Empty<DialogueGlyphFlags>(), Array.Empty<bool>(), Array.Empty<Color32>(), Array.Empty<string>());

            var plain = new StringBuilder(authored.Length);
            var flags = new List<DialogueGlyphFlags>(authored.Length);
            var colored = new List<bool>(authored.Length);
            var colors = new List<Color32>(authored.Length);
            var sizes = new List<string>(authored.Length);
            DialogueGlyphFlags current = DialogueGlyphFlags.None;
            bool hasColor = false;
            Color32 currentColor = default;
            string currentSize = null;

            for (int i = 0; i < authored.Length; i++)
            {
                char c = authored[i];
                if (c != '/')
                {
                    plain.Append(c);
                    flags.Add(current);
                    colored.Add(hasColor);
                    colors.Add(currentColor);
                    sizes.Add(currentSize);
                    continue;
                }

                if (i + 1 < authored.Length && authored[i + 1] == '/')
                {
                    plain.Append('/');
                    flags.Add(current);
                    colored.Add(hasColor);
                    colors.Add(currentColor);
                    sizes.Add(currentSize);
                    i++;
                    continue;
                }

                int j = i + 1;
                if (j < authored.Length && authored[j] == '#' && TryReadColor(authored, j + 1, out Color32 color, out int end))
                {
                    hasColor = true;
                    currentColor = color;
                    i = end - 1;
                    continue;
                }

                if (j < authored.Length && authored[j] == 's' && TryReadSize(authored, j + 1, out string size, out int sizeEnd))
                {
                    currentSize = size;
                    i = sizeEnd - 1;
                    continue;
                }

                DialogueGlyphFlags opened = DialogueGlyphFlags.None;
                while (j < authored.Length)
                {
                    DialogueGlyphFlags bit = FlagFor(authored[j]);
                    if (bit == DialogueGlyphFlags.None)
                        break;
                    opened |= bit;
                    j++;
                }

                if (opened != DialogueGlyphFlags.None)
                {
                    current |= opened;
                    i = j - 1;
                    continue;
                }

                char next = j < authored.Length ? authored[j] : '\0';
                if (next == '\0' || !char.IsLetter(next))
                {
                    current = DialogueGlyphFlags.None;
                    hasColor = false;
                    currentSize = null;
                    continue;
                }

                plain.Append('/');
                flags.Add(current);
                colored.Add(hasColor);
                colors.Add(currentColor);
                sizes.Add(currentSize);
            }

            return new DialogueMarkup(plain.ToString(), flags.ToArray(), colored.ToArray(), colors.ToArray(), sizes.ToArray());
        }

        public static string Strip(string authored)
        {
            return Parse(authored ?? string.Empty).Plain;
        }

        public string ToRichText()
        {
            if (Plain.Length == 0)
                return string.Empty;

            var rich = new StringBuilder(Plain.Length + 16);
            bool bold = false;
            bool colored = false;
            bool sized = false;
            Color32 color = default;
            string size = null;
            for (int i = 0; i < Plain.Length; i++)
            {
                bool wantBold = (Flags[i] & DialogueGlyphFlags.Bold) != 0;
                bool wantColor = Colored[i];
                Color32 want = Colors[i];
                bool wantSize = Sizes[i] != null;
                string wantSizeValue = Sizes[i];
                bool sizeDiffers = sized != wantSize || (wantSize && size != wantSizeValue);
                if (sizeDiffers || bold != wantBold || colored != wantColor || (wantColor && !color.Equals(want)))
                {
                    if (colored && (sizeDiffers || bold != wantBold || !wantColor || !color.Equals(want)))
                    {
                        rich.Append("</color>");
                        colored = false;
                    }

                    if (bold && (sizeDiffers || !wantBold))
                    {
                        rich.Append("</b>");
                        bold = false;
                    }

                    if (sizeDiffers && sized)
                    {
                        rich.Append("</size>");
                        sized = false;
                        size = null;
                    }

                    if (wantSize && !sized)
                    {
                        rich.Append("<size=");
                        rich.Append(wantSizeValue);
                        rich.Append('>');
                        sized = true;
                        size = wantSizeValue;
                    }

                    if (wantBold && !bold)
                    {
                        rich.Append("<b>");
                        bold = true;
                    }

                    if (wantColor && !colored)
                    {
                        rich.Append("<color=#");
                        AppendHex(rich, want);
                        rich.Append('>');
                        colored = true;
                        color = want;
                    }
                }

                AppendEscaped(rich, Plain[i]);
            }

            if (colored)
                rich.Append("</color>");
            if (bold)
                rich.Append("</b>");
            if (sized)
                rich.Append("</size>");

            return rich.ToString();
        }

        static bool TryReadSize(string authored, int start, out string size, out int end)
        {
            int i = start;
            if (i < authored.Length && (authored[i] == '+' || authored[i] == '-'))
                i++;

            bool dot = false;
            bool sawDigit = false;
            while (i < authored.Length)
            {
                char c = authored[i];
                if (c >= '0' && c <= '9')
                {
                    sawDigit = true;
                    i++;
                    continue;
                }

                if (c == '.' && !dot)
                {
                    dot = true;
                    i++;
                    continue;
                }

                break;
            }

            if (!sawDigit || authored[i - 1] == '.')
            {
                size = null;
                end = start;
                return false;
            }

            if (i < authored.Length && authored[i] == '%')
                i++;

            size = authored.Substring(start, i - start);
            end = i;
            return true;
        }

        static bool TryReadColor(string authored, int start, out Color32 color, out int end)
        {
            int i = start;
            while (i < authored.Length && IsHex(authored[i]))
                i++;

            int length = i - start;
            if (length != 3 && length != 4 && length != 6 && length != 8)
            {
                color = default;
                end = start;
                return false;
            }

            string hex = length == 3 || length == 4 ? ExpandShort(authored, start, length) : authored.Substring(start, length);
            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint packed))
            {
                color = default;
                end = start;
                return false;
            }

            byte r, g, b, a;
            if (hex.Length == 6)
            {
                r = (byte)((packed >> 16) & 0xff);
                g = (byte)((packed >> 8) & 0xff);
                b = (byte)(packed & 0xff);
                a = 255;
            }
            else
            {
                r = (byte)((packed >> 24) & 0xff);
                g = (byte)((packed >> 16) & 0xff);
                b = (byte)((packed >> 8) & 0xff);
                a = (byte)(packed & 0xff);
            }

            color = new Color32(r, g, b, a);
            end = i;
            return true;
        }

        static string ExpandShort(string authored, int start, int length)
        {
            var hex = new StringBuilder(length * 2);
            for (int i = 0; i < length; i++)
            {
                char c = authored[start + i];
                hex.Append(c);
                hex.Append(c);
            }

            return hex.ToString();
        }

        static bool IsHex(char c)
        {
            return (c >= '0' && c <= '9')
                || (c >= 'a' && c <= 'f')
                || (c >= 'A' && c <= 'F');
        }

        static DialogueGlyphFlags FlagFor(char c)
        {
            switch (c)
            {
                case 'b': return DialogueGlyphFlags.Bold;
                case 'j': return DialogueGlyphFlags.Jitter;
                case 'w': return DialogueGlyphFlags.Wave;
                default: return DialogueGlyphFlags.None;
            }
        }

        static void AppendHex(StringBuilder rich, Color32 color)
        {
            rich.Append(color.r.ToString("X2", CultureInfo.InvariantCulture));
            rich.Append(color.g.ToString("X2", CultureInfo.InvariantCulture));
            rich.Append(color.b.ToString("X2", CultureInfo.InvariantCulture));
            if (color.a != 255)
                rich.Append(color.a.ToString("X2", CultureInfo.InvariantCulture));
        }

        static void AppendEscaped(StringBuilder rich, char c)
        {
            switch (c)
            {
                case '<': rich.Append("&lt;"); break;
                case '>': rich.Append("&gt;"); break;
                case '&': rich.Append("&amp;"); break;
                default: rich.Append(c); break;
            }
        }
    }
}
