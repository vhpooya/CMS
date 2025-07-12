using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public interface ICodeEditorService
    {
        /// <summary>
        /// Executes JavaScript code safely in a sandboxed environment
        /// </summary>
        Task<CodeExecutionResult> ExecuteJavaScriptAsync(string code, int userId, Dictionary<string, object>? inputs = null);
        
        /// <summary>
        /// Saves code to the library
        /// </summary>
        Task<CodeLibrary> SaveCodeToLibraryAsync(int userId, string title, string description, string code, string category, string? tags = null, bool isPublic = false, bool isTemplate = false);
        
        /// <summary>
        /// Gets code from library by ID
        /// </summary>
        Task<CodeLibrary?> GetCodeFromLibraryAsync(int codeId, int userId);
        
        /// <summary>
        /// Updates code in library
        /// </summary>
        Task<CodeLibrary?> UpdateCodeInLibraryAsync(int codeId, int userId, string? title = null, string? description = null, string? code = null, string? category = null, string? tags = null, bool? isPublic = null);
        
        /// <summary>
        /// Deletes code from library
        /// </summary>
        Task<bool> DeleteCodeFromLibraryAsync(int codeId, int userId);
        
        /// <summary>
        /// Gets user's code library
        /// </summary>
        Task<List<CodeLibrary>> GetUserCodeLibraryAsync(int userId, string? category = null, string? search = null, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Gets public code templates
        /// </summary>
        Task<List<CodeLibrary>> GetPublicTemplatesAsync(string? category = null, string? search = null, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Gets all available categories
        /// </summary>
        Task<List<string>> GetCategoriesAsync();
        
        /// <summary>
        /// Rates a code library item
        /// </summary>
        Task<CodeLibraryRating> RateCodeAsync(int codeId, int userId, int rating, string? comment = null);
        
        /// <summary>
        /// Gets code ratings
        /// </summary>
        Task<List<CodeLibraryRating>> GetCodeRatingsAsync(int codeId);
        
        /// <summary>
        /// Increments usage count for a code library item
        /// </summary>
        Task IncrementUsageCountAsync(int codeId);
        
        /// <summary>
        /// Gets popular code snippets
        /// </summary>
        Task<List<CodeLibrary>> GetPopularCodeAsync(int limit = 10);
        
        /// <summary>
        /// Gets recently added code snippets
        /// </summary>
        Task<List<CodeLibrary>> GetRecentCodeAsync(int limit = 10);
        
        /// <summary>
        /// Searches code library
        /// </summary>
        Task<List<CodeLibrary>> SearchCodeLibraryAsync(string query, int userId, bool includePublic = true, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Gets code execution history for a user
        /// </summary>
        Task<List<CodeExecutionHistory>> GetExecutionHistoryAsync(int userId, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Saves code execution to history
        /// </summary>
        Task<CodeExecutionHistory> SaveExecutionHistoryAsync(int userId, string code, CodeExecutionResult result);
        
        /// <summary>
        /// Validates JavaScript code for security issues
        /// </summary>
        Task<CodeValidationResult> ValidateCodeAsync(string code);
        
        /// <summary>
        /// Gets code editor settings for a user
        /// </summary>
        Task<CodeEditorSettings> GetUserSettingsAsync(int userId);
        
        /// <summary>
        /// Updates code editor settings for a user
        /// </summary>
        Task<CodeEditorSettings> UpdateUserSettingsAsync(int userId, CodeEditorSettings settings);
        
        /// <summary>
        /// Gets default code templates for different scenarios
        /// </summary>
        Task<Dictionary<string, string>> GetDefaultTemplatesAsync();
        
        /// <summary>
        /// Formats JavaScript code
        /// </summary>
        Task<string> FormatCodeAsync(string code);
        
        /// <summary>
        /// Gets code suggestions/autocomplete
        /// </summary>
        Task<List<CodeSuggestion>> GetCodeSuggestionsAsync(string partialCode, int cursorPosition);
    }
    
    public class CodeExecutionResult
    {
        public bool Success { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
        public string? ConsoleOutput { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public int MemoryUsed { get; set; }
        public Dictionary<string, object>? Variables { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }
    
    public class CodeValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> SecurityIssues { get; set; } = new();
        public bool HasSecurityRisks { get; set; }
    }
    
    public class CodeEditorSettings
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Theme { get; set; } = "vs-dark";
        public int FontSize { get; set; } = 14;
        public string FontFamily { get; set; } = "Monaco, 'Courier New', monospace";
        public bool WordWrap { get; set; } = true;
        public bool ShowLineNumbers { get; set; } = true;
        public bool ShowMinimap { get; set; } = true;
        public bool AutoSave { get; set; } = true;
        public int AutoSaveInterval { get; set; } = 30; // seconds
        public bool EnableLinting { get; set; } = true;
        public bool EnableFormatting { get; set; } = true;
        public string TabSize { get; set; } = "2";
        public bool InsertSpaces { get; set; } = true;
        public Dictionary<string, object> CustomSettings { get; set; } = new();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public class CodeExecutionHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Output { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
    }
    
    public class CodeSuggestion
    {
        public string Label { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty; // function, variable, keyword, etc.
        public string Detail { get; set; } = string.Empty;
        public string Documentation { get; set; } = string.Empty;
        public string InsertText { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
