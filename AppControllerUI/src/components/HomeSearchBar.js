function HomeSearchBar({ filterRows }) {
  function handleSearch() {
    const searchText = document.getElementById("homeSearchBar")?.value;

    if (searchText) {
      filterRows(searchText);
    }
  }

  return (
    <div className="container">
      <div className="input-group mb-3 mt-3">
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
              User
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
          className="form-control"
          placeholder="Search Text here..."
          aria-label="Text input with dropdown button"
          id="homeSearchBar"
          onKeyUp={handleSearch}
        />
      </div>
      {/* <p className="fw-lighter text-center">
        Search any text... For date range use dd-mm-YYYY dd-mm-YYYY
        <br />
        for example: 01-04-2024 02-04-2024
      </p> */}
    </div>
  );
}

export default HomeSearchBar;
