import ReactDOM from "react-dom/client";
import { BrowserRouter, Routes, Route, Link } from "react-router-dom";
import Home from "./Home";
import Master from "./Master";

export default function App() {
  return (
    
    <BrowserRouter>
      <div className="App">
          <ul className="App-header">
              <li>
                  <Link to="/">Home</Link>
              </li>
              <li>
                  <Link to="/Master">Master</Link>
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
