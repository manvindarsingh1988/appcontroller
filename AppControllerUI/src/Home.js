import {useEffect, useState, useMemo, forwardRef, useRef}  from 'react';
import axios from 'axios';
import styled from "styled-components";
import {
  useTable,
  useFilters,
  usePagination,
  useRowSelect,
  useSortBy
} from "react-table";
// A great library for fuzzy filtering/sorting items
import {matchSorter}from "match-sorter";

const Styles = styled.div`
  padding: 1rem;

  .row {
    box-sizing: border-box;
    display: flex;
    flex: 0 1 auto;
    flex-direction: row;
    flex-wrap: wrap;
    margin-right: var(--gutter-compensation, -0.5rem);
    margin-left: var(--gutter-compensation, -0.5rem);
  }

  .col-xs-6 {
    flex-basis: 50%;
  }

  table {
    border-spacing: 0;
    border: 1px solid black;
    tr {
      :last-child {
        td {
          border-bottom: 0;
        }
      }
    }

    th,
    td {
      margin: 0;
      padding: 0.5rem;
      border-bottom: 1px solid black;
      border-right: 1px solid black; 
      max-width: 500px;
      word-wrap: break-word;
    }
  }
  .pagination {
    padding: 0.5rem;
  }
`;

// Define a default UI for filtering
function DefaultColumnFilter({
  column: { filterValue, preFilteredRows, setFilter }
}) {
  const count = preFilteredRows.length;

  return (
    <input 
      value={filterValue || ""}
      onChange={e => {
        setFilter(e.target.value || undefined); // Set undefined to remove the filter entirely
      }}
      placeholder={`Search ${count} records...`}
    />
  );
}

// This is a custom filter UI for selecting
// a unique option from a list
function SelectColumnFilter({
  column: { filterValue, setFilter, preFilteredRows, id }
}) {
  // Calculate the options for filtering
  // using the preFilteredRows
  const options = useMemo(() => {
    const options = new Set();
    preFilteredRows.forEach(row => {
      options.add(row.values[id]);
    });
    return [...options.values()];
  }, [id, preFilteredRows]);

  // Render a multi-select box
  return (
    <select
      value={filterValue}
      onChange={e => {
        setFilter(e.target.value || undefined);
      }}
    >
      <option value="">All</option>
      {options.map((option, i) => (
        <option key={i} value={option}>
          {option}
        </option>
      ))}
    </select>
  );
}

function dateBetweenFilterFn(rows, id, filterValues) {
  const sd = filterValues[0] ? new Date(filterValues[0]) : undefined;
  const ed = filterValues[1] ? new Date(filterValues[1]) : undefined;
  if (ed || sd) {
    return rows.filter((r) => {
      let dateAndHour = r.values[id].split(" ");
      var [day, month, year] = dateAndHour[0].split("-");
      var [hours, minutes, seconds] = dateAndHour[1].split(":");
      
      const cellDate = new Date(year,month - 1,day,hours,minutes,seconds);

      if (ed && sd) {
        return cellDate >= sd && cellDate <= ed;
      } else if (sd) {
        return cellDate >= sd;
      } else {
        return cellDate <= ed;
      }
    });
  } else {
    return rows;
  }
}

function DateRangeColumnFilter({
  column: { filterValue = [], preFilteredRows, setFilter, id }
}) {
  const [min, max] = useMemo(() => {
    let min = preFilteredRows.length
      ? new Date(preFilteredRows[0].values[id])
      : new Date(0);
    let max = preFilteredRows.length
      ? new Date(preFilteredRows[0].values[id])
      : new Date(0);

    preFilteredRows.forEach((row) => {
      const rowDate = new Date(row.values[id]);

      min = rowDate <= min ? rowDate : min;
      max = rowDate >= max ? rowDate : max;
    });

    return [min, max];
  }, [id, preFilteredRows]);

  return (
    <div>
      <input
        //min={min.toISOString().slice(0, 10)}
        onChange={(e) => {
          const val = e.target.value;
          setFilter((old = []) => [val ? val : undefined, old[1]]);
        }}
        type="date"
        value={filterValue[0] || ""}
      />
      {" to "}
      <input
        //max={max.toISOString().slice(0, 10)}
        onChange={(e) => {
          const val = e.target.value;
          setFilter((old = []) => [
            old[0],
            val ? val.concat("T23:59:59.999Z") : undefined
          ]);
        }}
        type="date"
        value={filterValue[1]?.slice(0, 10) || ""}
      />
    </div>
  );
}

