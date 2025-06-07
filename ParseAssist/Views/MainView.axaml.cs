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

namespace ParseAssist.Views;

public partial class MainView : UserControl
{
    private SyntaxColorizer colorizer;

    private void MenuItemOpen_Clicked(object? sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        if (DataContext is MainViewModel vm && window is not null)
        {
            vm.PropertyChanged -= OnTextChanged;
            vm.OnOpenFile.Execute(window).Subscribe();
            vm.PropertyChanged += OnTextChanged;
        }
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
            char x = 'x';
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

            hex.AppendFormat("{0:X2}   ", c);

            // Ignore null chars
            // TODO: Eventually add unicode char checking?
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

    private void OnAddPart()
    {
        SyntaxColorizer.LinePart part = new SyntaxColorizer.LinePart(0, 4, Brushes.Blue, SyntaxColorizer.LinePart.Area.front);
        colorizer.AppendPart(part);

        part = new SyntaxColorizer.LinePart(5, 8, Brushes.Red, SyntaxColorizer.LinePart.Area.back);
        colorizer.AppendPart(part);
    }

    public MainView()
    {
        InitializeComponent();

        // TODO, use scroll from AvaloniaEdit instead of default ScrollViewer

        colorizer = new SyntaxColorizer();

        Editor.Text = "Load a binary file to start parsing!";
        OffsetTable.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden;
        OffsetTable.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden;

        Editor.TextArea.TextView.LineTransformers.Add(colorizer);

        Editor.IsReadOnly = true;
        Editor.TextArea.TextView.Margin = new Avalonia.Thickness(10, 0, 0, 0);
    }
}
