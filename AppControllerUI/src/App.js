import ReactDOM from "react-dom/client";
import { BrowserRouter, Routes, Route, NavLink, useLocation, matchPath } from "react-router-dom";
import Home from "./Home";
import Master from "./Master";
import './App.css'

export default function App() {
  return (    
    <BrowserRouter>
      <div className="sticky">
          <ul className="App-header">
              <li>
                  <NavLink to="/" activeClassName="active" >Home</NavLink>
              </li>
              <li>
                  <NavLink to="/Master" activeClassName="active">Master</NavLink>
              </li>
          </ul>
          <Routes>
              <Route
                  exact
                  path="/"
                  element={<Home />}
              ></Route>
              <Route
                  exact
                  path="/Master"
                  element={<Master />}
              ></Route>
          </Routes>
      </div>
    </BrowserRouter>
  );
}

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(<App />);
