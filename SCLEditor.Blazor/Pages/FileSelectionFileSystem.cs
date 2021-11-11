//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.IO;
//using System.Reflection;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Xml.Schema;
//using Reductech.EDR.Core;
//using Reductech.EDR.Core.Abstractions;
//using Reductech.EDR.Core.Enums;
//using Reductech.EDR.Core.ExternalProcesses;
//using Thinktecture;
//using Thinktecture.IO;
//using Thinktecture.Text;

//namespace Reductech.Utilities.SCLEditor.Blazor.Pages
//{

//public record BrowserFile(
//    string Name,
//    StringStream Text,
//    string MimeType,
//    DateTimeOffset Modified,
//    long Size);

//public record FileSelectionFileSystem
//    (ConcurrentDictionary<string, BrowserFile> Dictionary) : IFileSystem, IDirectory, IFile
//{
//    /// <inheritdoc />
//    public IDirectory Directory => this;

//    /// <inheritdoc />
//    public IFile File => this;

//    /// <inheritdoc />
//    public ICompression Compression => throw new Exception("Cannot use compression in this editor");

//    /// <inheritdoc />
//    public IDirectoryInfo CreateDirectory(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public Task<string>
//        ReadAllTextAsync(string path, CancellationToken cancellationToken = new()) =>
//        ReadAllTextAsync(path, Encoding.Default, cancellationToken);

//    /// <inheritdoc />
//    public async Task<string> ReadAllTextAsync(
//        string path,
//        IEncoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        await Task.CompletedTask;
//        return ReadAllText(path, encoding);
//    }

//    /// <inheritdoc />
//    public Task<string> ReadAllTextAsync(
//        string path,
//        Encoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        return ReadAllTextAsync(path, encoding.ToInterface()!, cancellationToken);
//    }

