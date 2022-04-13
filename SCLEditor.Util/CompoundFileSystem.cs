using System.Collections.Specialized;
using BlazorDownloadFile;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Forms;

namespace Reductech.Utilities.SCLEditor.Util;

public class CompoundFileSystem : INotifyCollectionChanged
{
    /// <summary>
    /// Create a new FileSystem using the provided local storage and download services.
    /// </summary>
    public CompoundFileSystem(
        ILocalStorageService localStorage,
        IBlazorDownloadFileService blazorDownloadFileService)
    {
        LocalStorage              = localStorage;
        BlazorDownloadFileService = blazorDownloadFileService;
    }

    /// <summary>
    /// The file prefix used to differentiated files in the browser file system.
    /// </summary>
    public const string FilePrefix = "SCLPlaygroundFile-";

    /// <summary>
    /// The FileSystem to use for storing and accessing file metadata
    /// </summary>
    public MockFileSystem FileSystem { get; } = new();

    /// <summary>
    /// The file currently selected
    /// </summary>
    public FileData? SelectedFile { get; set; }

    private ILocalStorageService LocalStorage { get; }

    private IBlazorDownloadFileService BlazorDownloadFileService { get; }

    private bool _initialized = false;

    /// <summary>
    /// Create a new local file system and load all files persisted in the browser storage
    /// </summary>
    public async Task Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        var length = await LocalStorage.LengthAsync();

        for (var i = 0; i < length; i++)
        {
            var key = await LocalStorage.KeyAsync(i);

            if (key.StartsWith(FilePrefix))
            {
                var text = await LocalStorage.GetItemAsync<string>(key);

                FileSystem.AddFile(
                    key.Substring(FilePrefix.Length),
                    new MockFileData(text) { LastWriteTime = DateTimeOffset.Now }
                );
            }
        }
    }

    /// <summary>
    /// Download file from the browser storage
    /// </summary>
    public async Task Download(string fileName)
    {
        var text = FileSystem.GetFile(FilePrefix + fileName).TextContents;

        await BlazorDownloadFileService.DownloadFileFromText(
            fileName,
            text,
            Encoding.UTF8,
            "text/plain"
        );
    }

    /// <summary>
    /// Import files from an import file dialog
    /// </summary>
    public async Task ImportFiles(IEnumerable<IBrowserFile> files)
    {
        var newFiles = new List<string>();

        foreach (var browserFile in files)
        {
            using var reader = new StreamReader(browserFile.OpenReadStream());

            var text = await reader.ReadToEndAsync();
            var mfd  = new MockFileData(text);

            FileSystem.AddFile(browserFile.Name, mfd);
            await LocalStorage.SetItemAsync(FilePrefix + browserFile.Name, text);
            newFiles.Add(browserFile.Name);
        }

        OnCollectionChanged(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newFiles)
        );
    }

    /// <summary>
    /// Save file to browser storage
    /// </summary>
    public async Task SaveFile(string path, string text)
    {
        FileSystem.AddFile(path, new MockFileData(text) { LastWriteTime = DateTimeOffset.Now });
        await LocalStorage.SetItemAsync(FilePrefix + path.TrimStart('/'), text);

        OnCollectionChanged(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, path)
        );
    }

    /// <summary>
    /// Save a file from an editor
    /// </summary>
    public async Task SaveFile(MonacoEditor editor, string title)
    {
        var text = await editor.GetValue();
        await SaveFile(title, text);
    }

    /// <summary>
    /// True if any files exist in the file system.
    /// </summary>
    public bool FilesExist() => FileSystem.AllFiles.Any();

    /// <summary>
    /// Delete file from the file system and browser storage
    /// </summary>
    public async Task DeleteFile(string path)
    {
        FileSystem.RemoveFile(path);
        await LocalStorage.RemoveItemAsync(FilePrefix + path.TrimStart('/'));

        OnCollectionChanged(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, path)
        );
    }

    /// <summary>
    /// Remove all files from the file system and browser storage
    /// </summary>
    public async Task ClearFiles()
    {
        foreach (var file in FileSystem.AllFiles.ToList())
        {
            FileSystem.RemoveFile(file);
        }

        await LocalStorage.ClearAsync();

        OnCollectionChanged(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
        );
    }

    /// <summary>
    /// Get all files stored in the file system
    /// </summary>
    public IEnumerable<FileData> GetFileData()
    {
        foreach (var file in FileSystem.AllFiles.Select(f => f.TrimStart('/')))
        {
            var mfd = FileSystem.GetFile(file);

            yield return new FileData(file, mfd);
        }
    }

    /// <summary>
    /// Events fired when files are imported/saved/deleted/cleared.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);
}
