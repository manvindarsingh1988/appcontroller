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

const useCustomQueryFilter = (data, columns, query) => {
  const filteredData = useMemo(() => {
    if (!query) return data;

    const queries = query.split(/ (AND|OR) /);
    const filteredDataArr = queries.map((q) => {
      const { columnName, operator, value } = parseQuery(q);
      return data.filter((row) => {
        const cellName = columns.find((c) => c.Header === columnName.trim())?.accessor;

        const cellValue = row[cellName];
        switch (operator) {
          case ">":
            return Number(cellValue) > Number(value);
          case "<":
            return Number(cellValue) < Number(value);
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
