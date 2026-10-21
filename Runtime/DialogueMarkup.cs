using System;
using System.Collections.Generic;
using System.Text;

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

        DialogueMarkup(string plain, DialogueGlyphFlags[] flags)
        {
            Plain = plain;
            Flags = flags;
        }

        public static DialogueMarkup Parse(string authored)
        {
            if (string.IsNullOrEmpty(authored))
                return new DialogueMarkup(string.Empty, Array.Empty<DialogueGlyphFlags>());

            var plain = new StringBuilder(authored.Length);
            var flags = new List<DialogueGlyphFlags>(authored.Length);
            DialogueGlyphFlags current = DialogueGlyphFlags.None;

            for (int i = 0; i < authored.Length; i++)
            {
                char c = authored[i];
                if (c != '/')
                {
                    plain.Append(c);
                    flags.Add(current);
                    continue;
                }

                if (i + 1 < authored.Length && authored[i + 1] == '/')
                {
                    plain.Append('/');
                    flags.Add(current);
                    i++;
                    continue;
                }

                int j = i + 1;
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
                    continue;
                }

                plain.Append('/');
                flags.Add(current);
            }

            return new DialogueMarkup(plain.ToString(), flags.ToArray());
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
            for (int i = 0; i < Plain.Length; i++)
            {
                bool wantBold = (Flags[i] & DialogueGlyphFlags.Bold) != 0;
                if (wantBold && !bold)
                {
                    rich.Append("<b>");
                    bold = true;
                }
                else if (!wantBold && bold)
                {
                    rich.Append("</b>");
                    bold = false;
                }

                AppendEscaped(rich, Plain[i]);
            }

            if (bold)
                rich.Append("</b>");

            return rich.ToString();
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
