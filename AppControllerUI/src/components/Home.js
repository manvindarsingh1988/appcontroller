import { useEffect, useState, useMemo } from "react";
import axios from "axios";
import Table from "./Table";
import URL from "../data/url.json";

function dateBetweenFilterFn(rows, id, filterValues) {
  const sd = filterValues[0] ? new Date(filterValues[0]) : undefined;
  const ed = filterValues[1] ? new Date(filterValues[1]) : undefined;
  if (ed || sd) {
    return rows.filter((r) => {
      let dateAndHour = r.values[id].split(" ");
      var [day, month, year] = dateAndHour.includes("-")
        ? dateAndHour[0].split("-")
        : dateAndHour[0].split("/");
      var [hours, minutes, seconds] = dateAndHour[1].split(":");

      const cellDate = new Date(year, month - 1, day, hours, minutes, seconds);

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
  column: { filterValue = [], preFilteredRows, setFilter, id },
}) {
  useMemo(() => {
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
            val ? val.concat("T23:59:59.999Z") : undefined,
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
  column: { filterValue = [], preFilteredRows, setFilter, id },
}) {
  const [min, max] = useMemo(() => {
    let min = preFilteredRows.length ? preFilteredRows[0].values[id] : 0;
    let max = preFilteredRows.length ? preFilteredRows[0].values[id] : 0;
    preFilteredRows.forEach((row) => {
      min = Math.min(row.values[id], min);
      max = Math.max(row.values[id], max);
    });
    return [min, max];
  }, [id, preFilteredRows]);

  return (
    <div
      style={{
        display: "flex",
      }}
    >
      <input
        value={filterValue[0] || ""}
        type="number"
        onChange={(e) => {
          const val = e.target.value;
          setFilter((old = []) => [
            val ? parseInt(val, 10) : undefined,
            old[1],
          ]);
        }}
        placeholder={`Min (${min})`}
        style={{
          width: "70px",
          marginRight: "0.5rem",
        }}
      />
      to
      <input
        value={filterValue[1] || ""}
        type="number"
        onChange={(e) => {
          const val = e.target.value;
          setFilter((old = []) => [
            old[0],
            val ? parseInt(val, 10) : undefined,
          ]);
        }}
        placeholder={`Max (${max})`}
        style={{
          width: "70px",
          marginLeft: "0.5rem",
        }}
      />
    </div>
  );
}

// Define a custom filter filter function!
function filterGreaterThan(rows, id, filterValue) {
  return rows.filter((row) => {
    const rowValue = row.values[id];
    return rowValue >= filterValue;
  });
}

// This is an autoRemove method on the filter function that
// when given the new filter value and returns true, the filter
// will be automatically removed. Normally this is just an undefined
// check, but here, we want to remove the filter if it's not a number
filterGreaterThan.autoRemove = (val) => typeof val !== "number";

function Home() {
  // let windowWidth = window.innerWidth;
  // windowWidth = windowWidth - 150 - 610 - 400 - 20 - 35;
  const columns = useMemo(
    () => [
      {
        Header: "Date",
        accessor: "date",
        Filter: DateRangeColumnFilter,
        filter: dateBetweenFilterFn,
        width: "30%"
      },
      {
        Header: "User",
        accessor: "user",
        width: "20%"
      },
      {
        Header: "App",
        accessor: "appName",
        width: "30%"
      },
      {
        Header: "Summary",
        accessor: "summary",
        width:"18%"
      },
    ],
    []
  );

  const [appInfos, setAppInfos] = useState([]);
  const [updatedOn, setUpdatedOn] = useState(0);

  useEffect(() => {
    axios
      .get(URL.url + "appinfo")
      .then((res) => {
        setAppInfos(res.data);
      })
      .catch((err) => console.log(err));
  }, [updatedOn]);

  const handleDelete = (array) => {
    let ids = [];
    array.map((_) => ids.push(_.original.id));
    if (ids.length > 0) {
      const payload = {
        ids: ids,
      };
      axios
        .post(URL.url + "appinfo/DeleteDetails", payload)
        .then((response) => {
          alert(ids.length + " row(s) deleted.");
          setUpdatedOn(new Date());
        })
        .catch((error) => {
          if (error.response) {
            console.log(error.response.data);
            console.log(error.response.status);
            console.log(error.response.headers);
          } else if (error.request) {
            console.log(error.request);
          } else {
            console.log("Error", error.message);
          }
          console.log(error.config);
        });
    }
  };

  return (
    <Table
      columns={columns}
      data={appInfos}
      handleCheckboxSelection={handleDelete}
    />
  );
}

export default Home;
