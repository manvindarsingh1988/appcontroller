import { useMemo } from "react";

// Query parser function
const parseQuery = (query) => {
  const operators = [">", "<", "=", "contains"]; // Add more operators as needed
  const [columnName, operator, value] = query.split(
    new RegExp(`\\s+(${operators.join("|")})\\s+`)
  );
  const conjunction = query.includes(" OR ") ? "OR" : "AND";
  return { columnName, operator, value, conjunction };
};

function parseValue(value, type) {
  switch (type) {
    case "string":
      return String(value);
    case "number":
      return parseFloat(value);
    case "integer":
      return parseInt(value, 10);
    case "boolean":
      return value === "true";
    case "date":
      return parseDateTime(value);
    default:
      return value;
  }
}

function parseDateTime(dateTimeString) {
  if (!dateTimeString?.toString().includes("/")) {
    return new Date(dateTimeString);
  }
  // Split the date string by the " " separator
  const [datePart, timePart] = dateTimeString.split(" ");

  const [day, month, year] = datePart.split("/").map(Number);

  let hours = 0,
    minutes = 0,
    seconds = 0;
  if (timePart) {
    // If there is a time part, split it by the ":" separator
    const [hoursStr, minutesStr, secondsStr] = timePart.split(":").map(Number);
    hours = hoursStr;
    minutes = minutesStr;
    seconds = secondsStr || 0; // If seconds are not provided, default to 0
  }

  // Create a new Date object using the components
  // Note: Months in JavaScript's Date object are 0-indexed, so we subtract 1 from the month
  return new Date(year, month - 1, day, hours, minutes, seconds);
}

const useCustomQueryFilter = (data, columns, query) => {
  const filteredData = useMemo(() => {
    if (!query) return data;

    const queries = query.split(/ (AND|OR) /);
    const filteredDataArr = queries.map((q) => {
      let { columnName, operator, value } = parseQuery(q);
      return data.filter((row) => {
        const result = columns.find((c) => c.Header === columnName.trim());

        if (!result) {
          return false;
        }

        const cellName = result?.accessor;
        const cellType = result?.type;

        const cellValue = parseValue(row[cellName], cellType);
        value = parseValue(value, cellType);

        switch (operator) {
          case ">":
            return cellValue > value;
          case "<":
            return cellValue < value;
          case "=":
            return cellValue === value;
          case "contains":
            return cellValue.includes(value);
          default:
            return true;
        }
      });
    });

    if (queries.length === 1) return filteredDataArr[0];

    return queries[1] === "OR"
      ? filteredDataArr.reduce((acc, curr) => [...acc, ...curr])
      : filteredDataArr.reduce((acc, curr) =>
          acc.filter((row) => curr.includes(row))
        );
  }, [data, columns, query]);

  return filteredData;
};

export default useCustomQueryFilter;
