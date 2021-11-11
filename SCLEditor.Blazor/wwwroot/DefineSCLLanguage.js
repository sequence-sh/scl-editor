
function registerSCL() {
  // Register a new language




  monaco.languages.register({ id: 'scl' });

  monaco.languages.setMonarchTokensProvider('scl',
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
