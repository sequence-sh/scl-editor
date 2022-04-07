import React, { useCallback, useState } from 'react';
import logo from './logo.svg';
import './App.css';
import { SCLPlayground } from './SCLPlayground';

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <SCLPlayground />
      </header>
    </div>
  );
}

export default App;