// This is a custom UI for our 'between' or number range
// filter. It uses two number boxes and filters rows to
// ones that have values between the two
function NumberRangeColumnFilter({
  column: { filterValue = [], preFilteredRows, setFilter, id }
}) {
  const [min, max] = useMemo(() => {
    let min = preFilteredRows.length ? preFilteredRows[0].values[id] : 0;
    let max = preFilteredRows.length ? preFilteredRows[0].values[id] : 0;
    preFilteredRows.forEach(row => {
      min = Math.min(row.values[id], min);
      max = Math.max(row.values[id], max);
    });
    return [min, max];
  }, [id, preFilteredRows]);

  return (
    <div
      style={{
        display: "flex"
      }}
    >
      <input
        value={filterValue[0] || ""}
        type="number"
        onChange={e => {
          const val = e.target.value;
          setFilter((old = []) => [
            val ? parseInt(val, 10) : undefined,
            old[1]
          ]);
        }}
        placeholder={`Min (${min})`}
        style={{
          width: "70px",
          marginRight: "0.5rem"
        }}
      />
      to
      <input
        value={filterValue[1] || ""}
        type="number"
        onChange={e => {
          const val = e.target.value;
          setFilter((old = []) => [
            old[0],
            val ? parseInt(val, 10) : undefined
          ]);
        }}
        placeholder={`Max (${max})`}
        style={{
          width: "70px",
          marginLeft: "0.5rem"
        }}
      />
    </div>
  );
}

function fuzzyTextFilterFn(rows, id, filterValue) {
  return matchSorter(rows, filterValue, { keys: [row => row.values[id]] });
}

// Let the table remove the filter if the string is empty
fuzzyTextFilterFn.autoRemove = val => !val;

