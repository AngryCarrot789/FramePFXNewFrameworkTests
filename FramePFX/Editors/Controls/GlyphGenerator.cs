using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FramePFX.Editors.Controls {
    public class GlyphGenerator {
        public static GlyphRun CreateText(string text, double fontSize, Control control) {
            return CreateText(text, fontSize, control, new Point(0, fontSize));
        }

        public static GlyphRun CreateText(string text, double fontSize, Control control, Point origin) {
            Typeface typeface = new Typeface(control.FontFamily, control.FontStyle, control.FontWeight, control.FontStretch);
            return CreateText(text, fontSize, typeface, origin);
        }

        public static GlyphRun CreateText(string text, double fontSize, Typeface typeface, Point origin) {
            if (!typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
                throw new InvalidOperationException("No glyph typeface found");

            ushort[] indices = new ushort[text.Length];
            double[] widths = new double[text.Length];
            for (int i = 0, count = text.Length; i < count; i++) {
                ushort idx = (ushort) (text[i] - 29);
                indices[i] = idx;
                widths[i] = glyphTypeface.AdvanceWidths[idx] * fontSize;
            }

            return new GlyphRun(glyphTypeface, 0, false, fontSize, indices, origin, widths, null, null, null, null, null, null);
        }
    }
}