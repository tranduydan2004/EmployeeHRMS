import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';

export default function Pagination({
  pageNumber = 1,
  pageSize = 10,
  totalPages = 1,
  totalCount = 0,
  hasPreviousPage = false,
  hasNextPage = false,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [5, 10, 20, 50],
}) {
  const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const endItem = Math.min(pageNumber * pageSize, totalCount);

  const getPageNumbers = () => {
    const pages = [];
    const maxVisible = 5;

    if (totalPages <= maxVisible) {
      for (let i = 1; i <= totalPages; i++) pages.push(i);
    } else {
      let start = Math.max(1, pageNumber - 2);
      let end = Math.min(totalPages, start + maxVisible - 1);
      if (end - start < maxVisible - 1) {
        start = Math.max(1, end - maxVisible + 1);
      }
      for (let i = start; i <= end; i++) pages.push(i);
    }
    return pages;
  };

  return (
    <div className="pagination-wrapper">
      <div style={{ fontSize: '0.875rem', color: 'var(--text-muted)' }}>
        Hiển thị <strong>{startItem}</strong> - <strong>{endItem}</strong> trong tổng số <strong>{totalCount}</strong> kết quả
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
        {onPageSizeChange && (
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.85rem' }}>
            <span>Số dòng:</span>
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              style={{
                padding: '0.25rem 0.5rem',
                borderRadius: 'var(--radius-sm)',
                border: '1px solid var(--border-color)',
                fontSize: '0.85rem',
              }}
            >
              {pageSizeOptions.map((sz) => (
                <option key={sz} value={sz}>
                  {sz}
                </option>
              ))}
            </select>
          </div>
        )}

        <div className="pagination-controls">
          <button
            type="button"
            className="pagination-btn"
            disabled={!hasPreviousPage}
            onClick={() => onPageChange(1)}
            title="Trang đầu"
          >
            <ChevronsLeft size={16} />
          </button>
          <button
            type="button"
            className="pagination-btn"
            disabled={!hasPreviousPage}
            onClick={() => onPageChange(pageNumber - 1)}
            title="Trang trước"
          >
            <ChevronLeft size={16} />
          </button>

          {getPageNumbers().map((p) => (
            <button
              key={p}
              type="button"
              className={`pagination-btn ${p === pageNumber ? 'active' : ''}`}
              onClick={() => onPageChange(p)}
            >
              {p}
            </button>
          ))}

          <button
            type="button"
            className="pagination-btn"
            disabled={!hasNextPage}
            onClick={() => onPageChange(pageNumber + 1)}
            title="Trang sau"
          >
            <ChevronRight size={16} />
          </button>
          <button
            type="button"
            className="pagination-btn"
            disabled={!hasNextPage}
            onClick={() => onPageChange(totalPages)}
            title="Trang cuối"
          >
            <ChevronsRight size={16} />
          </button>
        </div>
      </div>
    </div>
  );
}
