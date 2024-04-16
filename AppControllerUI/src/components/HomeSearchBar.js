function HomeSearchBar() {
  return (
    <div className="container">
      <div class="input-group mb-3 mt-3">
        {/* <button
          class="btn btn-outline-secondary dropdown-toggle"
          type="button"
          data-bs-toggle="dropdown"
          aria-expanded="false"
        >
          Dropdown
        </button>
        <ul class="dropdown-menu" style={{zIndex: 10000000}}>
          <li>
            <a className="dropdown-item" href="#">
              Action
            </a>
          </li>
          <li>
            <a className="dropdown-item" href="#">
              Another action
            </a>
          </li>
          <li>
            <a className="dropdown-item" href="#">
              Something else here
            </a>
          </li>
        </ul> */}
        <input
          type="text"
          class="form-control"
          placeholder="Search Text here..."
          aria-label="Text input with dropdown button"
        />
      </div>
    </div>
  );
}

export default HomeSearchBar;
