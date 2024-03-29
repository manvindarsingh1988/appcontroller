import {useState} from 'react';
import ReactDOM from "react-dom/client";
import { BrowserRouter, Routes, Route, NavLink } from "react-router-dom";
import Home from "./Home";
import Master from "./Master";
import UserDetails from "./UserDetails";
import Login from "./Login";
import './App.css'

export default function App() {

    const token = localStorage.getItem('token');
    if(!token) {
        return <Login />
    }
  return (    
    <BrowserRouter>
      <div className="sticky">
          <ul className="ul">
              <li className="li">
                  <NavLink to="/" activeClassName="active" >Home</NavLink>
              </li>
              <li className="li">
                  <NavLink to="/Master" activeClassName="active">Master</NavLink>
              </li>
              <li className="li">
                  <NavLink to="/UserDetails" activeClassName="active">User History</NavLink>
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
              <Route
                  exact
                  path="/UserDetails"
                  element={<UserDetails />}
              ></Route>
          </Routes>
      </div>
      
    </BrowserRouter>
  );
}

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(<App />);
