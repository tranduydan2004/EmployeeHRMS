import { ArrowUpDown, ArrowUp, ArrowDown } from 'lucide-react';
import Skeleton from '../Skeleton/Skeleton';

export default function Table({
  columns = [],
  data = [],
  isLoading = false,
  emptyMessage = 'Chưa có dữ liệu nào.',
  sortBy,
  isDescending,
  onSort,
  keyExtractor = (item, index) => item.id || item.employeeId || index,
}) {
  const renderSortIcon = (columnKey) => {
    if (sortBy?.toLowerCase() !== columnKey?.toLowerCase()) {
      return <ArrowUpDown size={14} style={{ opacity: 0.4, marginLeft: '4px' }} />;
    }
    return isDescending ? (
      <ArrowDown size={14} style={{ color: 'var(--primary-600)', marginLeft: '4px' }} />
    ) : (
      <ArrowUp size={14} style={{ color: 'var(--primary-600)', marginLeft: '4px' }} />
    );
  };

  return (
    <div className="table-container">
      <table className="custom-table">
        <thead>
          <tr>
            {columns.map((col) => (
              <th
                key={col.key || col.title}
                style={{ width: col.width, textAlign: col.align || 'left' }}
                className={col.sortable ? 'sortable' : ''}
                onClick={() => col.sortable && onSort && onSort(col.sortKey || col.key)}
              >
                <div style={{ display: 'inline-flex', alignItems: 'center' }}>
                  {col.title}
                  {col.sortable && renderSortIcon(col.sortKey || col.key)}
                </div>
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {isLoading ? (
            Array.from({ length: 5 }).map((_, rIdx) => (
              <tr key={rIdx}>
                {columns.map((col, cIdx) => (
                  <td key={cIdx} style={{ textAlign: col.align || 'left' }}>
                    <Skeleton height="18px" width={col.skeletonWidth || '80%'} />
                  </td>
                ))}
              </tr>
            ))
          ) : data.length === 0 ? (
            <tr>
              <td
                colSpan={columns.length}
                style={{
                  textAlign: 'center',
                  padding: '3rem 1rem',
                  color: 'var(--text-muted)',
                }}
              >
                {emptyMessage}
              </td>
            </tr>
          ) : (
            data.map((row, rIdx) => (
              <tr key={keyExtractor(row, rIdx)}>
                {columns.map((col, cIdx) => (
                  <td key={cIdx} style={{ textAlign: col.align || 'left' }}>
                    {col.render ? col.render(row, rIdx) : row[col.key]}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}
