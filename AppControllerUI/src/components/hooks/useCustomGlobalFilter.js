import { useMemo } from "react";

// Custom hook for global filtering
const useCustomGlobalFilter = (data, columns, globalFilterValue) => {
  const filteredData = useMemo(() => {
    if (!globalFilterValue) return data;

    return data.filter((row) => {
      return columns.some((column) => {
        const cellValue = row[column.accessor];
        return String(cellValue).toLowerCase().includes(globalFilterValue.toLowerCase());
      });
    });
  }, [data, columns, globalFilterValue]);

  return filteredData;
};

export default useCustomGlobalFilter;
