using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteDesktopApp.Models;
using RemoteDesktopApp.Services;

namespace RemoteDesktopApp.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/unit")]
    public class UnitApiController : ControllerBase
    {
        private readonly IUnitService _unitService;
        private readonly IUserService _userService;
        private readonly ILogger<UnitApiController> _logger;

        public UnitApiController(IUnitService unitService, IUserService userService, ILogger<UnitApiController> logger)
        {
            _unitService = unitService;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Unit>>> GetAllUnits()
        {
            try
            {
                var units = await _unitService.GetAllUnitsAsync();
                return Ok(units);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all units");
                return StatusCode(500, new { message = "Error retrieving units" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Unit>> GetUnit(int id)
        {
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);
                if (unit == null)
                    return NotFound(new { message = "Unit not found" });

                return Ok(unit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unit {UnitId}", id);
                return StatusCode(500, new { message = "Error retrieving unit" });
            }
        }

        [HttpGet("hierarchy")]
        public async Task<ActionResult<List<Unit>>> GetUnitsHierarchy()
        {
            try
            {
                var rootUnits = await _unitService.GetRootUnitsAsync();
                return Ok(rootUnits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting units hierarchy");
                return StatusCode(500, new { message = "Error retrieving units hierarchy" });
            }
        }

        [HttpGet("{id}/subunits")]
        public async Task<ActionResult<List<Unit>>> GetSubUnits(int id)
        {
            try
            {
                var subUnits = await _unitService.GetSubUnitsAsync(id);
                return Ok(subUnits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sub-units for unit {UnitId}", id);
                return StatusCode(500, new { message = "Error retrieving sub-units" });
            }
        }

        [HttpGet("{id}/users")]
        public async Task<ActionResult<List<User>>> GetUnitUsers(int id)
        {
            try
            {
                var users = await _unitService.GetUnitUsersAsync(id);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users for unit {UnitId}", id);
                return StatusCode(500, new { message = "Error retrieving unit users" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Unit>> CreateUnit([FromBody] CreateUnitRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized();

                var unit = new Unit
                {
                    Name = request.Name,
                    Description = request.Description,
                    Code = request.Code,
                    ParentUnitId = request.ParentUnitId,
                    ManagerId = request.ManagerId,
                    Location = request.Location,
                    PhoneExtension = request.PhoneExtension,
                    Email = request.Email,
                    CreatedByUserId = currentUserId.Value
                };

                var createdUnit = await _unitService.CreateUnitAsync(unit);
                return CreatedAtAction(nameof(GetUnit), new { id = createdUnit.Id }, createdUnit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating unit");
                return StatusCode(500, new { message = "Error creating unit" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Unit>> UpdateUnit(int id, [FromBody] UpdateUnitRequest request)
        {
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);
                if (unit == null)
                    return NotFound(new { message = "Unit not found" });

                unit.Name = request.Name;
                unit.Description = request.Description;
                unit.Code = request.Code;
                unit.ParentUnitId = request.ParentUnitId;
                unit.ManagerId = request.ManagerId;
                unit.Location = request.Location;
                unit.PhoneExtension = request.PhoneExtension;
                unit.Email = request.Email;

                var updatedUnit = await _unitService.UpdateUnitAsync(unit);
                return Ok(updatedUnit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unit {UnitId}", id);
                return StatusCode(500, new { message = "Error updating unit" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteUnit(int id)
        {
            try
            {
                var canDelete = await _unitService.CanDeleteUnitAsync(id);
                if (!canDelete)
                    return BadRequest(new { message = "Cannot delete unit. It has sub-units or users assigned." });

                var deleted = await _unitService.DeleteUnitAsync(id);
                if (!deleted)
                    return NotFound(new { message = "Unit not found" });

                return Ok(new { message = "Unit deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting unit {UnitId}", id);
                return StatusCode(500, new { message = "Error deleting unit" });
            }
        }

        [HttpPost("{unitId}/users/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignUserToUnit(int unitId, int userId)
        {
            try
            {
                var success = await _unitService.AssignUserToUnitAsync(userId, unitId);
                if (!success)
                    return BadRequest(new { message = "Failed to assign user to unit" });

                return Ok(new { message = "User assigned to unit successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning user {UserId} to unit {UnitId}", userId, unitId);
                return StatusCode(500, new { message = "Error assigning user to unit" });
            }
        }

        [HttpDelete("users/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RemoveUserFromUnit(int userId)
        {
            try
            {
                var success = await _unitService.RemoveUserFromUnitAsync(userId);
                if (!success)
                    return BadRequest(new { message = "Failed to remove user from unit" });

                return Ok(new { message = "User removed from unit successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from unit", userId);
                return StatusCode(500, new { message = "Error removing user from unit" });
            }
        }

        [HttpGet("{id}/links")]
        public async Task<ActionResult<List<UnitLink>>> GetUnitLinks(int id)
        {
            try
            {
                var links = await _unitService.GetUnitLinksAsync(id);
                return Ok(links);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting links for unit {UnitId}", id);
                return StatusCode(500, new { message = "Error retrieving unit links" });
            }
        }

        [HttpPost("links")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UnitLink>> CreateUnitLink([FromBody] CreateUnitLinkRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized();

                var unitLink = new UnitLink
                {
                    SourceUnitId = request.SourceUnitId,
                    TargetUnitId = request.TargetUnitId,
                    LinkType = request.LinkType,
                    ExpiresAt = request.ExpiresAt,
                    Reason = request.Reason,
                    CreatedByUserId = currentUserId.Value
                };

                var createdLink = await _unitService.CreateUnitLinkAsync(unitLink);
                return Ok(createdLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating unit link");
                return StatusCode(500, new { message = "Error creating unit link" });
            }
        }

        [HttpDelete("links/{linkId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RemoveUnitLink(int linkId)
        {
            try
            {
                var success = await _unitService.RemoveUnitLinkAsync(linkId);
                if (!success)
                    return NotFound(new { message = "Unit link not found" });

                return Ok(new { message = "Unit link removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing unit link {LinkId}", linkId);
                return StatusCode(500, new { message = "Error removing unit link" });
            }
        }

        [HttpGet("{id}/communicable-users")]
        public async Task<ActionResult<List<User>>> GetCommunicableUsers(int id, [FromQuery] CommunicationType communicationType = CommunicationType.All)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized();

                var users = await _unitService.GetCommunicableUsersAsync(currentUserId.Value, communicationType);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting communicable users for unit {UnitId}", id);
                return StatusCode(500, new { message = "Error retrieving communicable users" });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<Unit>>> SearchUnits([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest(new { message = "Search term is required" });

                var units = await _unitService.SearchUnitsAsync(searchTerm);
                return Ok(units);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching units with term: {SearchTerm}", searchTerm);
                return StatusCode(500, new { message = "Error searching units" });
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
    public class CreateUnitRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int? ParentUnitId { get; set; }
        public int? ManagerId { get; set; }
        public string Location { get; set; } = string.Empty;
        public string PhoneExtension { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateUnitRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int? ParentUnitId { get; set; }
        public int? ManagerId { get; set; }
        public string Location { get; set; } = string.Empty;
        public string PhoneExtension { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CreateUnitLinkRequest
    {
        public int SourceUnitId { get; set; }
        public int TargetUnitId { get; set; }
        public UnitLinkType LinkType { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
