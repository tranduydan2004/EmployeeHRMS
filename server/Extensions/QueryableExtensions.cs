namespace EmployeeHRMS.Api.Extensions
{
    /// <summary>
    /// Extension methods cho IEnumerable — minh họa: Extension Methods + Func delegate + Generic
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Filter một collection bằng Func<T, bool> predicate.
        /// Minh họa: Extension method + Func delegate
        /// </summary>
        public static IEnumerable<T> ApplyFilter<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return source.Where(predicate);
        }

        /// <summary>
        /// Phân trang cho collection.
        /// Minh họa: Extension method với multiple parameters
        /// </summary>
        public static IEnumerable<T> ToPagedList<T>(this IEnumerable<T> source, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            return source
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
        }

        /// <summary>
        /// Projection từ TSource → TResult bằng Func delegate.
        /// Minh họa: Generic extension method + Func<TSource, TResult>
        /// </summary>
        public static IEnumerable<TResult> ProjectTo<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return source.Select(selector);
        }

        /// <summary>
        /// Sắp xếp linh hoạt theo key selector — có thể ascending hoặc descending.
        /// Minh họa: Func<T, TKey> dùng làm key selector
        /// </summary>
        public static IEnumerable<T> SortBy<T, TKey>(
            this IEnumerable<T> source,
            Func<T, TKey> keySelector,
            bool descending = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return descending
                ? source.OrderByDescending(keySelector)
                : source.OrderBy(keySelector);
        }

        /// <summary>
        /// Kiểm tra collection có thỏa mãn tất cả điều kiện hay không.
        /// Minh họa: Extension method kết hợp Func delegate + LINQ All()
        /// </summary>
        public static bool AllMatch<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            return source.All(predicate);
        }

        /// <summary>
        /// Thực thi một Action lên mỗi phần tử — minh họa: Action<T> delegate
        /// </summary>
        public static void ForEachItem<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            foreach (var item in source)
            {
                action(item);
            }
        }
    }
}
