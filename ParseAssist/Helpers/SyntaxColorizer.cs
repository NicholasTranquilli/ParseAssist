using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParseAssist.Helpers
{
    public class SyntaxColorizer : DocumentColorizingTransformer
    {
        public struct LinePart
        {
            public enum Area
            {
                front,
                back,
            }

            public int offStart;
            public int offEnd;
            public IImmutableSolidColorBrush color;
            public Area area;

            public LinePart(int _offStart, int _offEnd, IImmutableSolidColorBrush _color, Area _area)
            {
                this.offStart = _offStart;
                this.offEnd = _offEnd;
                this.color = _color;
                this.area = _area;
            }
        }

        private List<LinePart> parts;

        protected override void ColorizeLine(DocumentLine line)
        {
            string text = CurrentContext.Document.GetText(line);

            int startOffset = line.Offset;

            foreach (var part in parts)
            {
                ChangeLinePart(startOffset + part.offStart, startOffset + part.offStart + part.offEnd, element =>
                {
                    if (part.area == LinePart.Area.front)
                        element.TextRunProperties.SetForegroundBrush(part.color);
                    else if (part.area == LinePart.Area.back)
                        element.TextRunProperties.SetBackgroundBrush(part.color);
                });
            }
        }

        public SyntaxColorizer()
        {
            parts = new List<LinePart>();
        }
        
        public void AppendPart(LinePart part)
        {
            parts.Add(part);
        }
    }
}
