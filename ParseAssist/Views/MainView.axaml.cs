using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DynamicData;
using System.ComponentModel;
using System;
using ParseAssist.ViewModels;
using Avalonia.Platform.Storage;
using System.IO;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;
using System.Numerics;
using System.Text;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using System.Linq;
using ParseAssist.Helpers;
using Avalonia.VisualTree;
using PluginLib;
using Avalonia.Controls.Shapes;
using Avalonia.ReactiveUI;
using System.Reactive.Disposables;

namespace ParseAssist.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    private SyntaxColorizer colorizer;

    ScrollViewer StringViewerScroll { get; set; }
    ScrollViewer EditorScroll { get; set; }
    ScrollViewer OffsetTableScroll { get; set; }

    private void MenuItemOpen_Clicked(object? sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        if (DataContext is MainViewModel vm && window is not null)
        {
            vm.PropertyChanged -= OnTextChanged;
            vm.OnOpenFile.Execute(window).Subscribe();
            vm.PropertyChanged += OnTextChanged;
        }

        StringViewerScroll = StringViewer.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault(defaultValue: null);
        EditorScroll = Editor.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault(defaultValue: null);
        OffsetTableScroll = OffsetTable.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault(defaultValue: null);
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.HeaderText = " Offset\t00   01   02   03   04   05   06   07   08   09   0A   0B   0C   0D   0E   0F";
            vm.StringHeaderText = "Decoded String View";
            Editor.Text = this.BuildHexView(ref vm);
            OffsetTable.Text = vm.OffsetText;
        }
    }

    private string BuildHexView(ref MainViewModel vm)
    {
        string binaryContent = vm.Content;
        StringBuilder hex = new StringBuilder(binaryContent.Length * 5);
        StringBuilder offsets = new StringBuilder();
        StringBuilder asChars = new StringBuilder();

        int i = 0;
        foreach (byte c in binaryContent)
        {
            if ((i % 16) == 0)
            {
                if (i != 0)
                {
                    hex.AppendLine();
                    asChars.AppendLine();
                }
                offsets.AppendFormat("{0:X8}", i);
                offsets.AppendLine();
            }
            
            if ((i % 16) == 15)
                hex.AppendFormat("{0:X2} ", c);
            else
                hex.AppendFormat("{0:X2}   ", c);

            // Ignore null chars
            if (c < 32 || c == 127 || c == '\n' || c == '\r' || c == '\t')
                asChars.Append('.');
            else
                asChars.Append((char)c);

            i++;
        }

        vm.OffsetText = offsets.ToString();
        StringViewer.Text = asChars.ToString();
        return hex.ToString();
    }

    private void ParsePL(string s)
    {
        try
        {
            ParseLangParser pl = new ParseLangParser(s);
            pl.UtilizeColorizer(colorizer);
        }
        catch
        {

        }
    }

    public MainView()
    {
        InitializeComponent();

        colorizer = new SyntaxColorizer();

        Editor.TextArea.TextView.ScrollOffsetChanged += EditorScrollChanged;
        OffsetTable.TextArea.TextView.ScrollOffsetChanged += OffsetTableScrollOffsetChanged;
        StringViewer.TextArea.TextView.ScrollOffsetChanged += StringViewerScrollOffsetChanged;

        OffsetTable.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden;
        OffsetTable.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden;
        Editor.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden;
        Editor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden;

        Editor.Text = "Load a binary file to start parsing!";
        Editor.TextArea.TextView.LineTransformers.Add(colorizer);

        Editor.IsReadOnly = true;
        Editor.TextArea.TextView.Margin = new Avalonia.Thickness(10, 0, 0, 0);

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel.EditorText)
                .Subscribe(debug => ParsePL(debug))
                .DisposeWith(disposables);
        });
    }

    // TODO: this is terrible tbh, but it does work... fix later
    private void StringViewerScrollOffsetChanged(object? sender, EventArgs e)
    {
        if (EditorScroll != null)
            EditorScroll.Offset = StringViewer.TextArea.TextView.ScrollOffset;

        if (OffsetTableScroll != null)
            OffsetTableScroll.Offset = StringViewer.TextArea.TextView.ScrollOffset;
    }

    private void OffsetTableScrollOffsetChanged(object? sender, EventArgs e)
    {
        if (StringViewerScroll != null)
            StringViewerScroll.Offset = OffsetTable.TextArea.TextView.ScrollOffset;

        if (EditorScroll != null)
            EditorScroll.Offset = OffsetTable.TextArea.TextView.ScrollOffset;
    }

    private void EditorScrollChanged(object? sender, EventArgs e)
    {
        if (StringViewerScroll != null)
            StringViewerScroll.Offset = Editor.TextArea.TextView.ScrollOffset;

        if (OffsetTableScroll != null)
            OffsetTableScroll.Offset = Editor.TextArea.TextView.ScrollOffset;
    }
}
