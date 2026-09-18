namespace EmployeeHRMS.Api.Helpers
{
    /// <summary>
    /// Event Logger — minh họa: Action<string> delegate, lambda expressions
    /// Cho phép tùy chỉnh hành vi log bằng cách gán lambda vào delegate
    /// </summary>
    public class EventLogger
    {
        private readonly ILogger<EventLogger> _logger;

        // === Action<string> delegate fields ===
        // Action<string> là delegate nhận 1 tham số string và không trả về giá trị (void)
        // Cho phép inject custom logging behavior từ bên ngoài

        /// <summary>Delegate được gọi khi một entity mới được tạo</summary>
        public Action<string> OnEntityCreated { get; set; }

        /// <summary>Delegate được gọi khi một entity được cập nhật</summary>
        public Action<string> OnEntityUpdated { get; set; }

        /// <summary>Delegate được gọi khi một entity bị xóa</summary>
        public Action<string> OnEntityDeleted { get; set; }

        public EventLogger(ILogger<EventLogger> logger)
        {
            _logger = logger;

            // Gán default implementation bằng lambda expressions
            // Lambda: (parameter) => expression
            OnEntityCreated = message => _logger.LogInformation("[CREATED] {Message}", message);
            OnEntityUpdated = message => _logger.LogInformation("[UPDATED] {Message}", message);
            OnEntityDeleted = message => _logger.LogInformation("[DELETED] {Message}", message);
        }

        /// <summary>
        /// Log một event — nhận Action<string> delegate làm callback parameter.
        /// Minh họa: Higher-order function (hàm nhận delegate làm tham số)
        /// </summary>
        /// <param name="entityName">Tên entity (VD: "Employee", "Department")</param>
        /// <param name="action">Hành động được thực hiện (VD: "created", "updated")</param>
        /// <param name="callback">Action delegate sẽ được invoke với message</param>
        public void LogEvent(string entityName, string action, Action<string> callback)
        {
            var message = $"{entityName} was {action} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            callback(message); // Invoke delegate
        }

        /// <summary>
        /// Log event với custom Func&lt;string, string&gt; transformer.
        /// Minh họa: Func delegate (có return value) vs Action delegate (void)
        /// </summary>
        /// <param name="entityName">Tên entity</param>
        /// <param name="action">Hành động</param>
        /// <param name="messageTransformer">Func delegate biến đổi message trước khi log</param>
        public void LogEventWithTransform(string entityName, string action, Func<string, string> messageTransformer)
        {
            var rawMessage = $"{entityName} was {action} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            var transformedMessage = messageTransformer(rawMessage); // Invoke Func delegate
            _logger.LogInformation("{Message}", transformedMessage);
        }
    }
}
