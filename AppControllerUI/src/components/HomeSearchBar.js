import { useCallback, useState } from "react";
import QueryModal from "./QueryModal";

function HomeSearchBar({ filterRows }) {
  const [text, setText] = useState("");

  function handleSearch() {
    const searchText = document.getElementById("homeSearchBar")?.value;

    filterRows(searchText);
  }

  function handleClear() {
    setText("")
    document.getElementById("homeSearchBar").value = "";
    filterRows("");
  }

  const appendTextInSearchBar = useCallback((additionalText) => {
    setText((prevText) => prevText + " " + additionalText + " ");
  }, []);

  return (
    <div className="container">
      <div className="input-group mb-3 mt-3">
        <input
          type="text"
          className="form-control"
          placeholder="Write Query Language Here"
          aria-label="Text input with dropdown button"
          id="homeSearchBar"
          value={text}
          onChange={useCallback((e) => setText(e.target.value), [])}
        />
        <button
          className="mx-1 btn btn-outline-primary"
          type="button"
          id="button-addon2"
          onClick={handleSearch}
        >
          Search
        </button>{" "}
        <button
          className="mx-1 btn btn-secondary"
          type="button"
          id="button-addon2"
          onClick={handleClear}
        >
          Clear
        </button>
      </div>
      <div className="justify-content-center input-group mb-3">
        <label className="p-2">Key Words for Query Language</label>
        <button
          type="button"
          className="btn btn-secondary btn-sm ms-1"
          onClick={() => appendTextInSearchBar("App")}
        >
          App
        </button>
        <button
          type="button"
          className="btn btn-secondary btn-sm ms-1"
          onClick={() => appendTextInSearchBar("User")}
        >
          User
        </button>
        <button
          type="button"
          className="btn btn-secondary btn-sm ms-1"
          onClick={() => appendTextInSearchBar("Summary")}
        >
          Summary
        </button>
        <button
          type="button"
          className="btn btn-secondary btn-sm ms-1 me-2"
          onClick={() => appendTextInSearchBar("Date")}
        >
          Date
        </button>
        <button
          type="button"
          className="btn btn-secondary btn-sm ms-1"
          onClick={() => appendTextInSearchBar("AND")}
        >
          AND
        </button>
        <button
          type="button"
          className="btn btn-secondary btn-sm ms-1 me-2"
          onClick={() => appendTextInSearchBar("OR")}
        >
          OR
        </button>

        <button
          type="button"
          className="btn btn-secondary btn-sm ms-1"
          onClick={() => appendTextInSearchBar("=")}
        >
          {"="}
        </button>
        <button
          type="button"
          className="btn btn-secondary btn-sm ms-1"
          onClick={() => appendTextInSearchBar("<")}
        >
          {"<"}
        </button>
        <button
          type="button"
          className="btn btn-secondary btn-sm ms-1"
          onClick={() => appendTextInSearchBar(">")}
        >
          {">"}
        </button>
        <button
          type="button"
          className="btn btn-secondary btn-sm ms-1"
          onClick={() => appendTextInSearchBar("contains")}
        >
          {"contains"}
        </button>
        <a
          href="#"
          className="ms-5 alert-link me-1 pt-2"
          data-bs-toggle="modal"
          data-bs-target="#queryModal"
        >
          How to use?
        </a>
        <QueryModal />
      </div>
    </div>
  );
}

export default HomeSearchBar;
