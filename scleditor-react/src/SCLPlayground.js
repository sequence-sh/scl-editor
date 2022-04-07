import { useEffect, useState } from 'react';
import { useBlazor } from './blazor-react';

const playgroundId = 'scl-playground';

function addScript(src, callback) {
  const script = document.createElement('script');
  script.src = src;
  if (callback) {
    script.onload = callback;
  }
  document.body.appendChild(script);
}

export function SCLPlayground() {
  const [scriptsLoaded, setScriptsLoaded] = useState(false);
  const [blazorLoaded, setBlazorLoaded] = useState(false);

  useEffect(() => {
    if (!scriptsLoaded) {
      addScript('_content/MudBlazor/MudBlazor.min.js');
      addScript('_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js', () => {
        require.config({ paths: { vs: '_content/BlazorMonaco/lib/monaco-editor/min/vs' } });
        addScript('_content/BlazorMonaco/jsInterop.js', () =>
          addScript('_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js', () =>
            addScript('_content/SCLEditor.Util/DefineSCLLanguage.js', () => setScriptsLoaded(true))
          )
        );
      });
    }
  }, [scriptsLoaded]);

  useEffect(() => {
    if (!scriptsLoaded) {
      return;
    }
    if (!blazorLoaded) {
      const script = document.createElement('script');
      script.src = '_framework/blazor.webassembly.js';
      script.setAttribute('autostart', false);
      document.body.appendChild(script);
      // eslint-disable-next-line no-undef
      script.onload = () => Blazor.start().then(() => setBlazorLoaded(true));
    }
  }, [scriptsLoaded, blazorLoaded]);

  const fragment = useBlazor('scl-playground', playgroundId, {});

  let content =
    scriptsLoaded && blazorLoaded ? <div id={playgroundId}>{fragment}</div> : <div>Loading...</div>;

  return content;
}