//    /// <inheritdoc />
//    public async Task WriteAllTextAsync(
//        string path,
//        string content,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task WriteAllTextAsync(
//        string path,
//        string content,
//        IEncoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task WriteAllTextAsync(
//        string path,
//        string content,
//        Encoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task<byte[]> ReadAllBytesAsync(
//        string path,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task WriteAllBytesAsync(
//        string path,
//        byte[] bytes,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task<string[]> ReadAllLinesAsync(
//        string path,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task<string[]> ReadAllLinesAsync(
//        string path,
//        IEncoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task<string[]> ReadAllLinesAsync(
//        string path,
//        Encoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task WriteAllLinesAsync(
//        string path,
//        IEnumerable<string> contents,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task WriteAllLinesAsync(
//        string path,
//        IEnumerable<string> contents,
//        IEncoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task WriteAllLinesAsync(
//        string path,
//        IEnumerable<string> contents,
//        Encoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task AppendAllLinesAsync(
//        string path,
//        IEnumerable<string> contents,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task AppendAllLinesAsync(
//        string path,
//        IEnumerable<string> contents,
//        IEncoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task AppendAllLinesAsync(
//        string path,
//        IEnumerable<string> contents,
//        Encoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task AppendAllTextAsync(
//        string path,
//        string content,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task AppendAllTextAsync(
//        string path,
//        string content,
//        IEncoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public async Task AppendAllTextAsync(
//        string path,
//        string content,
//        Encoding encoding,
//        CancellationToken cancellationToken = new())
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void AppendAllLines(string path, IEnumerable<string> contents)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void AppendAllLines(string path, IEnumerable<string> contents, IEncoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void AppendAllText(string path, string contents)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void AppendAllText(string path, string contents, IEncoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void AppendAllText(string path, string contents, Encoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IStreamWriter AppendText(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void Copy(string sourceFileName, string destFileName)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void Copy(string sourceFileName, string destFileName, bool overwrite)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IFileStream Create(string path)
//    {
//        return Create(path, 4096);
//    }

//    /// <inheritdoc />
//    public IFileStream Create(string path, int bufferSize)
//    {
//        return Create(path, bufferSize, FileOptions.None);
//    }

//    /// <inheritdoc />
//    public IFileStream Create(string path, int bufferSize, FileOptions options)
//    {
//        var stream1    = new MemoryStream();
//        var fileStream = new FakeFileStreamAdapter(stream1);
//        var stream2    = new MemoryStream();

//        var copier = new AsyncStreamCopier(stream1, stream2, bufferSize);

//        copier.Completed += (s, e) =>

//        {
//            Dictionary[path] = new BrowserFile(
//                path,
//                new StringStream(stream2, EncodingEnum.UTF8),
//                "text/plain",
//                DateTimeOffset.Now,
//                stream2.Length
//            );
//        };

//        copier.Start();

//        return fileStream;
//    }

//    private class AsyncStreamCopier
//    {
//        public event EventHandler Completed;

//        private readonly Stream _input;
//        private readonly Stream _output;

//        private readonly byte[] _buffer;

//        public AsyncStreamCopier(Stream input, Stream output, int bufferSize)
//        {
//            _input  = input;
//            _output = output;
//            _buffer = new byte[bufferSize];
//        }

//        public void Start()
//        {
//            GetNextChunk();
//        }

//        private void GetNextChunk()
//        {
//            _input.BeginRead(_buffer, 0, _buffer.Length, InputReadComplete, null);
//        }

//        private void InputReadComplete(IAsyncResult ar)
//        {
//            if (!_input.CanRead)
//            {
//                var bufferField =
//                    _input.GetType()
//                        .GetField(
//                            "_buffer",
//                            BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance
//                        );

//                var lengthField =
//                    _input.GetType()
//                        .GetField(
//                            "_length",
//                            BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance
//                        );

//                byte[] dBuffer = (byte[])bufferField.GetValue(_input);
//                int    len     = (int)lengthField.GetValue(_input);

//                _output.Write(dBuffer, 0, len);

//                RaiseCompleted();
//                return;
//            }

//            // input read asynchronously completed
//            var bytesRead = _input.EndRead(ar);

//            if (bytesRead == 0)
//            {
//                RaiseCompleted();
//                return;
//            }

//            // write synchronously
//            _output.Write(_buffer, 0, bytesRead);

//            // get next
//            GetNextChunk();
//        }

//        private void RaiseCompleted()
//        {
//            if (Completed != null)
//            {
//                Completed(this, EventArgs.Empty);
//            }
//        }
//    }

//    /// <inheritdoc />
//    public IStreamWriter CreateText(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IFile.Delete(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void Decrypt(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void Encrypt(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    bool IFile.Exists(string path)
//    {
//        return Dictionary.ContainsKey(path);
//    }

//    /// <inheritdoc />
//    public FileAttributes GetAttributes(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IFile.GetCreationTime(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IFile.GetCreationTimeUtc(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IFile.GetLastAccessTime(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IFile.GetLastAccessTimeUtc(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IFile.GetLastWriteTime(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IFile.GetLastWriteTimeUtc(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IFile.Move(string sourceFileName, string destFileName)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IFileStream Open(string path, FileMode mode)
//    {
//        return Open(path, mode, FileAccess.Read, FileShare.Read);
//    }

//    /// <inheritdoc />
//    public IFileStream Open(string path, FileMode mode, FileAccess access)
//    {
//        return Open(path, mode, access, FileShare.Read);
//    }

//    /// <inheritdoc />
//    public IFileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
//    {
//        return new FakeFileStreamAdapter(Dictionary[path].Text.GetString());
//    }

//    /// <inheritdoc />
//    public IFileStream OpenRead(string path)
//    {
//        return Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
//    }

//    /// <inheritdoc />
//    public IStreamReader OpenText(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IFileStream OpenWrite(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public byte[] ReadAllBytes(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] ReadAllLines(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] ReadAllLines(string path, IEncoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] ReadAllLines(string path, Encoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string ReadAllText(string path)
//    {
//        return ReadAllText(path, Encoding.Default);
//    }

//    /// <inheritdoc />
//    public string ReadAllText(string path, IEncoding encoding)
//    {
//        return Dictionary[path].Text.GetString();
//    }

//    /// <inheritdoc />
//    public string ReadAllText(string path, Encoding encoding)
//    {
//        return ReadAllText(path, encoding.ToInterface()!);
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> ReadLines(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> ReadLines(string path, IEncoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> ReadLines(string path, Encoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void SetAttributes(string path, FileAttributes fileAttributes)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IFile.SetCreationTime(string path, DateTime creationTime)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IFile.SetCreationTimeUtc(string path, DateTime creationTimeUtc)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IFile.SetLastAccessTime(string path, DateTime lastAccessTime)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IFile.SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IFile.SetLastWriteTime(string path, DateTime lastWriteTime)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IFile.SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void WriteAllBytes(string path, byte[] bytes)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void WriteAllLines(string path, string[] contents)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void WriteAllLines(string path, string[] contents, Encoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void WriteAllLines(string path, string[] contents, IEncoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void WriteAllLines(string path, IEnumerable<string> contents)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void WriteAllLines(string path, IEnumerable<string> contents, IEncoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void WriteAllText(string path, string contents)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void WriteAllText(string path, string contents, IEncoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void WriteAllText(string path, string contents, Encoding encoding)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void Replace(
//        string sourceFileName,
//        string destinationFileName,
//        string destinationBackupFileName)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void Replace(
//        string sourceFileName,
//        string destinationFileName,
//        string destinationBackupFileName,
//        bool ignoreMetadataErrors)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IDirectory.Delete(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void Delete(string path, bool recursive)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateDirectories(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateDirectories(
//        string path,
//        string searchPattern,
//        SearchOption searchOption)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateFiles(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateFiles(
//        string path,
//        string searchPattern,
//        SearchOption searchOption)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateFileSystemEntries(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateFileSystemEntries(
//        string path,
//        string searchPattern,
//        SearchOption searchOption)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetLogicalDrives()
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    bool IDirectory.Exists(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IDirectory.GetCreationTime(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IDirectory.GetCreationTimeUtc(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string GetCurrentDirectory()
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetDirectories(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetDirectories(string path, string searchPattern)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string GetDirectoryRoot(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetFiles(
//        string path,
//        string searchPattern,
//        EnumerationOptions enumerationOptions)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetDirectories(
//        string path,
//        string searchPattern,
//        EnumerationOptions enumerationOptions)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetFileSystemEntries(
//        string path,
//        string searchPattern,
//        EnumerationOptions enumerationOptions)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateDirectories(
//        string path,
//        string searchPattern,
//        EnumerationOptions enumerationOptions)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateFiles(
//        string path,
//        string searchPattern,
//        EnumerationOptions enumerationOptions)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IEnumerable<string> EnumerateFileSystemEntries(
//        string path,
//        string searchPattern,
//        EnumerationOptions enumerationOptions)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetFiles(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetFiles(string path, string searchPattern)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetFileSystemEntries(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetFileSystemEntries(string path, string searchPattern)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public string[] GetFileSystemEntries(
//        string path,
//        string searchPattern,
//        SearchOption searchOption)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IDirectory.GetLastAccessTime(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IDirectory.GetLastAccessTimeUtc(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IDirectory.GetLastWriteTime(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    DateTime IDirectory.GetLastWriteTimeUtc(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public IDirectoryInfo GetParent(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IDirectory.Move(string sourceDirName, string destDirName)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IDirectory.SetCreationTime(string path, DateTime creationTime)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IDirectory.SetCreationTimeUtc(string path, DateTime creationTimeUtc)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    public void SetCurrentDirectory(string path)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IDirectory.SetLastAccessTime(string path, DateTime lastAccessTime)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IDirectory.SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IDirectory.SetLastWriteTime(string path, DateTime lastWriteTime)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc />
//    void IDirectory.SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
//    {
//        throw new NotImplementedException();
//    }
//}

//}


