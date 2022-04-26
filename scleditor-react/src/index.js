import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter, Link, Route, Routes } from 'react-router-dom';
import Another from './Another';
import SCLPlayground from './SCLPlayground';

ReactDOM.render(
  <React.StrictMode>
    <BrowserRouter>
      <nav style={{ margin: 10 }}>
        <Link to="/" style={{ padding: 5, border: 'solid 1px' }}>
          Playground
        </Link>
        <Link to="/another" style={{ padding: 5, border: 'solid 1px' }}>
          Another Page
        </Link>
      </nav>
      <Routes>
        <Route path="/" element={<SCLPlayground />} />
        <Route path="/another" element={<Another />} />
      </Routes>
    </BrowserRouter>
  </React.StrictMode>,
  document.getElementById('root')
);
