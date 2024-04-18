import { useEffect, useMemo, forwardRef, useRef, useState } from "react";
import {
  useTable,
  useFilters,
  usePagination,
  useRowSelect,
  useSortBy,
} from "react-table";
// A great library for fuzzy filtering/sorting items
import { matchSorter } from "match-sorter";
import Pagination from "./Pagination";
import HomeSearchBar from "./HomeSearchBar";
import useCustomGlobalFilter from "./hooks/useCustomGlobalFilter";
import useCustomQueryFilter from "./hooks/useCustomQueryFilter";

const IndeterminateCheckbox = forwardRef(({ indeterminate, ...rest }, ref) => {
  const defaultRef = useRef();
  const resolvedRef = ref || defaultRef;

  useEffect(() => {
    resolvedRef.current.indeterminate = indeterminate;
  }, [resolvedRef, indeterminate]);

  return <input type="checkbox" ref={resolvedRef} {...rest} />;
});

function DefaultColumnFilter({
  column: { filterValue, preFilteredRows, setFilter },
}) {
  const count = preFilteredRows.length;

  return (
    <input
      value={filterValue || ""}
      onChange={(e) => {
        setFilter(e.target.value || undefined); // Set undefined to remove the filter entirely
      }}
      placeholder={`Search ${count} records...`}
    />
  );
}

function fuzzyTextFilterFn(rows, id, filterValue) {
  return matchSorter(rows, filterValue, { keys: [(row) => row.values[id]] });
}

// Let the table remove the filter if the string is empty
fuzzyTextFilterFn.autoRemove = (val) => !val;

function Table({ columns, data, handleCheckboxSelection }) {
  const [globalFilter, setGlobalFilter] = useState("");

  const filterTypes = useMemo(
    () => ({
      // Add a new fuzzyTextFilterFn filter type.
      fuzzyText: fuzzyTextFilterFn,
      // Or, override the default text filter to use
      // "startWith"
      text: (rows, id, filterValue) => {
        return rows.filter((row) => {
          const rowValue = row.values[id];
          return rowValue !== undefined
            ? String(rowValue)
                .toLowerCase()
                .startsWith(String(filterValue).toLowerCase())
            : true;
        });
      },
    }),
    []
  );

  const defaultColumn = useMemo(
    () => ({
      // Let's set up our default Filter UI
      Filter: DefaultColumnFilter,
    }),
    []
  );

  const {
    getTableProps,
    getTableBodyProps,
    headerGroups,
    prepareRow,
    pageCount,
    page, // Instead of using 'rows', we'll use page, which has only the rows for the active page

    // The rest of these things are super handy, too ;)
    canPreviousPage,
    canNextPage,
    pageOptions,
    gotoPage,
    nextPage,
    previousPage,
    setPageSize,

    state,
    selectedFlatRows,
  } = useTable(
    {
      columns,
      // data: useCustomGlobalFilter(data, columns, globalFilter), // Apply global filter to data,
      data: useCustomQueryFilter(data, columns, globalFilter),
      defaultColumn, // Be sure to pass the defaultColumn option
      filterTypes,
      initialState: { pageSize: 100 },
    },
    useFilters,
    useSortBy,
    usePagination,
    useRowSelect,
    (hooks) => {
      hooks.visibleColumns.push((columns) => [
        // Let's make a column for selection
        {
          id: "selection",
          width: 20,
          // The header can use the table's getToggleAllRowsSelectedProps method
          // to render a checkbox
          Header: ({ getToggleAllRowsSelectedProps }) => (
            <div>
              <IndeterminateCheckbox {...getToggleAllRowsSelectedProps()} />
            </div>
          ),
          // The cell can use the individual row's getToggleRowSelectedProps method
          // to the render a checkbox
          Cell: ({ row }) => (
            <div>
              <IndeterminateCheckbox {...row.getToggleRowSelectedProps()} />
            </div>
          ),
        },
        ...columns,
      ]);
    }
  );

  const { pageSize, pageIndex } = state;
  const paginationProps = {
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
  };

  function filterRows(searchText) {
    setGlobalFilter(searchText);
    gotoPage(0);
  }

  return (
    <>
      <HomeSearchBar filterValue={globalFilter} filterRows={filterRows} />
      <div className="table-responsive">
        <table {...getTableProps()} className="table">
          <thead className="sticky-top bg-success p-2 text-white">
            {headerGroups.map((headerGroup) => (
              <tr
                {...headerGroup.getHeaderGroupProps()}
                className="red"
                key={headerGroup.key}
              >
                {headerGroup.headers.map((column) => (
                  <th key={column.key}>{column.render("Header")}</th>
                ))}
              </tr>
            ))}
          </thead>
          <tbody {...getTableBodyProps()}>
            {page.map((row, i) => {
              prepareRow(row);
              return (
                <tr
                  key={row.key}
                  {...row.getRowProps()}
                  style={{
                    backgroundColor: !row.isSelected ? "" : "#dcf7e3",
                  }}
                >
                  {row.cells.map((cell) => {
                    return (
                      <td key={cell.key} {...cell.getCellProps()}>
                        {cell.render("Cell")}
                      </td>
                    );
                  })}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {/* </div>
      </div> */}
      <Pagination
        handleCheckboxSelection={handleCheckboxSelection}
        selectedFlatRows={selectedFlatRows}
        {...paginationProps}
      />
    </>
  );
}

export default Table;
