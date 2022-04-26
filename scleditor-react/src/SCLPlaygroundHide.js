import React, { createRef } from 'react';
import { createPortal } from 'react-dom';

class SCLPlayground extends React.Component {
  static playgroundId = 'scl-playground-root';
  static componentName = 'scl-playground-react';
  static portalRoot = 'sclplayground-portal';

  constructor(props) {
    super(props);

    this.ref = createRef();
    this.ref.current = document.getElementById(SCLPlayground.playgroundId);

    this.state = {
      loading: this.ref.current == null,
    };
  }

  componentDidMount() {
    // eslint-disable-next-line no-undef
    if (typeof Blazor === 'undefined' || Blazor == null) {
      this.blazorInit(() => this.setState({ loading: false }));
    }
    this.ref.current.style.display = null;
  }

  componentWillUnmount() {
    this.ref.current.style.display = 'none';
  }

  render() {
    if (this.state.loading) {
      return <div>{this.props.loading ?? 'Loading...'}</div>;
    } else {
      return createPortal(null, this.ref.current);
    }
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
    window.Blazor.rootComponents.add(parentElement, component, {});
  }

  blazorInit(callback) {
    if (!this.ref.current) {
      const pgRoot = document.createElement('div');
      pgRoot.id = SCLPlayground.playgroundId;
      pgRoot.style.display = 'none';
      document.body.appendChild(pgRoot);
      this.ref.current = pgRoot;
    }

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
  loading: null,
};

export default SCLPlayground;
