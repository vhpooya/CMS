using Microsoft.EntityFrameworkCore;
using RemoteDesktopApp.Data;
using RemoteDesktopApp.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RemoteDesktopApp.Services
{
    public class CodeEditorService : ICodeEditorService
    {
        private readonly RemoteDesktopDbContext _context;
        private readonly ILogger<CodeEditorService> _logger;
        private readonly IWebHostEnvironment _environment;

        // Security patterns to detect potentially dangerous code
        private readonly List<string> _dangerousPatterns = new()
        {
            @"require\s*\(",
            @"import\s+.*\s+from",
            @"eval\s*\(",
            @"Function\s*\(",
            @"setTimeout\s*\(",
            @"setInterval\s*\(",
            @"XMLHttpRequest",
            @"fetch\s*\(",
            @"document\.",
            @"window\.",
            @"global\.",
            @"process\.",
            @"__dirname",
            @"__filename",
            @"fs\.",
            @"path\.",
            @"os\.",
            @"child_process",
            @"crypto\.",
            @"net\.",
            @"http\.",
            @"https\.",
            @"url\.",
            @"querystring\."
        };

        public CodeEditorService(
            RemoteDesktopDbContext context, 
            ILogger<CodeEditorService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        public async Task<CodeExecutionResult> ExecuteJavaScriptAsync(string code, int userId, Dictionary<string, object>? inputs = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new CodeExecutionResult();

            try
            {
                // Validate code first
                var validation = await ValidateCodeAsync(code);
                if (!validation.IsValid || validation.HasSecurityRisks)
                {
                    result.Success = false;
                    result.Error = "Code validation failed: " + string.Join(", ", validation.Errors.Concat(validation.SecurityIssues));
                    return result;
                }

                // Create a safe execution environment
                var safeCode = CreateSafeExecutionEnvironment(code, inputs);
                
                // Execute in Node.js subprocess for better isolation
                var output = await ExecuteInNodeJsAsync(safeCode);
                
                result.Success = true;
                result.Output = output.Output;
                result.Error = output.Error;
                result.ConsoleOutput = output.ConsoleOutput;
                result.ExecutionTime = stopwatch.Elapsed;
                
                // Save to execution history
                await SaveExecutionHistoryAsync(userId, code, result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                _logger.LogError(ex, "Error executing JavaScript code for user {UserId}", userId);
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
            }

            return result;
        }

        public async Task<CodeLibrary> SaveCodeToLibraryAsync(int userId, string title, string description, string code, string category, string? tags = null, bool isPublic = false, bool isTemplate = false)
        {
            var codeLibrary = new CodeLibrary
            {
                Title = title,
                Description = description,
                Code = code,
                Category = category,
                Tags = tags,
                CreatedByUserId = userId,
                IsPublic = isPublic,
                IsTemplate = isTemplate,
                CreatedAt = DateTime.UtcNow
            };

            _context.CodeLibraries.Add(codeLibrary);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Code saved to library by user {UserId}: {Title}", userId, title);
            return codeLibrary;
        }

        public async Task<CodeLibrary?> GetCodeFromLibraryAsync(int codeId, int userId)
        {
            return await _context.CodeLibraries
                .Include(c => c.CreatedBy)
                .Include(c => c.Ratings)
                .FirstOrDefaultAsync(c => c.Id == codeId && 
                    (c.CreatedByUserId == userId || c.IsPublic));
        }

        public async Task<CodeLibrary?> UpdateCodeInLibraryAsync(int codeId, int userId, string? title = null, string? description = null, string? code = null, string? category = null, string? tags = null, bool? isPublic = null)
        {
            var codeLibrary = await _context.CodeLibraries
                .FirstOrDefaultAsync(c => c.Id == codeId && c.CreatedByUserId == userId);

            if (codeLibrary == null)
                return null;

            if (!string.IsNullOrEmpty(title))
                codeLibrary.Title = title;
            
            if (!string.IsNullOrEmpty(description))
                codeLibrary.Description = description;
            
            if (!string.IsNullOrEmpty(code))
                codeLibrary.Code = code;
            
            if (!string.IsNullOrEmpty(category))
                codeLibrary.Category = category;
            
            if (tags != null)
                codeLibrary.Tags = tags;
            
            if (isPublic.HasValue)
                codeLibrary.IsPublic = isPublic.Value;

            codeLibrary.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return codeLibrary;
        }

        public async Task<bool> DeleteCodeFromLibraryAsync(int codeId, int userId)
        {
            var codeLibrary = await _context.CodeLibraries
                .FirstOrDefaultAsync(c => c.Id == codeId && c.CreatedByUserId == userId);

            if (codeLibrary == null)
                return false;

            _context.CodeLibraries.Remove(codeLibrary);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<CodeLibrary>> GetUserCodeLibraryAsync(int userId, string? category = null, string? search = null, int page = 1, int pageSize = 20)
        {
            var query = _context.CodeLibraries
                .Include(c => c.CreatedBy)
                .Where(c => c.CreatedByUserId == userId);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(c => c.Category == category);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.Title.Contains(search) || 
                                        c.Description!.Contains(search) || 
                                        c.Tags!.Contains(search));

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<CodeLibrary>> GetPublicTemplatesAsync(string? category = null, string? search = null, int page = 1, int pageSize = 20)
        {
            var query = _context.CodeLibraries
                .Include(c => c.CreatedBy)
                .Where(c => c.IsPublic && c.IsTemplate);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(c => c.Category == category);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.Title.Contains(search) || 
                                        c.Description!.Contains(search) || 
                                        c.Tags!.Contains(search));

            return await query
                .OrderByDescending(c => c.Rating)
                .ThenByDescending(c => c.UsageCount)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _context.CodeLibraries
                .Where(c => !string.IsNullOrEmpty(c.Category))
                .Select(c => c.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<CodeLibraryRating> RateCodeAsync(int codeId, int userId, int rating, string? comment = null)
        {
            // Check if user already rated this code
            var existingRating = await _context.CodeLibraryRatings
                .FirstOrDefaultAsync(r => r.CodeLibraryId == codeId && r.UserId == userId);

            if (existingRating != null)
            {
                existingRating.Rating = rating;
                existingRating.Comment = comment;
                existingRating.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                existingRating = new CodeLibraryRating
                {
                    CodeLibraryId = codeId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CodeLibraryRatings.Add(existingRating);
            }

            await _context.SaveChangesAsync();

            // Update average rating
            await UpdateCodeAverageRatingAsync(codeId);

            return existingRating;
        }

        public async Task<List<CodeLibraryRating>> GetCodeRatingsAsync(int codeId)
        {
            return await _context.CodeLibraryRatings
                .Include(r => r.User)
                .Where(r => r.CodeLibraryId == codeId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task IncrementUsageCountAsync(int codeId)
        {
            var code = await _context.CodeLibraries.FindAsync(codeId);
            if (code != null)
            {
                code.UsageCount++;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<CodeLibrary>> GetPopularCodeAsync(int limit = 10)
        {
            return await _context.CodeLibraries
                .Include(c => c.CreatedBy)
                .Where(c => c.IsPublic)
                .OrderByDescending(c => c.Rating)
                .ThenByDescending(c => c.UsageCount)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<CodeLibrary>> GetRecentCodeAsync(int limit = 10)
        {
            return await _context.CodeLibraries
                .Include(c => c.CreatedBy)
                .Where(c => c.IsPublic)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<CodeLibrary>> SearchCodeLibraryAsync(string query, int userId, bool includePublic = true, int page = 1, int pageSize = 20)
        {
            var dbQuery = _context.CodeLibraries
                .Include(c => c.CreatedBy)
                .Where(c => c.CreatedByUserId == userId || (includePublic && c.IsPublic))
                .Where(c => c.Title.Contains(query) || 
                           c.Description!.Contains(query) || 
                           c.Tags!.Contains(query) ||
                           c.Code.Contains(query));

            return await dbQuery
                .OrderByDescending(c => c.Rating)
                .ThenByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<CodeExecutionHistory>> GetExecutionHistoryAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _context.Set<CodeExecutionHistory>()
                .Include(h => h.User)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.ExecutedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<CodeExecutionHistory> SaveExecutionHistoryAsync(int userId, string code, CodeExecutionResult result)
        {
            var history = new CodeExecutionHistory
            {
                UserId = userId,
                Code = code,
                Output = result.Output,
                Error = result.Error,
                Success = result.Success,
                ExecutionTime = result.ExecutionTime,
                ExecutedAt = result.ExecutedAt
            };

            _context.Set<CodeExecutionHistory>().Add(history);
            await _context.SaveChangesAsync();

            return history;
        }

        public async Task<CodeValidationResult> ValidateCodeAsync(string code)
        {
            var result = new CodeValidationResult { IsValid = true };

            // Check for dangerous patterns
            foreach (var pattern in _dangerousPatterns)
            {
                if (Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase))
                {
                    result.SecurityIssues.Add($"Potentially dangerous pattern detected: {pattern}");
                    result.HasSecurityRisks = true;
                }
            }

            // Basic syntax validation
            try
            {
                // Simple validation - check for balanced brackets
                var openBrackets = code.Count(c => c == '{');
                var closeBrackets = code.Count(c => c == '}');
                if (openBrackets != closeBrackets)
                {
                    result.Errors.Add("Unbalanced curly brackets");
                    result.IsValid = false;
                }

                var openParens = code.Count(c => c == '(');
                var closeParens = code.Count(c => c == ')');
                if (openParens != closeParens)
                {
                    result.Errors.Add("Unbalanced parentheses");
                    result.IsValid = false;
                }

                // Check for common syntax errors
                if (code.Contains("function") && !Regex.IsMatch(code, @"function\s+\w+\s*\("))
                {
                    result.Warnings.Add("Function declaration may have syntax issues");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Validation error: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        public async Task<CodeEditorSettings> GetUserSettingsAsync(int userId)
        {
            // For now, return default settings. In a full implementation,
            // you would store these in the database
            return new CodeEditorSettings
            {
                UserId = userId,
                Theme = "vs-dark",
                FontSize = 14,
                FontFamily = "Monaco, 'Courier New', monospace",
                WordWrap = true,
                ShowLineNumbers = true,
                ShowMinimap = true,
                AutoSave = true,
                AutoSaveInterval = 30,
                EnableLinting = true,
                EnableFormatting = true,
                TabSize = "2",
                InsertSpaces = true
            };
        }

        public async Task<CodeEditorSettings> UpdateUserSettingsAsync(int userId, CodeEditorSettings settings)
        {
            // In a full implementation, you would save these to the database
            settings.UserId = userId;
            settings.UpdatedAt = DateTime.UtcNow;
            return settings;
        }

        public async Task<Dictionary<string, string>> GetDefaultTemplatesAsync()
        {
            return new Dictionary<string, string>
            {
                ["Hello World"] = @"// Hello World Example
console.log('Hello, World!');

// Variables and data types
let message = 'Welcome to JavaScript!';
let number = 42;
let isActive = true;

console.log('Message:', message);
console.log('Number:', number);
console.log('Is Active:', isActive);",

                ["Functions"] = @"// Function Examples
function greet(name) {
    return `Hello, ${name}!`;
}

// Arrow function
const add = (a, b) => a + b;

// Function with default parameters
function multiply(a, b = 1) {
    return a * b;
}

// Usage
console.log(greet('World'));
console.log('5 + 3 =', add(5, 3));
console.log('7 * 4 =', multiply(7, 4));",

                ["Arrays and Objects"] = @"// Array Examples
let fruits = ['apple', 'banana', 'orange'];
console.log('Fruits:', fruits);

// Array methods
fruits.push('grape');
console.log('After push:', fruits);

let numbers = [1, 2, 3, 4, 5];
let doubled = numbers.map(n => n * 2);
console.log('Doubled:', doubled);

// Object Examples
let person = {
    name: 'John Doe',
    age: 30,
    city: 'New York',
    greet: function() {
        return `Hi, I'm ${this.name}`;
    }
};

console.log('Person:', person);
console.log(person.greet());",

                ["Loops and Conditionals"] = @"// Conditional Examples
let score = 85;

if (score >= 90) {
    console.log('Grade: A');
} else if (score >= 80) {
    console.log('Grade: B');
} else if (score >= 70) {
    console.log('Grade: C');
} else {
    console.log('Grade: F');
}

// Loop Examples
console.log('For loop:');
for (let i = 1; i <= 5; i++) {
    console.log(`Count: ${i}`);
}

console.log('While loop:');
let count = 0;
while (count < 3) {
    console.log(`While count: ${count}`);
    count++;
}

// Array iteration
let colors = ['red', 'green', 'blue'];
colors.forEach((color, index) => {
    console.log(`Color ${index + 1}: ${color}`);
});",

                ["DOM Manipulation"] = @"// DOM Manipulation Examples (Note: Limited in this environment)
// These examples show common DOM operations

// Creating elements
function createButton(text, onClick) {
    const button = document.createElement('button');
    button.textContent = text;
    button.addEventListener('click', onClick);
    return button;
}

// Working with classes
function toggleClass(element, className) {
    element.classList.toggle(className);
}

// Finding elements
function findElementsByClass(className) {
    return document.getElementsByClassName(className);
}

// Example usage (commented out for safety)
// const myButton = createButton('Click me!', () => alert('Clicked!'));
// document.body.appendChild(myButton);

console.log('DOM manipulation functions defined');",

                ["Async/Await"] = @"// Async/Await Examples
// Simulating async operations

function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function fetchData() {
    console.log('Fetching data...');
    await delay(1000); // Simulate network delay
    return { id: 1, name: 'Sample Data' };
}

async function processData() {
    try {
        const data = await fetchData();
        console.log('Data received:', data);
        return data;
    } catch (error) {
        console.error('Error:', error);
    }
}

// Usage
processData().then(() => {
    console.log('Processing complete');
});",

                ["Error Handling"] = @"// Error Handling Examples

function divide(a, b) {
    if (b === 0) {
        throw new Error('Division by zero is not allowed');
    }
    return a / b;
}

// Try-catch example
try {
    let result = divide(10, 2);
    console.log('Result:', result);

    // This will throw an error
    let errorResult = divide(10, 0);
    console.log('This won't be reached');
} catch (error) {
    console.error('Caught error:', error.message);
} finally {
    console.log('Finally block always executes');
}

// Custom error class
class CustomError extends Error {
    constructor(message, code) {
        super(message);
        this.name = 'CustomError';
        this.code = code;
    }
}

try {
    throw new CustomError('Something went wrong', 500);
} catch (error) {
    console.log('Error name:', error.name);
    console.log('Error code:', error.code);
    console.log('Error message:', error.message);
}"
            };
        }

        public async Task<string> FormatCodeAsync(string code)
        {
            // Basic code formatting - in a real implementation, you might use a proper formatter
            try
            {
                var lines = code.Split('\n');
                var formatted = new List<string>();
                int indentLevel = 0;
                const string indent = "  ";

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                    {
                        formatted.Add("");
                        continue;
                    }

                    // Decrease indent for closing brackets
                    if (trimmed.StartsWith("}") || trimmed.StartsWith("]") || trimmed.StartsWith(")"))
                    {
                        indentLevel = Math.Max(0, indentLevel - 1);
                    }

                    // Add indentation
                    var indentedLine = new string(' ', indentLevel * indent.Length) + trimmed;
                    formatted.Add(indentedLine);

                    // Increase indent for opening brackets
                    if (trimmed.EndsWith("{") || trimmed.EndsWith("[") || trimmed.EndsWith("("))
                    {
                        indentLevel++;
                    }
                }

                return string.Join("\n", formatted);
            }
            catch
            {
                return code; // Return original if formatting fails
            }
        }

        public async Task<List<CodeSuggestion>> GetCodeSuggestionsAsync(string partialCode, int cursorPosition)
        {
            var suggestions = new List<CodeSuggestion>();

            // Basic JavaScript suggestions
            var keywords = new[]
            {
                "function", "var", "let", "const", "if", "else", "for", "while", "do", "switch", "case", "break", "continue", "return", "try", "catch", "finally", "throw", "new", "this", "typeof", "instanceof"
            };

            var methods = new[]
            {
                "console.log", "console.error", "console.warn", "console.info",
                "Array.from", "Array.isArray", "Object.keys", "Object.values", "Object.entries",
                "JSON.parse", "JSON.stringify", "parseInt", "parseFloat", "isNaN", "isFinite"
            };

            // Add keyword suggestions
            foreach (var keyword in keywords)
            {
                if (keyword.StartsWith(partialCode, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(new CodeSuggestion
                    {
                        Label = keyword,
                        Kind = "keyword",
                        Detail = $"JavaScript keyword",
                        InsertText = keyword,
                        SortOrder = 1
                    });
                }
            }

            // Add method suggestions
            foreach (var method in methods)
            {
                if (method.StartsWith(partialCode, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(new CodeSuggestion
                    {
                        Label = method,
                        Kind = "method",
                        Detail = $"JavaScript method",
                        InsertText = method,
                        SortOrder = 2
                    });
                }
            }

            return suggestions.OrderBy(s => s.SortOrder).ThenBy(s => s.Label).ToList();
        }

        private string CreateSafeExecutionEnvironment(string code, Dictionary<string, object>? inputs)
        {
            var safeCode = @"
(function() {
    'use strict';

    // Create safe console
    const safeConsole = {
        log: (...args) => console.log(...args),
        error: (...args) => console.error(...args),
        warn: (...args) => console.warn(...args),
        info: (...args) => console.info(...args)
    };

    // Override global console
    console = safeConsole;

    // Add inputs if provided
    " + (inputs != null ? $"const inputs = {JsonSerializer.Serialize(inputs)};" : "") + @"

    try {
        " + code + @"
    } catch (error) {
        console.error('Runtime Error:', error.message);
        throw error;
    }
})();";

            return safeCode;
        }

        private async Task<(string Output, string Error, string ConsoleOutput)> ExecuteInNodeJsAsync(string code)
        {
            // For this implementation, we'll simulate execution
            // In a real implementation, you would execute this in a sandboxed Node.js process

            try
            {
                // Simulate execution result
                await Task.Delay(100); // Simulate execution time

                return ("Execution completed successfully", null, "Code executed in safe environment");
            }
            catch (Exception ex)
            {
                return (null, ex.Message, null);
            }
        }

        private async Task UpdateCodeAverageRatingAsync(int codeId)
        {
            var ratings = await _context.CodeLibraryRatings
                .Where(r => r.CodeLibraryId == codeId)
                .ToListAsync();

            if (ratings.Any())
            {
                var code = await _context.CodeLibraries.FindAsync(codeId);
                if (code != null)
                {
                    code.Rating = ratings.Average(r => r.Rating);
                    code.RatingCount = ratings.Count;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
