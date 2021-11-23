
function registerSCL(sclHelper) {
  // Register a new language


  window.monaco.languages.register({ id: 'scl' });

  window.monaco.languages.registerCompletionItemProvider("scl",
    {
      triggerCharacters: ["."],

      resolveCompletionItem: (item, token) => {
        return this.resolveCompletionItem(item, sclHelper)
      },
      provideCompletionItems: (model, position, context) => {
        return this.provideCompletionItems(model, position, context, sclHelper)
      }

    }
  );

  window.monaco.languages.setMonarchTokensProvider('scl',
    {
  // Set defaultToken to invalid to see what you do not tokenize yet

  ignoreCase: 'true',
  defaultToken: 'invalid',


  operators: [
    '=', '>', '<',    '==', '<=', '>=', '!=',
    '&&', '||',  '+', '-', '*', '/', '&', '|', '^', '%'
  ],

  // we include these common regular expressions
  symbols:  /[=><!~?:&|+\-*\/\^%]+/,

  // C# style strings
  escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

  // The main tokenizer for our languages
  tokenizer: {
    root: [
      // identifiers and keywords
      [/[Tt][Rr][Uu][Ee]|[Ff][Aa][Ll][Ss][Ee]/, 'constant.boolean'],

      [/<[A-Za-z0-9_-]+>/, 'variable.name' ],
      [/[A-Za-z_\.-]+:/, 'type.identifier' ],
      [/[A-Za-z_-]+\.[A-Za-z_-]+/, 'constant.enum'],
      [/\b[A-Za-z_-]+\b/, 'keyword' ],
      [/\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}:\d{2})?/, 'number.date'],


      // whitespace
      { include: '@whitespace' },

      // delimiters and operators
      [/[{}()\[\]]/, '@brackets'],
      [/[<>](?!@symbols)/, '@brackets'],
      [/@symbols/, { cases: { '@operators': 'operator',
                              '@default'  : '' } } ],


      // numbers
      [/-?\d*\.\d+([eE][\-+]?\d+)?/, 'number.float'],
      [/-?\d+/, 'number'],

      // delimiter: after number because of .\d floats
      [/[;,.]/, 'delimiter'],

      // strings
      [/"([^"\\]|\\.)*$/, 'string.invalid' ],  // non-teminated string
      [/"/,  { token: 'string.dquote', bracket: '@open', next: '@string' } ],
      [/'/,  { token: 'string.squote', bracket: '@open', next: '@string' } ],

    ],

    comment: [
      [/[^\/*]+/, 'comment' ],
      [/\/\*/,    'comment', '@push' ],    // nested comment
      ["\\*/",    'comment', '@pop'  ],
      [/[\/*]/,   'comment' ]
    ],

    string: [
      [/[^\\"'']+/,  'string'],
      [/@escapes/, 'string.escape'],
      [/\\./,      'string.escape.invalid'],
      [/"/,        { token: 'string.dquote', bracket: '@close', next: '@pop' } ],
      [/'/,        { token: 'string.squote', bracket: '@close', next: '@pop' } ]
    ],

    whitespace: [
      [/[ \t\r\n]+/, 'white'],
      [/\/\*/,       'comment', '@comment' ],
      [/#.*$/,    'comment'],
    ],
  },
});
}


async function provideCompletionItems(model, position, context, sclHelper) {
  let request = this._createRequest(position);
  request.CompletionTrigger = (context.triggerKind + 1);
  request.TriggerCharacter = context.triggerCharacter;

  {
    const code = model.getValue();
    const response = await sclHelper.invokeMethodAsync("GetCompletionAsync", code, request);
    const mappedItems = response.items.map(this._convertToVscodeCompletionItem);

    let lastCompletions = new Map();

    for (let i = 0; i < mappedItems.length; i++) {
      lastCompletions.set(mappedItems[i], response.items[i]);
    }

    this._lastCompletions = lastCompletions;

    return { suggestions: mappedItems };
  }
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
  try {
    const response = await sclHelper.invokeMethodAsync("GetCompletionResolveAsync", request);
    return this._convertToVscodeCompletionItem(response.item);
  } catch (error) {
    return;
  }
}


function _createRequest(position) {

    let Line, Column;
    
    Line = position.lineNumber - 1;
    Column = position.column - 1;

    let request = {
      Line,
      Column
    };

    return request;
  }

  function _convertToVscodeCompletionItem(sclCompletion) {
    const docs = sclCompletion.documentation;

    const mapRange = function (edit) {
      const newStart = {
        lineNumber: edit.startLine + 1,
        column: edit.startColumn + 1
      };
      const newEnd = {
        lineNumber: edit.endLine + 1,
        column: edit.endColumn + 1
      };
      return {
        startLineNumber: newStart.lineNumber,
        startColumn: newStart.column,
        endLineNumber: newEnd.lineNumber,
        endColumn: newEnd.column
      };
    };

    const mapTextEdit = function (edit) {
      return new TextEdit(mapRange(edit), edit.NewText);
    };

    const additionalTextEdits = sclCompletion.additionalTextEdits?.map(mapTextEdit);

    const newText = sclCompletion.textEdit?.newText ?? sclCompletion.insertText;
    const insertText = newText;

    const insertRange = sclCompletion.textEdit ? mapRange(sclCompletion.textEdit) : undefined;

    return {
      label: sclCompletion.label,
      kind: sclCompletion.kind - 1,
      detail: sclCompletion.detail,
      documentation: {
        value: docs
      },
      commitCharacters: sclCompletion.commitCharacters,
      preselect: sclCompletion.preselect,
      filterText: sclCompletion.filterText,
      insertText: insertText,
      range: insertRange,
      tags: sclCompletion.tags,
      sortText: sclCompletion.sortText,
      additionalTextEdits: additionalTextEdits,
      keepWhitespace: true
    };
  }

