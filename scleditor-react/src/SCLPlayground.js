import React from 'react';

class SCLPlayground extends React.Component {
  static playgroundId = 'scl-playground-root';
  static componentName = 'scl-playground-react';

  constructor(props) {
    super(props);
    this.state = {
      loading: true,
    };
  }

  componentDidMount() {
    // eslint-disable-next-line no-undef
    if (typeof Blazor === 'undefined' || Blazor == null) {
      SCLPlayground.blazorInit(() => this.setState({ loading: false }));
    } else {
      SCLPlayground.playgroundInit(SCLPlayground.componentName, {});
    }
  }

  componentWillUnmount() {
    if (window.sclPlaygroundComponent) {
      window.sclPlaygroundComponent.then((component) => component.dispose());
      window.sclPlaygroundComponent = null;
      this.setState({ loading: true });
    }
  }

  // componentDidCatch() {}

  render() {
    const inner = this.state.loading ? 'Loading...' : '';
    return <div id={SCLPlayground.playgroundId}>{inner}</div>;
  }

  static addScript(src, attributes = {}) {
    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = src;
      Object.entries(attributes).map(([k, v]) => script.setAttribute(k, v));
      script.onload = resolve;
      script.onerror = reject;
      document.body.appendChild(script);
    });
  }

  static playgroundInit(component, params) {
    const parentElement = document.querySelector(`#${SCLPlayground.playgroundId}`);
    const sclPlayground = window.Blazor.rootComponents.add(parentElement, component, {});
    window.sclPlaygroundComponent = sclPlayground;
  }

  static blazorInit(callback) {
    window.sclPlaygroundInit = SCLPlayground.playgroundInit;

    SCLPlayground.addScript('_content/MudBlazor/MudBlazor.min.js')
      .then(() =>
        SCLPlayground.addScript('_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js')
      )
      .then(() => {
        const script = document.createElement('script');
        script.innerText = `require.config({ paths: { vs: '_content/BlazorMonaco/lib/monaco-editor/min/vs'}});`;
        document.body.appendChild(script);
        SCLPlayground.addScript('_content/BlazorMonaco/jsInterop.js');
      })
      .then(() =>
        SCLPlayground.addScript(
          '_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js'
        )
      )
      .then(() => SCLPlayground.addScript('_content/SCLEditor.Components/DefineSCLLanguage.js'))
      .then(() => SCLPlayground.addScript('_framework/blazor.webassembly.js', { autostart: false }))
      // eslint-disable-next-line no-undef
      .then(() => Blazor.start().then(callback))
      .catch((error) => console.error(error));
  }
}

SCLPlayground.defaultProps = {
  isDarkMode: false,
};

export default SCLPlayground;
