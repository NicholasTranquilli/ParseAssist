using Avalonia.Controls;
using ParseAssist.Views;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PluginLib;
using Avalonia.Platform.Storage;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

namespace ParseAssist.ViewModels;

public class MainViewModel : ViewModelBase, IPluginWindow
{
    // For IPluginWindow
    public string PluginName => "ParseAssist";

    private string _debug = "TEST FAILED";
    public string DEBUG
    {
        get => _debug;
        set => this.RaiseAndSetIfChanged(ref _debug, value);
    }

    private HostToPluginData _pluginData;
    public HostToPluginData PluginData
    {
        get => _pluginData;
        set
        {
            OnDataUpdate(ref _pluginData, value);
            _pluginData.PropertyChanged += OnPluginDataChanged;
        }
    }

    private void OnPluginDataChanged(object sender, PropertyChangedEventArgs e)
    {
        this.DEBUG = PluginData.EditorText.ToString();
    }

    public void OnDataUpdate(ref HostToPluginData _data, HostToPluginData data)
    {
        // TODO: This function is kinda irrelevant, try to remove later
        _data = data;
        this.DEBUG = _data.EditorText.ToString();
    }

    public Window CreateWindow(HostToPluginData data)
    {
        return new Window
        {
            Title = "ParseAssist Plugin",
            Width = 800,
            Height = 450,
            DataContext = new MainViewModel(data),
            Content = new MainView()
        };
    }

    // End IPluginWindow

    private string _filePath;
    public string FilePath
    {
        get => _filePath;
        set => this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    private string _headerText;
    public string HeaderText
    {
        get => _headerText;
        set => this.RaiseAndSetIfChanged(ref _headerText, value);
    }

    private string _stringHeaderText;
    public string StringHeaderText
    {
        get => _stringHeaderText;
        set => this.RaiseAndSetIfChanged(ref _stringHeaderText, value);
    }

    private string _offsetText;
    public string OffsetText
    {
        get => _offsetText;
        set => this.RaiseAndSetIfChanged(ref _offsetText, value);
    }

    private string _lineCount;
    public string LineCount
    {
        get => _lineCount;
        set => this.RaiseAndSetIfChanged(ref _lineCount, value);
    }

    private string _content;
    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                this.RaiseAndSetIfChanged(ref _content, value);
            }
        }
    }

    public ReactiveCommand<Window, Unit> OnOpenFile { get; }
    private async Task OpenFile(Window window)
    {
        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);

            this.Content = reader.ReadToEnd();
            this.FilePath = files[0].Path.AbsolutePath;
        }
    }

    public MainViewModel()
    {
        OnOpenFile = ReactiveCommand.CreateFromTask<Window>(OpenFile);
    }

    public MainViewModel(HostToPluginData data)
    {
        this.PluginData = data;
    }
}
