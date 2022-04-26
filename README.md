# SCL Editor

Sequence Configuration Language in-browser editor.

Built using [Blazor Monaco](https://github.com/serdarciplak/BlazorMonaco),
a wrapper around Microsoft's
[Monaco editor](https://github.com/microsoft/monaco-editor) that powers vscode.

## Running/Testing React Components

Build/publish the `SCLEditor.React` project, copy it to `scleditor-react` app
and run:

```powershell
Remove-Item -rec -for ./publish, ./scleditor-react/public/_content, ./scleditor-react/public/_framework -ErrorAction SilentlyContinue
dotnet publish --output ./publish ./SCLEditor.React/SCLEditor.React.csproj
Move-Item ./publish/wwwroot/_content, ./publish/wwwroot/_framework ./scleditor-react/public/ -ErrorAction Stop
Remove-Item -rec -for ./publish
cd ./scleditor-react
npm install
npm run start
```

or just run the `.\run-react.ps1` script.

## Ahead-of-Time Compilation

Setting `RunAOTCompilation` to enabled for `SCLEditor.React` publishes
a `Release` bundle of 192MB vs 74MB for the standard bundle. Compile
time also increases 10x so it's not used at the moment.