const IndeterminateCheckbox = forwardRef(
  ({ indeterminate, ...rest }, ref) => {
    const defaultRef = useRef();
    const resolvedRef = ref || defaultRef;

    useEffect(() => {
      resolvedRef.current.indeterminate = indeterminate;
    }, [resolvedRef, indeterminate]);

    return (
      <>
        <input type="checkbox" ref={resolvedRef} {...rest} />
      </>
    );
  }
);

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
  selectedFlatRows
}) {
  return (
    <div className="pagination">
      <button onClick={() => gotoPage(0)} disabled={!canPreviousPage}>
        {"<<"}
      </button>{" "}
      <button onClick={() => previousPage()} disabled={!canPreviousPage}>
        {"<"}
      </button>{" "}
      <button onClick={() => nextPage()} disabled={!canNextPage}>
        {">"}
      </button>{" "}
      <button onClick={() => gotoPage(pageCount - 1)} disabled={!canNextPage}>
        {">>"}
      </button>{" "}
      <span>
        Page{" "}
        <strong>
          {pageIndex + 1} of {pageOptions.length}
        </strong>{" "}
      </span>
      <span>
        | Go to page:{" "}
        <input
          type="number"
          defaultValue={pageIndex + 1}
          onChange={e => {
            const page = e.target.value ? Number(e.target.value) - 1 : 0;
            gotoPage(page);
          }}
          style={{ width: "100px" }}
        />
      </span>{" "}
      <select
        value={pageSize}
        onChange={e => {
          setPageSize(Number(e.target.value));
        }}
      >
        {[20, 50, 100, 1000, 10000].map(pageSize => (
          <option key={pageSize} value={pageSize}>
            Show {pageSize}
          </option>
        ))}
      </select>
      <button style={{position: 'absolute', right: '10px'}} onClick={() => handleCheckboxSelection(selectedFlatRows)}>Delete Selected</button>
    </div>
  );
}
// Our table component
function Table({ columns, data, handleCheckboxSelection }) {
  const filterTypes = useMemo(
    () => ({
      // Add a new fuzzyTextFilterFn filter type.
      fuzzyText: fuzzyTextFilterFn,
      // Or, override the default text filter to use
      // "startWith"
      text: (rows, id, filterValue) => {
        return rows.filter(row => {
          const rowValue = row.values[id];
          return rowValue !== undefined
            ? String(rowValue)
                .toLowerCase()
                .startsWith(String(filterValue).toLowerCase())
            : true;
        });
      }
    }),
    []
  );

  const defaultColumn = useMemo(
    () => ({
      // Let's set up our default Filter UI
      Filter: DefaultColumnFilter
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
    selectedFlatRows
  } = useTable(
    {
      columns,
      data,
      defaultColumn, // Be sure to pass the defaultColumn option
      filterTypes,
      initialState: { pageSize: 100 }
    },
    useFilters,
    useSortBy,
    usePagination,
    useRowSelect,
    hooks => {
      hooks.visibleColumns.push(columns => [
        // Let's make a column for selection
        {
          id: "selection",
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
          )
        },
        ...columns
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
    setPageSize    
  };

  return (
    <>
      <Pagination handleCheckboxSelection={handleCheckboxSelection} selectedFlatRows={selectedFlatRows} {...paginationProps} />
      <table {...getTableProps()}>
        <thead>
          {headerGroups.map(headerGroup => (
            <tr {...headerGroup.getHeaderGroupProps()}>
              {headerGroup.headers.map(column => (
                <th {...column.getHeaderProps(column.getSortByToggleProps())}>
                  {column.render("Header")}
                  {/* Add a sort direction indicator */}
                  <span>
                    {column.isSorted
                      ? column.isSortedDesc
                        ? " 🔽"
                        : " 🔼"
                      : ""}
                  </span>
                  {/* Render the columns filter UI */}
                  <div>{column.canFilter ? column.render("Filter") : null}</div>
                </th>
              ))}
            </tr>
          ))}
        </thead>
        <tbody {...getTableBodyProps()}>
          {page.map((row, i) => {
            prepareRow(row);
            return (
              <tr {...row.getRowProps()}
              style={{
                backgroundColor: !row.isSelected ? "" : "#04AA6D",
                color: !row.isSelected ? "" : '#fff'
               }}
              >
                {row.cells.map(cell => {
                  return (
                    <td {...cell.getCellProps()} >{cell.render("Cell")}</td>
                  );
                })}
              </tr>
            );
          })}
        </tbody>
      </table>
      <Pagination handleCheckboxSelection={handleCheckboxSelection} selectedFlatRows={selectedFlatRows} {...paginationProps} />
    </>
  );
}

// Define a custom filter filter function!
function filterGreaterThan(rows, id, filterValue) {
  return rows.filter(row => {
    const rowValue = row.values[id];
    return rowValue >= filterValue;
  });
}

// This is an autoRemove method on the filter function that
// when given the new filter value and returns true, the filter
// will be automatically removed. Normally this is just an undefined
// check, but here, we want to remove the filter if it's not a number
filterGreaterThan.autoRemove = val => typeof val !== "number";

function Home() {
  const columns = useMemo(
    () => [
      {
        Header: "Info",
        columns: [
          {
            Header: "Date",
            accessor: "date",
            Filter: DateRangeColumnFilter,
            filter: dateBetweenFilterFn
          },
          {
            Header: "User",
            accessor: "user",
          },
          {
            Header: "App",
            accessor: "appName",
          },
          {
            Header: "Summary",
            accessor: "summary",
          }
        ]
      }
    ],
    []
  );

  const [appInfos, setAppInfos] = useState([]);
  const [updatedOn, setUpdatedOn] = useState(0)
  
  useEffect(() => {
   axios.get('https://manvindarsingh.bsite.net/appinfo')
   .then(res => {
    setAppInfos(res.data);
   }) 
   .catch(err => console.log(err));
  }, [updatedOn]);

  const handleDelete = (array) => {
    let ids = [];
    array.map(_ => ids.push(_.original.id));
    if(ids.length > 0) {
      const payload = {
        ids: ids
      } 
      axios.post('https://manvindarsingh.bsite.net/appinfo/DeleteDetails', payload)
      .then(response => {
        alert(ids.length + ' row(s) deleted.')
        setUpdatedOn(new Date());
      })
      .catch(error => {
        if (error.response) {
          console.log(error.response.data);
          console.log(error.response.status);
          console.log(error.response.headers);
        } else if (error.request) {
          console.log(error.request);
        } else {
          console.log('Error', error.message);
        }
        console.log(error.config);
      });
    }    
  }

  return (  
    <Styles>
      <Table columns={columns} data={appInfos} handleCheckboxSelection={handleDelete}/>
    </Styles>
          
  );
}

export default Home;

