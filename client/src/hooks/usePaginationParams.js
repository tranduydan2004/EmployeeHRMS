import { useSearchParams } from 'react-router-dom';
import { useMemo, useCallback } from 'react';

export function usePaginationParams(defaults = {}) {
  const [searchParams, setSearchParams] = useSearchParams();

  const defaultPageSize = defaults.pageSize || 10;
  const defaultSortBy = defaults.sortBy || '';
  const defaultIsDescending = defaults.isDescending ?? false;

  const pageNumber = useMemo(() => {
    const p = parseInt(searchParams.get('pageNumber'), 10);
    return isNaN(p) || p < 1 ? 1 : p;
  }, [searchParams]);

  const pageSize = useMemo(() => {
    const s = parseInt(searchParams.get('pageSize'), 10);
    return isNaN(s) || s < 1 ? defaultPageSize : s;
  }, [searchParams, defaultPageSize]);

  const sortBy = useMemo(() => {
    return searchParams.get('sortBy') || defaultSortBy;
  }, [searchParams, defaultSortBy]);

  const isDescending = useMemo(() => {
    const desc = searchParams.get('isDescending');
    if (desc === null) return defaultIsDescending;
    return desc === 'true';
  }, [searchParams, defaultIsDescending]);

  const search = useMemo(() => {
    return searchParams.get('search') || '';
  }, [searchParams]);

  const departmentId = useMemo(() => {
    return searchParams.get('departmentId') || '';
  }, [searchParams]);

  const status = useMemo(() => {
    return searchParams.get('status') || '';
  }, [searchParams]);

  const setPage = useCallback(
    (newPage) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        if (newPage <= 1) {
          next.delete('pageNumber');
        } else {
          next.set('pageNumber', newPage.toString());
        }
        return next;
      });
    },
    [setSearchParams]
  );

  const setPageSize = useCallback(
    (newSize) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        next.set('pageSize', newSize.toString());
        next.delete('pageNumber'); // Reset to page 1 on pageSize change
        return next;
      });
    },
    [setSearchParams]
  );

  const setSort = useCallback(
    (columnKey) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        const currentSortBy = next.get('sortBy') || defaultSortBy;
        const currentDesc = next.get('isDescending') === 'true';

        if (currentSortBy === columnKey) {
          if (currentDesc) {
            // Reset sort
            next.delete('sortBy');
            next.delete('isDescending');
          } else {
            // Toggle to descending
            next.set('isDescending', 'true');
          }
        } else {
          // Set new column, ascending
          next.set('sortBy', columnKey);
          next.set('isDescending', 'false');
        }
        next.delete('pageNumber');
        return next;
      });
    },
    [setSearchParams, defaultSortBy]
  );

  const setSearch = useCallback(
    (searchTerm) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        if (!searchTerm) {
          next.delete('search');
        } else {
          next.set('search', searchTerm);
        }
        next.delete('pageNumber');
        return next;
      });
    },
    [setSearchParams]
  );

  const setFilter = useCallback(
    (key, value) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        if (value === undefined || value === null || value === '') {
          next.delete(key);
        } else {
          next.set(key, value.toString());
        }
        next.delete('pageNumber'); // Reset to page 1 on filter change
        return next;
      });
    },
    [setSearchParams]
  );

  const resetParams = useCallback(() => {
    setSearchParams(new URLSearchParams());
  }, [setSearchParams]);

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize,
      sortBy: sortBy || undefined,
      isDescending,
      search: search || undefined,
      ...(departmentId ? { departmentId } : {}),
      ...(status ? { status } : {}),
    }),
    [pageNumber, pageSize, sortBy, isDescending, search, departmentId, status]
  );

  return {
    pageNumber,
    pageSize,
    sortBy,
    isDescending,
    search,
    departmentId,
    status,
    queryParams,
    setPage,
    setPageSize,
    setSort,
    setSearch,
    setFilter,
    resetParams,
  };
}
