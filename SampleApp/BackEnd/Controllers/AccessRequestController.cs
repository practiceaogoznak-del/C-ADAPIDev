using BackEnd.Models;
using BackEnd.Services;
using BackEnd.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccessRequestController : ControllerBase
{
    private readonly IActiveDirectoryService _adService;
    private readonly IAccessRequestRepository _repository;
    private readonly ILogger<AccessRequestController> _logger;

    public AccessRequestController(
        IActiveDirectoryService adService, 
        IAccessRequestRepository repository,
        ILogger<AccessRequestController> logger)
    {
        _adService = adService;
        _repository = repository;
        _logger = logger;
    }

    [HttpGet("resources")]
    public async Task<ActionResult<List<ADResource>>> GetResources()
    {
        try
        {
            var resources = await _adService.GetAllResourcesAsync();
            return Ok(resources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AD resources");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("users/{username}")]
    public async Task<ActionResult<ADUser>> GetUser(string username)
    {
        try
        {
            var user = await _adService.GetUserAsync(username);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AD user");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("users/{username}/groups")]
    public async Task<ActionResult<List<string>>> GetUserGroups(string username)
    {
        try
        {
            var groups = await _adService.GetUserGroupsAsync(username);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user groups");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("requests")]
    public async Task<ActionResult<IEnumerable<AccessRequest>>> GetRequests()
    {
        try
        {
            var requests = await _repository.GetAllAsync();
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access requests");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("requests/{id}")]
    public async Task<ActionResult<AccessRequest>> GetRequest(int id)
    {
        try
        {
            var request = await _repository.GetByIdAsync(id);
            if (request == null)
                return NotFound();

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access request");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("requests")]
    public async Task<ActionResult<AccessRequest>> CreateRequest(AccessRequest request)
    {
        try
        {
            // Устанавливаем текущего пользователя как создателя заявки
            request.UserId = User.Identity?.Name ?? "unknown";
            request.Status = AccessRequestStatus.Pending;
            request.CreatedAt = DateTime.UtcNow;

            var createdRequest = await _repository.CreateAsync(request);
            return CreatedAtAction(nameof(GetRequest), new { id = createdRequest.Id }, createdRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating access request");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("requests/{id}")]
    public async Task<IActionResult> UpdateRequest(int id, AccessRequest request)
    {
        if (id != request.Id)
            return BadRequest();

        try
        {
            request.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(request);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating access request");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("requests/{id}")]
    public async Task<IActionResult> DeleteRequest(int id)
    {
        try
        {
            await _repository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting access request");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("requests/user/{userId}")]
    public async Task<ActionResult<IEnumerable<AccessRequest>>> GetUserRequests(string userId)
    {
        try
        {
            var requests = await _repository.GetByUserIdAsync(userId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user access requests");
            return StatusCode(500, "Internal server error");
        }
    }
}