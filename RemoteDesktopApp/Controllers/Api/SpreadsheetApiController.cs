using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteDesktopApp.Models;
using RemoteDesktopApp.Services;

namespace RemoteDesktopApp.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/spreadsheet")]
    public class SpreadsheetApiController : ControllerBase
    {
        private readonly ISpreadsheetService _spreadsheetService;
        private readonly ILogger<SpreadsheetApiController> _logger;

        public SpreadsheetApiController(ISpreadsheetService spreadsheetService, ILogger<SpreadsheetApiController> logger)
        {
            _spreadsheetService = spreadsheetService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Spreadsheet>>> GetSpreadsheets()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var spreadsheets = await _spreadsheetService.GetUserSpreadsheetsAsync(userId.Value);
            return Ok(spreadsheets);
        }

        [HttpGet("shared")]
        public async Task<ActionResult<List<Spreadsheet>>> GetSharedSpreadsheets()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var spreadsheets = await _spreadsheetService.GetSharedSpreadsheetsAsync(userId.Value);
            return Ok(spreadsheets);
        }

        [HttpGet("templates")]
        public async Task<ActionResult<List<Spreadsheet>>> GetTemplates()
        {
            var templates = await _spreadsheetService.GetTemplatesAsync();
            return Ok(templates);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Spreadsheet>> GetSpreadsheet(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var spreadsheet = await _spreadsheetService.GetSpreadsheetByIdAsync(id, userId.Value);
            if (spreadsheet == null)
                return NotFound();

            return Ok(spreadsheet);
        }

        [HttpGet("{id}/data")]
        public async Task<ActionResult<SpreadsheetData>> GetSpreadsheetData(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var data = await _spreadsheetService.GetSpreadsheetDataAsync(id, userId.Value);
            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<ActionResult<Spreadsheet>> CreateSpreadsheet([FromBody] CreateSpreadsheetRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var spreadsheet = new Spreadsheet
                {
                    OwnerId = userId.Value,
                    Name = request.Name,
                    Description = request.Description,
                    Category = request.Category,
                    Tags = request.Tags,
                    IsPublic = request.IsPublic,
                    RowCount = request.RowCount > 0 ? request.RowCount : 100,
                    ColumnCount = request.ColumnCount > 0 ? request.ColumnCount : 26
                };

                var createdSpreadsheet = await _spreadsheetService.CreateSpreadsheetAsync(spreadsheet);
                return CreatedAtAction(nameof(GetSpreadsheet), new { id = createdSpreadsheet.Id }, createdSpreadsheet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating spreadsheet for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while creating the spreadsheet" });
            }
        }

        [HttpPost("{templateId}/create-from-template")]
        public async Task<ActionResult<Spreadsheet>> CreateFromTemplate(int templateId, [FromBody] CreateFromTemplateRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var spreadsheet = await _spreadsheetService.CreateFromTemplateAsync(templateId, userId.Value, request.Name);
                return CreatedAtAction(nameof(GetSpreadsheet), new { id = spreadsheet.Id }, spreadsheet);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating spreadsheet from template {TemplateId} for user {UserId}", templateId, userId);
                return StatusCode(500, new { message = "An error occurred while creating the spreadsheet" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Spreadsheet>> UpdateSpreadsheet(int id, [FromBody] UpdateSpreadsheetRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var updatedSpreadsheet = new Spreadsheet
                {
                    Name = request.Name,
                    Description = request.Description,
                    Category = request.Category,
                    Tags = request.Tags,
                    IsPublic = request.IsPublic
                };

                var result = await _spreadsheetService.UpdateSpreadsheetAsync(id, userId.Value, updatedSpreadsheet);
                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating spreadsheet {SpreadsheetId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while updating the spreadsheet" });
            }
        }

        [HttpPost("{id}/data")]
        public async Task<ActionResult> SaveSpreadsheetData(int id, [FromBody] SpreadsheetData data)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _spreadsheetService.SaveSpreadsheetDataAsync(id, userId.Value, data);
            if (!success)
                return NotFound();

            return Ok(new { message = "Data saved successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSpreadsheet(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _spreadsheetService.DeleteSpreadsheetAsync(id, userId.Value);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpPost("{id}/share")]
        public async Task<ActionResult<SpreadsheetShare>> ShareSpreadsheet(int id, [FromBody] ShareSpreadsheetRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var share = await _spreadsheetService.ShareSpreadsheetAsync(id, userId.Value, request.TargetUserId, request.Permission);
                return Ok(share);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing spreadsheet {SpreadsheetId} with user {TargetUserId}", id, request.TargetUserId);
                return StatusCode(500, new { message = "An error occurred while sharing the spreadsheet" });
            }
        }

        [HttpGet("{id}/versions")]
        public async Task<ActionResult<List<SpreadsheetVersion>>> GetVersionHistory(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var versions = await _spreadsheetService.GetVersionHistoryAsync(id, userId.Value);
            return Ok(versions);
        }

        [HttpPost("{id}/versions")]
        public async Task<ActionResult<SpreadsheetVersion>> CreateVersion(int id, [FromBody] CreateVersionRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var version = await _spreadsheetService.CreateVersionAsync(id, userId.Value, request.ChangeDescription);
                return Ok(version);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating version for spreadsheet {SpreadsheetId}", id);
                return StatusCode(500, new { message = "An error occurred while creating the version" });
            }
        }

        [HttpPost("{id}/versions/{versionId}/restore")]
        public async Task<ActionResult> RestoreVersion(int id, int versionId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _spreadsheetService.RestoreVersionAsync(id, userId.Value, versionId);
            if (!success)
                return NotFound();

            return Ok(new { message = "Version restored successfully" });
        }

        [HttpGet("{id}/export")]
        public async Task<ActionResult> ExportSpreadsheet(int id, [FromQuery] ExportFormat format = ExportFormat.CSV)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var data = await _spreadsheetService.ExportSpreadsheetAsync(id, userId.Value, format);
                
                var contentType = format switch
                {
                    ExportFormat.CSV => "text/csv",
                    ExportFormat.JSON => "application/json",
                    ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ExportFormat.PDF => "application/pdf",
                    _ => "application/octet-stream"
                };

                var fileName = $"spreadsheet_{id}.{format.ToString().ToLower()}";
                return File(data, contentType, fileName);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting spreadsheet {SpreadsheetId}", id);
                return StatusCode(500, new { message = "An error occurred while exporting the spreadsheet" });
            }
        }

        [HttpPost("import")]
        public async Task<ActionResult<Spreadsheet>> ImportSpreadsheet([FromForm] ImportSpreadsheetRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var spreadsheet = await _spreadsheetService.ImportSpreadsheetAsync(userId.Value, request.File, request.Name);
                return CreatedAtAction(nameof(GetSpreadsheet), new { id = spreadsheet.Id }, spreadsheet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing spreadsheet for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while importing the spreadsheet" });
            }
        }

        [HttpPost("{id}/evaluate-formula")]
        public async Task<ActionResult<object>> EvaluateFormula(int id, [FromBody] EvaluateFormulaRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var data = await _spreadsheetService.GetSpreadsheetDataAsync(id, userId.Value);
                if (data == null)
                    return NotFound();

                var result = await _spreadsheetService.EvaluateFormulaAsync(request.Formula, data, request.CellAddress);
                return Ok(new { result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating formula in spreadsheet {SpreadsheetId}", id);
                return StatusCode(500, new { message = "An error occurred while evaluating the formula" });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }
    }

    // Request DTOs
    public class CreateSpreadsheetRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Tags { get; set; }
        public bool IsPublic { get; set; } = false;
        public int RowCount { get; set; } = 100;
        public int ColumnCount { get; set; } = 26;
    }

    public class UpdateSpreadsheetRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Tags { get; set; }
        public bool IsPublic { get; set; } = false;
    }

    public class CreateFromTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ShareSpreadsheetRequest
    {
        public int TargetUserId { get; set; }
        public SharePermission Permission { get; set; }
    }

    public class CreateVersionRequest
    {
        public string ChangeDescription { get; set; } = string.Empty;
    }

    public class ImportSpreadsheetRequest
    {
        public IFormFile File { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
    }

    public class EvaluateFormulaRequest
    {
        public string Formula { get; set; } = string.Empty;
        public string CellAddress { get; set; } = string.Empty;
    }
}
