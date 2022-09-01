async function provideHover(model, position, sclHelper) {
  const request = { Position: position };
  const code = model.getValue();
  const response = await sclHelper.invokeMethodAsync('GetQuickInfoAsync', code, request);
  return response;
}


async function provideCompletionItems(model, position, context, sclHelper) {
  const request = {
    Position: position,
    CompletionTrigger: context.triggerKind + 1,
    TriggerCharacter: context.triggerCharacter,
  };

  const code = model.getValue();
  const response = await sclHelper.invokeMethodAsync('GetCompletionAsync', code, request);

  return response;
}

async function resolveCompletionItem(item, sclHelper) {
  const lastCompletions = this._lastCompletions;
  if (!lastCompletions) {
    return item;
  }

  const lspItem = lastCompletions.get(item);
  if (!lspItem) {
    return item;
  }

  const request = { Item: lspItem };
  const response = await sclHelper.invokeMethodAsync('GetCompletionResolveAsync', request);
  return this._convertToVscodeCompletionItem(response.item);
}

async function provideSignatureHelp(model, position, sclHelper) {
  const req = { Position: position };
  const code = model.getValue();
  const res = await sclHelper.invokeMethodAsync('GetSignatureHelpAsync', code, req);

  return {
    value: res,
    dispose: () => { },
  };
}

function setDiagnostics(diagnostics, uri) {
  var model = window.monaco.editor.getModel(uri);
  window.monaco.editor.setModelMarkers(model, 'owner', diagnostics);
}

function registerSCL(sclHelper) {
  let exists = window.monaco.languages.getLanguages().filter((l) => l.id === 'scl');
  if (exists.length > 0) {
    return;
  }

  window.monaco.languages.register({ id: 'scl' });

  window.monaco.languages.registerCompletionItemProvider('scl', {
    triggerCharacters: [' '],

    resolveCompletionItem: (item, token) => {
      return this.resolveCompletionItem(item, sclHelper);
    },
    provideCompletionItems: (model, position, context) => {
      return this.provideCompletionItems(model, position, context, sclHelper);
    },
  });

  window.monaco.languages.registerSignatureHelpProvider('scl', {
    signatureHelpTriggerCharacters: [' '],
    provideSignatureHelp: (model, position) => {
      return this.provideSignatureHelp(model, position, sclHelper);
    },
  });

  window.monaco.languages.registerHoverProvider('scl', {
    provideHover: (model, position) => {
      return this.provideHover(model, position, sclHelper);
    },
  });

  window.monaco.languages.setMonarchTokensProvider('scl', {
    // Set defaultToken to invalid to see what you do not tokenize yet

    ignoreCase: 'true',
    defaultToken: 'invalid',

    operators: [
      '=',
      '>',
      '<',
      '==',
      '<=',
      '>=',
      '!=',
      '&&',
      '||',
      '+',
      '-',
      '*',
      '/',
      '&',
      '|',
      '^',
      '%',
    ],

    // we include these common regular expressions
    symbols: /[=><!~?:&|+\-*\/\^%]+/,

    // C# style strings
    escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

    // The main tokenizer for our languages
    tokenizer: {
      root: [
        // identifiers and keywords
        [/[Tt][Rr][Uu][Ee]|[Ff][Aa][Ll][Ss][Ee]/, 'constant.boolean'],

        [/<[A-Za-z0-9_-]+>/, 'variable.name'],
        [/[A-Za-z_\.-]+:/, 'type.identifier'],
        [/[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/, 'constant.enum'],
        [/\b[A-Za-z_-][A-Za-z0-9_-]*\b/, 'keyword'],
        [/\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}:\d{2})?/, 'number.date'],

        // whitespace
        { include: '@whitespace' },

        // delimiters and operators
        [/[{}()\[\]]/, '@brackets'],
        [/[<>](?!@symbols)/, '@brackets'],
        [/@symbols/, { cases: { '@operators': 'operator', '@default': '' } }],

        // numbers
        [/-?\d*\.\d+([eE][\-+]?\d+)?/, 'number.float'],
        [/-?\d+/, 'number'],

        // delimiter: after number because of .\d floats
        [/[;,.]/, 'delimiter'],

        // strings
        [/"([^"\\]|\\.)*$/, 'string.invalid'], // non-teminated string
        [/"""/, { token: 'string.multiline', bracket: '@open', next: '@stringMulti' }],
        [/"/, { token: 'string.dquote', bracket: '@open', next: '@stringDouble' }],
        [/'/, { token: 'string.squote', bracket: '@open', next: '@stringSingle' }],
      ],

      comment: [
        [/[^\/*]+/, 'comment'],
        [/\/\*/, 'comment', '@push'], // nested comment
        ['\\*/', 'comment', '@pop'],
        [/[\/*]/, 'comment'],
      ],

      stringMulti: [
        [/"""/, { token: 'string.multiline', bracket: '@close', next: '@pop' }],
        [/.+/, 'string'],

      ],

      stringSingle: [
        [/[^'']+/, 'string'],
        [/@escapes/, 'string.escape'],
        [/\\./, 'string.escape.invalid'],
        [/'/, { token: 'string.squote', bracket: '@close', next: '@pop' }],
      ],

      stringDouble: [
        [/[^\\"]+/, 'string'],
        [/@escapes/, 'string.escape'],
        [/\\./, 'string.escape.invalid'],
        [/"/, { token: 'string.dquote', bracket: '@close', next: '@pop' }],
      ],

      whitespace: [
        [/[ \t\r\n]+/, 'white'],
        [/\/\*/, 'comment', '@comment'],
        [/#.*$/, 'comment'],
      ],
    },
  });
}

function scrollToEnd(element) {
  if (element != null) {
    element.scrollTop = element.scrollHeight;
  }
}
