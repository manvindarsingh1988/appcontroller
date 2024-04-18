function QueryModal() {
  function handleCopyToClipboard(event) {
    function showFadingAlert() {
      // Show the alert
      const alert = document.getElementById("fadingAlert");
      alert.style.display = "block";

      // Automatically hide the alert after 3 seconds
      setTimeout(function () {
        alert.style.display = "none";
      }, 1000); // 3000 milliseconds = 3 seconds
    }

    const text = event.target.parentElement.querySelector("samp").textContent;
    navigator.clipboard.writeText(text);
    showFadingAlert();
  }

  return (
    <div
      className="modal fade"
      id="queryModal"
      tabIndex="-1"
      aria-labelledby="queryModalLabel"
      aria-hidden="true"
    >
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title" id="queryModalLabel">
              Saral Query Language
            </h5>
            <button
              type="button"
              className="btn-close"
              data-bs-dismiss="modal"
              aria-label="Close"
            ></button>
          </div>
          <div className="modal-body">
            <div
              class="alert alert-success fade show"
              role="alert"
              id="fadingAlert"
              style={{ display: "none" }}
            >
              Text copied to clipboard...
            </div>
            <p>
              <strong>
                With the help Query Language you can filter data pefectly
              </strong>
            </p>
            Use Column names <strong>[App, Date, User, Summary]</strong> with
            operators and conditions
            <ul>
              <li>
                <strong>{"< : "}</strong>Less Than, can be used with dates and
                numbers
              </li>
              <li>
                <strong>{"> : "}</strong>Less Than, can be used with dates and
                numbers
              </li>
              <li>
                <strong>{"= : "}</strong>Equals, can be used for texts, dates
                and numbers
              </li>
              <li>
                <strong>{"contains : "}</strong>contains, can be used for text
              </li>{" "}
              <br />
              <li>
                <strong>{"AND : "}</strong>AND, can be used to add more
                conditions
              </li>
              <li>
                <strong>{"OR : "}</strong>OR, can be used to add extra
                conditions
              </li>
            </ul>
            <p></p>
            <p>
              <strong>Example Queries</strong>
            </p>
            <p>
              <samp className="border border-1 rounded-1 p-3">
                {"App = Edge"}
              </samp>
              <button
                type="button"
                className="ms-2 btn btn-sm btn-outline-primary"
                onClick={handleCopyToClipboard}
              >
                Copy
              </button>
            </p>
            <br />
            <p>
              <samp className="border border-1 rounded-1 p-3">
                {"Name contains VINIT"}
              </samp>
              <button
                type="button"
                className="ms-2 btn btn-sm btn-outline-primary"
                onClick={handleCopyToClipboard}
              >
                Copy
              </button>
            </p>
            <br />
            <p>
              <samp className="border border-1 rounded-1 p-3">
                {"Date > 01/04/2024 AND Date < 02/04/2024"}
              </samp>
              <button
                type="button"
                className="ms-2 btn btn-sm btn-outline-primary"
                onClick={handleCopyToClipboard}
              >
                Copy
              </button>
            </p>
          </div>
          <div className="modal-footer">
            <button
              type="button"
              className="btn btn-secondary"
              data-bs-dismiss="modal"
            >
              Close
            </button>
            <button type="button" className="btn btn-primary">
              Save changes
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

export default QueryModal;
