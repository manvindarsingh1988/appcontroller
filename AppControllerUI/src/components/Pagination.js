function Pagination({
  gotoPage,
  previousPage,
  nextPage,
  canPreviousPage,
  canNextPage,
  pageCount,
  pageOptions,
  pageIndex,
  pageSize,
  setPageSize,
  handleCheckboxSelection,
  selectedFlatRows,
}) {
  return (
    <div id="search" className="bg-secondary">
      <button
        className="btn btn-info btn-sm ms-1"
        onClick={() => gotoPage(0)}
        disabled={!canPreviousPage}
      >
        {"<<"}
      </button>
      <button
        className="btn btn-info btn-sm ms-1"
        onClick={() => previousPage()}
        disabled={!canPreviousPage}
      >
        {"<"}
      </button>
      <button
        className="btn btn-info btn-sm ms-1"
        onClick={() => nextPage()}
        disabled={!canNextPage}
      >
        {">"}
      </button>
      <button
        className="btn btn-info btn-sm ms-1"
        onClick={() => gotoPage(pageCount - 1)}
        disabled={!canNextPage}
      >
        {">>"}
      </button>
      <span className="ms-2">
        Page{" "}
        <strong>
          {pageIndex + 1} of {pageOptions.length}
        </strong>
      </span>
      <span className="ms-2">
        | Go to page:{" "}
        <input
          type="number"
          className="form-control-sm  ms-2"
          defaultValue={pageIndex + 1}
          onChange={(e) => {
            const page = e.target.value ? Number(e.target.value) - 1 : 0;
            gotoPage(page);
          }}
          style={{ width: "100px" }}
        />
      </span>
      <select className="form-select-sm ms-2"
        value={pageSize}
        onChange={(e) => {
          setPageSize(Number(e.target.value));
        }}
      >
        {[20, 50, 100, 1000, 10000].map((pageSize) => (
          <option key={pageSize} value={pageSize}>
            Show {pageSize}
          </option>
        ))}
      </select>
      <button
        style={{ position: "absolute", right: "10px" }}
        className="btn btn-warning btn-sm"
        onClick={() => handleCheckboxSelection(selectedFlatRows)}
      >
        Delete Selected
      </button>
    </div>
  );
}

export default Pagination;
