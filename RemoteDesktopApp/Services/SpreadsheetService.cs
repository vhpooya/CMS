using Microsoft.EntityFrameworkCore;
using RemoteDesktopApp.Data;
using RemoteDesktopApp.Models;
using System.Text.Json;
using System.Text;

namespace RemoteDesktopApp.Services
{
    public class SpreadsheetService : ISpreadsheetService
    {
        private readonly RemoteDesktopDbContext _context;
        private readonly ILogger<SpreadsheetService> _logger;

        public SpreadsheetService(RemoteDesktopDbContext context, ILogger<SpreadsheetService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Spreadsheet>> GetUserSpreadsheetsAsync(int userId)
        {
            return await _context.Spreadsheets
                .Where(s => s.OwnerId == userId)
                .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Spreadsheet?> GetSpreadsheetByIdAsync(int spreadsheetId, int userId)
        {
            return await _context.Spreadsheets
                .Include(s => s.Shares)
                .FirstOrDefaultAsync(s => s.Id == spreadsheetId && 
                    (s.OwnerId == userId || s.Shares.Any(sh => sh.UserId == userId && sh.IsActive)));
        }

        public async Task<Spreadsheet> CreateSpreadsheetAsync(Spreadsheet spreadsheet)
        {
            spreadsheet.CreatedAt = DateTime.UtcNow;
            spreadsheet.Version = 1;
            
            // Initialize with empty data
            var initialData = new SpreadsheetData();
            spreadsheet.Data = JsonSerializer.Serialize(initialData);
            spreadsheet.FileSize = Encoding.UTF8.GetByteCount(spreadsheet.Data);

            _context.Spreadsheets.Add(spreadsheet);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created spreadsheet {SpreadsheetId} for user {UserId}", 
                spreadsheet.Id, spreadsheet.OwnerId);

            return spreadsheet;
        }

        public async Task<Spreadsheet?> UpdateSpreadsheetAsync(int spreadsheetId, int userId, Spreadsheet updatedSpreadsheet)
        {
            var existingSpreadsheet = await _context.Spreadsheets
                .FirstOrDefaultAsync(s => s.Id == spreadsheetId && s.OwnerId == userId);

            if (existingSpreadsheet == null)
                return null;

            existingSpreadsheet.Name = updatedSpreadsheet.Name;
            existingSpreadsheet.Description = updatedSpreadsheet.Description;
            existingSpreadsheet.Category = updatedSpreadsheet.Category;
            existingSpreadsheet.Tags = updatedSpreadsheet.Tags;
            existingSpreadsheet.IsPublic = updatedSpreadsheet.IsPublic;
            existingSpreadsheet.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated spreadsheet {SpreadsheetId} for user {UserId}", 
                spreadsheetId, userId);

            return existingSpreadsheet;
        }

        public async Task<bool> DeleteSpreadsheetAsync(int spreadsheetId, int userId)
        {
            var spreadsheet = await _context.Spreadsheets
                .FirstOrDefaultAsync(s => s.Id == spreadsheetId && s.OwnerId == userId);

            if (spreadsheet == null)
                return false;

            _context.Spreadsheets.Remove(spreadsheet);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted spreadsheet {SpreadsheetId} for user {UserId}", 
                spreadsheetId, userId);

            return true;
        }

        public async Task<bool> SaveSpreadsheetDataAsync(int spreadsheetId, int userId, SpreadsheetData data)
        {
            var spreadsheet = await GetSpreadsheetByIdAsync(spreadsheetId, userId);
            if (spreadsheet == null)
                return false;

            // Check permissions
            if (spreadsheet.OwnerId != userId)
            {
                var share = spreadsheet.Shares.FirstOrDefault(s => s.UserId == userId && s.IsActive);
                if (share == null || share.Permission < SharePermission.Edit)
                    return false;
            }

            var jsonData = JsonSerializer.Serialize(data);
            spreadsheet.Data = jsonData;
            spreadsheet.FileSize = Encoding.UTF8.GetByteCount(jsonData);
            spreadsheet.UpdatedAt = DateTime.UtcNow;
            spreadsheet.LastAccessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved data for spreadsheet {SpreadsheetId} by user {UserId}", 
                spreadsheetId, userId);

