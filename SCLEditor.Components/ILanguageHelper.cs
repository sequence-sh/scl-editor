namespace Reductech.Utilities.SCLEditor.Components;

public interface ILanguageHelper : IDisposable
{
    public Task OnInitializedAsync(Editor editor);

    public Task OnAfterRenderAsync(bool firstRender);

    public void OnDidChangeModelContent();
}
