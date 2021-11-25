﻿using BlazorDownloadFile;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Forms;

namespace Reductech.Utilities.SCLEditor.Blazor
{

public class CompoundFileSystem
{
    public CompoundFileSystem(
        ILocalStorageService localStorage,
        IBlazorDownloadFileService blazorDownloadFileService)
    {
        LocalStorage              = localStorage;
        BlazorDownloadFileService = blazorDownloadFileService;
    }

    public ILocalStorageService LocalStorage { get; }

    IBlazorDownloadFileService BlazorDownloadFileService { get; }

    public MockFileSystem FileSystem { get; } = new();

    private bool Initialized = false;

    public async Task Initialize()
    {
        if (Initialized)
            return;

        Initialized = true;

        var length = await LocalStorage.LengthAsync();

        for (var i = 0; i < length; i++)
        {
            var key = await LocalStorage.KeyAsync(i);

            if (key != EditorConfiguration.ConfigurationKey)
            {
                var text = await LocalStorage.GetItemAsync<string>(key);
                FileSystem.AddFile(key, text);
            }
        }
    }

    public FileData SelectedFile { get; set; }

    public async Task Download(string fileName)
    {
        var text = FileSystem.GetFile(fileName).TextContents;
        await BlazorDownloadFileService.DownloadFileFromText(fileName, text, "text/plain");
    }

    public async Task ImportFiles(IEnumerable<IBrowserFile> files)
    {
        foreach (var browserFile in files)
        {
            using var reader = new StreamReader(browserFile.OpenReadStream());

            var text = await reader.ReadToEndAsync();
            var mfd  = new MockFileData(text);

            FileSystem.AddFile(browserFile.Name, mfd);
            await LocalStorage.SetItemAsync(browserFile.Name, text);
        }
    }

    public async Task SaveFile(string path, string text)
    {
        FileSystem.AddFile(path, text);
        await LocalStorage.SetItemAsync(path, text);
    }

    public bool FilesExist()
    {
        return FileSystem.AllFiles.Any();
    }

    public async Task DeleteFile(string path)
    {
        FileSystem.RemoveFile(path);
        await LocalStorage.RemoveItemAsync(path);
    }

    public async Task ClearFiles()
    {
        foreach (var file in FileSystem.AllFiles.ToList())
        {
            FileSystem.RemoveFile(file);
        }

        await LocalStorage.ClearAsync();
    }

    public IEnumerable<FileData> GetFileData()
    {
        foreach (var file in FileSystem.AllFiles)
        {
            var mfd = FileSystem.GetFile(file);

            yield return new FileData(file, mfd);
        }
    }

    /// <summary>
    /// Save a file from an editor
    /// </summary>
    public async Task SaveFile(MonacoEditor editor, string title)
    {
        var text = await editor.GetValue();
        await SaveFile(title, text);
    }
}

}