            return true;
        }

        public async Task<SpreadsheetData?> GetSpreadsheetDataAsync(int spreadsheetId, int userId)
        {
            var spreadsheet = await GetSpreadsheetByIdAsync(spreadsheetId, userId);
            if (spreadsheet == null)
                return null;

            // Update last accessed time
            spreadsheet.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (string.IsNullOrEmpty(spreadsheet.Data))
                return new SpreadsheetData();

            try
            {
                return JsonSerializer.Deserialize<SpreadsheetData>(spreadsheet.Data);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing spreadsheet data for {SpreadsheetId}", spreadsheetId);
                return new SpreadsheetData();
            }
        }

        public async Task<SpreadsheetShare> ShareSpreadsheetAsync(int spreadsheetId, int ownerId, int targetUserId, SharePermission permission)
        {
            var spreadsheet = await _context.Spreadsheets
                .FirstOrDefaultAsync(s => s.Id == spreadsheetId && s.OwnerId == ownerId);

            if (spreadsheet == null)
                throw new ArgumentException("Spreadsheet not found or access denied");

            // Check if already shared
            var existingShare = await _context.SpreadsheetShares
                .FirstOrDefaultAsync(s => s.SpreadsheetId == spreadsheetId && s.UserId == targetUserId);

            if (existingShare != null)
            {
                existingShare.Permission = permission;
                existingShare.IsActive = true;
                await _context.SaveChangesAsync();
                return existingShare;
            }

            var share = new SpreadsheetShare
            {
                SpreadsheetId = spreadsheetId,
                UserId = targetUserId,
                Permission = permission,
                SharedByUserId = ownerId,
                SharedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.SpreadsheetShares.Add(share);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Shared spreadsheet {SpreadsheetId} with user {TargetUserId} by {OwnerId}", 
                spreadsheetId, targetUserId, ownerId);

            return share;
        }

        public async Task<List<Spreadsheet>> GetSharedSpreadsheetsAsync(int userId)
        {
            return await _context.Spreadsheets
                .Include(s => s.Owner)
                .Where(s => s.Shares.Any(sh => sh.UserId == userId && sh.IsActive))
                .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateSharePermissionAsync(int shareId, int ownerId, SharePermission permission)
        {
            var share = await _context.SpreadsheetShares
                .Include(s => s.Spreadsheet)
                .FirstOrDefaultAsync(s => s.Id == shareId && s.Spreadsheet.OwnerId == ownerId);

            if (share == null)
                return false;

            share.Permission = permission;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveShareAsync(int shareId, int ownerId)
        {
            var share = await _context.SpreadsheetShares
                .Include(s => s.Spreadsheet)
                .FirstOrDefaultAsync(s => s.Id == shareId && s.Spreadsheet.OwnerId == ownerId);

            if (share == null)
                return false;

            share.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<SpreadsheetVersion> CreateVersionAsync(int spreadsheetId, int userId, string changeDescription)
        {
            var spreadsheet = await GetSpreadsheetByIdAsync(spreadsheetId, userId);
            if (spreadsheet == null)
                throw new ArgumentException("Spreadsheet not found or access denied");

            var version = new SpreadsheetVersion
            {
                SpreadsheetId = spreadsheetId,
                Version = spreadsheet.Version + 1,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                ChangeDescription = changeDescription,
                Data = spreadsheet.Data,
                Formatting = spreadsheet.Formatting,
                FileSize = spreadsheet.FileSize
            };

            _context.SpreadsheetVersions.Add(version);
            
            // Update spreadsheet version
            spreadsheet.Version = version.Version;
            
            await _context.SaveChangesAsync();

            return version;
        }

        public async Task<List<SpreadsheetVersion>> GetVersionHistoryAsync(int spreadsheetId, int userId)
        {
            var spreadsheet = await GetSpreadsheetByIdAsync(spreadsheetId, userId);
            if (spreadsheet == null)
                return new List<SpreadsheetVersion>();

            return await _context.SpreadsheetVersions
                .Include(v => v.CreatedBy)
                .Where(v => v.SpreadsheetId == spreadsheetId)
                .OrderByDescending(v => v.Version)
                .ToListAsync();
        }

        public async Task<bool> RestoreVersionAsync(int spreadsheetId, int userId, int versionId)
        {
            var spreadsheet = await GetSpreadsheetByIdAsync(spreadsheetId, userId);
            if (spreadsheet == null || spreadsheet.OwnerId != userId)
                return false;

            var version = await _context.SpreadsheetVersions
                .FirstOrDefaultAsync(v => v.Id == versionId && v.SpreadsheetId == spreadsheetId);

            if (version == null)
                return false;

            // Create a new version before restoring
            await CreateVersionAsync(spreadsheetId, userId, $"Restored to version {version.Version}");

            // Restore data
            spreadsheet.Data = version.Data;
            spreadsheet.Formatting = version.Formatting;
            spreadsheet.FileSize = version.FileSize;
            spreadsheet.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<byte[]> ExportSpreadsheetAsync(int spreadsheetId, int userId, ExportFormat format)
        {
            var data = await GetSpreadsheetDataAsync(spreadsheetId, userId);
            if (data == null)
                throw new ArgumentException("Spreadsheet not found or access denied");

            return format switch
            {
                ExportFormat.CSV => ExportToCsv(data),
                ExportFormat.JSON => ExportToJson(data),
                ExportFormat.Excel => ExportToExcel(data),
                ExportFormat.PDF => ExportToPdf(data),
                _ => throw new ArgumentException("Unsupported export format")
            };
        }

        public async Task<Spreadsheet> ImportSpreadsheetAsync(int userId, IFormFile file, string name)
        {
            // TODO: Implement file parsing based on file type
            var spreadsheet = new Spreadsheet
            {
                OwnerId = userId,
                Name = name,
                Description = $"Imported from {file.FileName}",
                CreatedAt = DateTime.UtcNow
            };

            return await CreateSpreadsheetAsync(spreadsheet);
        }

        public async Task<object> EvaluateFormulaAsync(string formula, SpreadsheetData data, string cellAddress)
        {
            // TODO: Implement formula evaluation engine
            // This is a simplified version - in production, you'd use a proper formula parser
            
            if (!formula.StartsWith("="))
                return formula;

            try
            {
                // Simple SUM function example
                if (formula.StartsWith("=SUM(") && formula.EndsWith(")"))
                {
                    var range = formula.Substring(5, formula.Length - 6);
                    return await EvaluateSumFunction(range, data);
                }

                // Add more formula functions here
                return formula;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating formula {Formula} in cell {Cell}", formula, cellAddress);
                return "#ERROR!";
            }
        }

        public async Task<List<Spreadsheet>> GetTemplatesAsync()
        {
            return await _context.Spreadsheets
                .Where(s => s.IsTemplate && s.IsPublic)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Spreadsheet> CreateFromTemplateAsync(int templateId, int userId, string name)
        {
            var template = await _context.Spreadsheets
                .FirstOrDefaultAsync(s => s.Id == templateId && s.IsTemplate && s.IsPublic);

            if (template == null)
                throw new ArgumentException("Template not found");

            var spreadsheet = new Spreadsheet
            {
                OwnerId = userId,
                Name = name,
                Description = $"Created from template: {template.Name}",
                Category = template.Category,
                Data = template.Data,
                Formatting = template.Formatting,
                Charts = template.Charts,
                RowCount = template.RowCount,
                ColumnCount = template.ColumnCount,
                FileSize = template.FileSize
            };

            return await CreateSpreadsheetAsync(spreadsheet);
        }

        // Helper methods
        private byte[] ExportToCsv(SpreadsheetData data)
        {
            var csv = new StringBuilder();
            
            // Simple CSV export - in production, you'd handle proper CSV formatting
            foreach (var cell in data.Cells.OrderBy(c => c.Key))
            {
                csv.AppendLine($"{cell.Key},{cell.Value.Value}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] ExportToJson(SpreadsheetData data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            return Encoding.UTF8.GetBytes(json);
        }

        private byte[] ExportToExcel(SpreadsheetData data)
        {
            // TODO: Implement Excel export using a library like EPPlus
            throw new NotImplementedException("Excel export not implemented yet");
        }

        private byte[] ExportToPdf(SpreadsheetData data)
        {
            // TODO: Implement PDF export
            throw new NotImplementedException("PDF export not implemented yet");
        }

        private async Task<double> EvaluateSumFunction(string range, SpreadsheetData data)
        {
            // Simple range parsing - in production, you'd use a proper range parser
            double sum = 0;

            foreach (var cell in data.Cells)
            {
                if (double.TryParse(cell.Value.Value, out var value))
                {
                    sum += value;
                }
            }

            return await Task.FromResult(sum);
        }
    }
}
