import React, { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';

function useForceUpdate() {
  const [, setState] = useState();
  return () => setState({});
}

function addScript(src, attributes = {}) {
  return new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.src = src;
    Object.entries(attributes).map(([k, v]) => script.setAttribute(k, v));
    script.onload = resolve;
    script.onerror = reject;
    document.body.appendChild(script);
  });
}

function blazorInit() {
  window.sclPlaygroundInit = function (component, params) {
    const parentElement = document.querySelector('#scl-playground-root');
    const sclPlayground = window.Blazor.rootComponents.add(parentElement, component, {});
    window.sclPlaygroundComponent = sclPlayground;
  };

  addScript('_content/MudBlazor/MudBlazor.min.js')
    .then(() => addScript('_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js'))
    .then(() => {
      const script = document.createElement('script');
      script.innerText = `require.config({ paths: { vs: '_content/BlazorMonaco/lib/monaco-editor/min/vs'}});`;
      document.body.appendChild(script);
      addScript('_content/BlazorMonaco/jsInterop.js');
    })
    .then(() => addScript('_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js'))
    .then(() => addScript('_content/SCLEditor.Components/DefineSCLLanguage.js'))
    .then(() => addScript('_framework/blazor.webassembly.js'))
    .catch((error) => console.error(error));
  // eslint-disable-next-line no-undef
  // .then(() => Blazor.start())
  // .then(() => {
  //   // eslint-disable-next-line no-undef
  //   return Blazor.rootComponents.add(parentElement, `${identifier}-react`, props);
  // });
}

export function useBlazor(props) {
  const forceUpdate = useForceUpdate();

  // We prefer useRef over useState because we don't want changes to internal housekeeping
  // state to cause the component to re-render.
  const previousPropsRef = useRef({});
  const addRootComponentPromiseRef = useRef(null);
  const hasPendingSetParametersRef = useRef(true);
  const isDisposedRef = useRef(false);

  // After the initial render, we use the template element ref to find its parent node so we
  // can attach the dynamic root component to it.
  const onTemplateRender = useCallback(
    (node) => {
      if (!node) {
        return;
      }

      const parentElement = document.querySelector(`#scl-playground-root`);
      // const parentElement = node.parentElement;

      // We defer adding the root component until after this component re-renders.
      // If Blazor removes the template element from the DOM before React does,
      // it can throw off React's DOM management.
      addRootComponentPromiseRef.current = Promise.resolve()
        .then(() => {
          // eslint-disable-next-line no-undef
          if (typeof Blazor === 'undefined' || Blazor == null) {
            blazorInit();
          } else {
            // eslint-disable-next-line no-undef
            window.sclPlaygroundComponent = Blazor.rootComponents.add(
              parentElement,
              `scl-playground-react`,
              props
            );
          }
          return window.sclPlaygroundComponent;
        })
        .then((rootComponent) => {
          hasPendingSetParametersRef.current = false;
          return rootComponent;
        });

      // We want to cause a re-render here so the template element gets removed by
      // React rather than by Blazor.
      forceUpdate();
    },
    [forceUpdate, props]
  );

  // Supply .NET with updated parameters.
  useEffect(() => {
    if (hasPendingSetParametersRef.current) {
      return;
    }

    const parameters = {};
    let parametersDidChange = false;

    // Only send changed parameters to .NET.
    for (const [key, value] of Object.entries(props)) {
      if (previousPropsRef.current[key] !== value) {
        parameters[key] = value;
        parametersDidChange = true;
      }
    }

    if (!parametersDidChange) {
      return;
    }

    hasPendingSetParametersRef.current = true;
    addRootComponentPromiseRef.current
      .then((rootComponent) => {
        if (!isDisposedRef.current) {
          return rootComponent.setParameters(parameters);
        }
      })
      .then(() => {
        hasPendingSetParametersRef.current = false;
      });
  }, [props]);

  // This effect will run when the component is about to unmount.
  useEffect(
    () => () => {
      // setTimeout(() => {
      isDisposedRef.current = true;
      if (addRootComponentPromiseRef.current) {
        addRootComponentPromiseRef.current.then((rootComponent) => rootComponent.dispose());
      }
      // }, 1000);
    },
    []
  );

  // Update the previous props with the current props after each render.
  useEffect(() => {
    previousPropsRef.current = props;
  });

  return addRootComponentPromiseRef.current === null ? <template ref={onTemplateRender} /> : null;
}
