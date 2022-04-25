Remove-Item -rec -for ./publish, ./scleditor-react/public/_content, ./scleditor-react/public/_framework -ErrorAction SilentlyContinue
dotnet publish --output ./publish ./SCLEditor.React/SCLEditor.React.csproj
Move-Item ./publish/wwwroot/_content, ./publish/wwwroot/_framework ./scleditor-react/public/ -ErrorAction Stop
Remove-Item -rec -for ./publish
Push-Location ./scleditor-react
npm install
try { npm run start } finally { Pop-Location }
