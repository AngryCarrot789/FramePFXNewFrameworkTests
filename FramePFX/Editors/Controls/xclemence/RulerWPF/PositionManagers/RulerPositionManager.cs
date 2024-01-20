//  
// Copyright (c) Xavier CLEMENCE (xavier.clemence@gmail.com) and REghZy/AngryCarrot789. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information. 
// Ruler Wpf Version 3.1
// 

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using FramePFX.Utils;
using Microsoft.SqlServer.Server;

namespace FramePFX.Editors.Controls.xclemence.RulerWPF.PositionManagers {
    public abstract class RulerPositionManager {
        public static readonly Typeface FallbackTypeFace = new Typeface("Consolas");

        public RulerBase Control { get; }

        protected double cachedValue;
        protected GlyphRun cachedRun;
        protected FormattedText cachedText;

        protected RulerPositionManager(RulerBase control) {
            this.Control = control;
        }

        #region Rendering Functions

        public abstract void DrawMajorLine(DrawingContext dc, double offset);

        public abstract void DrawMinorLine(DrawingContext dc, double offset);

        public abstract void DrawText(DrawingContext dc, double value, double offset);

        protected FormattedText GetFormattedText(double value) {
            if (this.cachedText != null && Maths.Equals(value, this.cachedValue)) {
                return this.cachedText;
            }

            Typeface typeface = this.Control.CachedTypeFace;
            if (typeface == null) {
                FontFamily font = this.Control.FontFamily ?? (this.Control.FontFamily = new FontFamily("Consolas"));
                ICollection<Typeface> typefaces = font.GetTypefaces();
                this.Control.CachedTypeFace = typeface = typefaces.FirstOrDefault() ?? FallbackTypeFace;
            }

            CultureInfo culture = this.Control.TextCulture ?? CultureInfo.CurrentUICulture;
            string text = value.ToString(this.Control.TextFormat, culture);
            return this.cachedText = new FormattedText(text, culture, FlowDirection.LeftToRight, typeface, 12, this.Control.Foreground, 96);
        }

        protected GlyphRun GetTextRun(double value, Point origin) {
            if (this.cachedRun != null && Maths.Equals(value, this.cachedValue)) {
                return this.cachedRun;
            }

            CultureInfo culture = this.Control.TextCulture ?? CultureInfo.CurrentUICulture;
            string text = value.ToString(this.Control.TextFormat, culture);
            FontFamily font = this.Control.FontFamily ?? (this.Control.FontFamily = new FontFamily("Consolas"));
            ICollection<Typeface> typefaces = font.GetTypefaces();
            return this.cachedRun = GlyphGenerator.CreateText(text, 12, typefaces.FirstOrDefault() ?? FallbackTypeFace, origin);
        }

        #endregion

        #region Size Functions

        public abstract double GetSize();

        public abstract double GetHeight();

        public abstract double GetMajorSize();

        #endregion

        public abstract bool OnUpdateMakerPosition(Line marker, Point position);
    }
}