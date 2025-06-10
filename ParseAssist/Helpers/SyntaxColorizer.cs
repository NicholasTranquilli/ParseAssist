using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace ParseAssist.Helpers
{
    //public class SyntaxColorizer : ColorizingTransformer
    //{
    //    public struct LinePart
    //    {
    //        public enum Area
    //        {
    //            front,
    //            back,
    //        }

    //        public int offStart;
    //        public int offEnd;
    //        public IImmutableSolidColorBrush color;
    //        public Area area;

    //        public LinePart(int _offStart, int _offEnd, IImmutableSolidColorBrush _color, Area _area)
    //        {
    //            this.offStart = _offStart;
    //            this.offEnd = _offEnd;
    //            this.color = _color;
    //            this.area = _area;
    //        }
    //    }

    //    private List<LinePart> parts;

    //    protected override void Colorize(ITextRunConstructionContext context)
    //    {
    //        int startOffset = context.VisualLine.FirstDocumentLine.Offset;
    //        int endOffset = context.VisualLine.LastDocumentLine.EndOffset;

    //        string text = context.Document.GetText(startOffset, endOffset - startOffset);

    //        var vl = context.VisualLine;
    //        var visualStart = vl.GetVisualColumn(startOffset);
    //        var visualEnd = vl.GetVisualColumn(endOffset - startOffset);

    //        //for (int i = 0; i < parts.Count; i++) { 
    //        //    var part = parts[i];
    //        //    CurrentElements[i].Split(startOffset + part.);
    //        //}

    //        for (int i = 0; i < CurrentElements.Count; i++)
    //        {
    //            if (CurrentElements[i].VisualColumn + 5 > 30) break;
    //            CurrentElements[i].Split(CurrentElements[i].VisualColumn + 5, CurrentElements, i);
    //        }

    //        int x = 0;
    //        foreach (var part in CurrentElements)
    //            if ((++x % 2) == 0)
    //                part.TextRunProperties.SetBackgroundBrush(Brushes.Red);

    //        //foreach (var part in parts)
    //        //{
    //        //    try
    //        //    {
    //        //        ChangeVisualElements(visualStart, visualEnd, action =>
    //        //        {
    //        //            if (part.area == LinePart.Area.front)
    //        //                action.TextRunProperties.SetForegroundBrush(part.color);
    //        //            else if (part.area == LinePart.Area.back)
    //        //                action.TextRunProperties.SetBackgroundBrush(part.color);
    //        //        });
    //        //    }
    //        //    catch { }
    //        //}
    //    }

    //    public SyntaxColorizer()
    //    {
    //        parts = new List<LinePart>();
    //    }

    //    public void ClearParts()
    //    {
    //        parts.Clear();
    //    }

    //    public void AppendPart(LinePart part)
    //    {
    //        parts.Add(part);
    //    }
    //}

    public class SyntaxColorizer : DocumentColorizingTransformer
    {
        public struct LinePart
        {
            public enum Area
            {
                front,
                back,
            }

            public int offset;
            public int size;
            public IImmutableSolidColorBrush color;
            public Area area;

            public LinePart(int _offStart, int _offEnd, IImmutableSolidColorBrush _color, Area _area)
            {
                this.offset = _offStart;
                this.size = _offEnd;
                this.color = _color;
                this.area = _area;
            }
        }

        private List<LinePart> parts;

        private (int, int) IsWithin(DocumentLine line, LinePart part)
        {
            int ResultStart = -1;
            int ResultEnd = -1;

            int DocumentStartOffset = 0;

            int LineStartOffset = line.Offset;
            int LineEndOffset = line.Offset + line.Length;

            // Fix offsets
            part.offset *= 5;

            part.size *= 5;
            part.size -= 3;

            // Check bounds

            int PartStartOffset = DocumentStartOffset + part.offset;
            int PartEndOffset = PartStartOffset + part.size;

            if (PartEndOffset > LineStartOffset && PartStartOffset < LineEndOffset)
            {
                ResultStart = Math.Max(PartStartOffset, LineStartOffset);
                ResultEnd = Math.Min(PartEndOffset, LineEndOffset);
            }

            // Check for impossible
            if (ResultEnd < ResultStart)
                return (-1, -1);

            return (ResultStart, ResultEnd);
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int currentLineNumber = line.LineNumber;

            int startOffset = line.Offset;
            int maxOffset = line.EndOffset;

            int WithinStart = 0;
            int WithinEnd = 0;

            foreach (var part in parts)
            {
                (WithinStart, WithinEnd) = IsWithin(line, part);

                if (WithinStart >= 0 && WithinEnd >= 0)
                {
                    ChangeLinePart(WithinStart, WithinEnd, element =>
                    {
                        if (part.area == LinePart.Area.front)
                            element.TextRunProperties.SetForegroundBrush(part.color);
                        else if (part.area == LinePart.Area.back)
                            element.TextRunProperties.SetBackgroundBrush(part.color);
                    });
                }
            }
        }

        public SyntaxColorizer()
        {
            parts = new List<LinePart>();
        }

        public void ClearParts()
        {
            parts.Clear();
        }

        public void AppendPart(LinePart part)
        {
            parts.Add(part);
        }
    }
}

// IBackgroundRenderer

//public class SyntaxColorizer : IBackgroundRenderer
//{
//    KnownLayer IBackgroundRenderer.Layer => KnownLayer.Background;

//    public void AddHighlight(int startOffset, int length, string color)
//    {
//        parts.Add(new LinePart
//        {
//            StartOffset = startOffset,
//            Length = length,
//            color = ,
//        });
//    }

//    public class LinePart : TextSegment
//    {
//        public enum Area
//        {
//            front,
//            back,
//        }

//        public int offStart;
//        public int offEnd;
//        public IImmutableSolidColorBrush color;
//        public Area area;

//        public LinePart(int _offStart, int _offEnd, IImmutableSolidColorBrush _color, Area _area)
//        {
//            this.offStart = _offStart;
//            this.offEnd = _offEnd;
//            this.color = _color;
//            this.area = _area;
//        }
//    }

//    private List<LinePart> parts = new();

//    public void Clear() => parts.Clear();

//    public void Draw(TextView textView, DrawingContext drawingContext)
//    {
//        if (!parts.Any())
//            return;

//        textView.EnsureVisualLines();

//        foreach (var part in parts)
//        {
//            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, part))
//            {
//                drawingContext.FillRectangle(part.color, rect);
//            }
//        }
//    }
//    public SyntaxColorizer()
//    {
//        parts = new List<LinePart>();
//    }

//    public void ClearParts()
//    {
//        parts.Clear();
//    }

//    public void AppendPart(LinePart part)
//    {
//        parts.Add(part);
//    }
//}