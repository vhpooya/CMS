using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public interface ISpreadsheetService
    {
        /// <summary>
        /// Gets all spreadsheets for a user
        /// </summary>
        Task<List<Spreadsheet>> GetUserSpreadsheetsAsync(int userId);
        
        /// <summary>
        /// Gets a specific spreadsheet by ID
        /// </summary>
        Task<Spreadsheet?> GetSpreadsheetByIdAsync(int spreadsheetId, int userId);
        
        /// <summary>
        /// Creates a new spreadsheet
        /// </summary>
        Task<Spreadsheet> CreateSpreadsheetAsync(Spreadsheet spreadsheet);
        
        /// <summary>
        /// Updates an existing spreadsheet
        /// </summary>
        Task<Spreadsheet?> UpdateSpreadsheetAsync(int spreadsheetId, int userId, Spreadsheet updatedSpreadsheet);
        
        /// <summary>
        /// Deletes a spreadsheet
        /// </summary>
        Task<bool> DeleteSpreadsheetAsync(int spreadsheetId, int userId);
        
        /// <summary>
        /// Saves spreadsheet data
        /// </summary>
        Task<bool> SaveSpreadsheetDataAsync(int spreadsheetId, int userId, SpreadsheetData data);
        
        /// <summary>
        /// Gets spreadsheet data
        /// </summary>
        Task<SpreadsheetData?> GetSpreadsheetDataAsync(int spreadsheetId, int userId);
        
        /// <summary>
        /// Shares a spreadsheet with another user
        /// </summary>
        Task<SpreadsheetShare> ShareSpreadsheetAsync(int spreadsheetId, int ownerId, int targetUserId, SharePermission permission);
        
        /// <summary>
        /// Gets shared spreadsheets for a user
        /// </summary>
        Task<List<Spreadsheet>> GetSharedSpreadsheetsAsync(int userId);
        
        /// <summary>
        /// Updates share permissions
        /// </summary>
        Task<bool> UpdateSharePermissionAsync(int shareId, int ownerId, SharePermission permission);
        
        /// <summary>
        /// Removes a share
        /// </summary>
        Task<bool> RemoveShareAsync(int shareId, int ownerId);
        
        /// <summary>
        /// Creates a new version of the spreadsheet
        /// </summary>
        Task<SpreadsheetVersion> CreateVersionAsync(int spreadsheetId, int userId, string changeDescription);
        
        /// <summary>
        /// Gets version history for a spreadsheet
        /// </summary>
        Task<List<SpreadsheetVersion>> GetVersionHistoryAsync(int spreadsheetId, int userId);
        
        /// <summary>
        /// Restores a specific version
        /// </summary>
        Task<bool> RestoreVersionAsync(int spreadsheetId, int userId, int versionId);
        
        /// <summary>
        /// Exports spreadsheet to various formats
        /// </summary>
        Task<byte[]> ExportSpreadsheetAsync(int spreadsheetId, int userId, ExportFormat format);
        
        /// <summary>
        /// Imports spreadsheet from file
        /// </summary>
        Task<Spreadsheet> ImportSpreadsheetAsync(int userId, IFormFile file, string name);
        
        /// <summary>
        /// Evaluates a formula
        /// </summary>
        Task<object> EvaluateFormulaAsync(string formula, SpreadsheetData data, string cellAddress);
        
        /// <summary>
        /// Gets public spreadsheet templates
        /// </summary>
        Task<List<Spreadsheet>> GetTemplatesAsync();
        
        /// <summary>
        /// Creates a spreadsheet from template
        /// </summary>
        Task<Spreadsheet> CreateFromTemplateAsync(int templateId, int userId, string name);
    }
    
    public class SpreadsheetData
    {
        public Dictionary<string, SpreadsheetCell> Cells { get; set; } = new();
        public List<SpreadsheetChart> Charts { get; set; } = new();
        public SpreadsheetFormatting Formatting { get; set; } = new();
        public SpreadsheetSettings Settings { get; set; } = new();
    }
    
    public class SpreadsheetCell
    {
        public string Value { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string DataType { get; set; } = "text"; // text, number, date, boolean, formula
        public CellFormatting Formatting { get; set; } = new();
        public string? Comment { get; set; }
        public bool IsLocked { get; set; } = false;
    }
    
    public class CellFormatting
    {
        public string? BackgroundColor { get; set; }
        public string? TextColor { get; set; }
        public string? FontFamily { get; set; }
        public int? FontSize { get; set; }
        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;
        public bool IsUnderline { get; set; } = false;
        public string TextAlign { get; set; } = "left"; // left, center, right
        public string VerticalAlign { get; set; } = "middle"; // top, middle, bottom
        public string? BorderStyle { get; set; }
        public string? BorderColor { get; set; }
        public string? NumberFormat { get; set; }
    }
    
    public class SpreadsheetFormatting
    {
        public Dictionary<string, CellFormatting> CellFormats { get; set; } = new();
        public Dictionary<string, CellFormatting> RowFormats { get; set; } = new();
        public Dictionary<string, CellFormatting> ColumnFormats { get; set; } = new();
        public List<ConditionalFormat> ConditionalFormats { get; set; } = new();
    }
    
    public class ConditionalFormat
    {
        public string Range { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public CellFormatting Format { get; set; } = new();
    }
    
    public class SpreadsheetChart
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "column"; // column, line, pie, bar, area, scatter
        public string Title { get; set; } = string.Empty;
        public string DataRange { get; set; } = string.Empty;
        public ChartPosition Position { get; set; } = new();
        public ChartOptions Options { get; set; } = new();
    }
    
    public class ChartPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 400;
        public int Height { get; set; } = 300;
    }
    
    public class ChartOptions
    {
        public bool ShowLegend { get; set; } = true;
        public bool ShowDataLabels { get; set; } = false;
        public string? XAxisTitle { get; set; }
        public string? YAxisTitle { get; set; }
        public List<string> Colors { get; set; } = new();
    }
    
    public class SpreadsheetSettings
    {
        public bool ShowGridlines { get; set; } = true;
        public bool ShowRowHeaders { get; set; } = true;
        public bool ShowColumnHeaders { get; set; } = true;
        public int FrozenRows { get; set; } = 0;
        public int FrozenColumns { get; set; } = 0;
        public double ZoomLevel { get; set; } = 1.0;
        public string DefaultFont { get; set; } = "Arial";
        public int DefaultFontSize { get; set; } = 11;
    }
    
    public enum ExportFormat
    {
        Excel = 0,
        CSV = 1,
        PDF = 2,
        JSON = 3
    }
}
