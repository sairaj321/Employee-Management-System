using EMS.Application.DTOs;
using EMS.Application.Exceptions;
using EMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController : BaseApiController
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [Authorize(Policy = "Employee.Read")]
    public async Task<IActionResult> GetPaged([FromQuery] EmployeeQueryDto query, CancellationToken ct)
    {
        var result = await _employeeService.GetPagedAsync(query, ct);
        return OkResponse(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Employee.Read.Own")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _employeeService.GetByIdAsync(id, ct);
        return OkResponse(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var result = await _employeeService.GetByUserIdAsync(ActingUserId, ct);
        if (result == null)
            throw new NotFoundException("Employee profile not found for the logged in user.");
        return OkResponse(result);
    }

    [HttpGet("team")]
    [Authorize(Policy = "Employee.Read.Team")]
    public async Task<IActionResult> GetTeamMembers(CancellationToken ct)
    {
        if (!CallerEmployeeId.HasValue)
            throw new ForbiddenException("Only managers with an employee profile can view team members.");

        var result = await _employeeService.GetTeamMembersAsync(CallerEmployeeId.Value, ct);
        return OkResponse(result);
    }

    [HttpPost]
    [Authorize(Policy = "Employee.Create")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto, CancellationToken ct)
    {
        var result = await _employeeService.CreateAsync(dto, ActingUserId, ct);
        return CreatedResponse($"/api/employees/{result.Id}", result, "Employee created successfully.");
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Employee.Update")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeDto dto, CancellationToken ct)
    {
        var result = await _employeeService.UpdateAsync(id, dto, ActingUserId, ct);
        return OkResponse(result, "Employee updated successfully.");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Employee.Delete")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _employeeService.DeactivateAsync(id, ActingUserId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/departments")]
public class DepartmentsController : BaseApiController
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [Authorize(Policy = "Department.Read")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _departmentService.GetAllAsync(ct);
        return OkResponse(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Department.Read")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _departmentService.GetByIdAsync(id, ct);
        return OkResponse(result);
    }

    [HttpPost]
    [Authorize(Policy = "Department.Create")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto, CancellationToken ct)
    {
        var result = await _departmentService.CreateAsync(dto, ActingUserId, ct);
        return CreatedResponse($"/api/departments/{result.Id}", result, "Department created successfully.");
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Department.Update")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentDto dto, CancellationToken ct)
    {
        var result = await _departmentService.UpdateAsync(id, dto, ActingUserId, ct);
        return OkResponse(result, "Department updated successfully.");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Department.Delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _departmentService.DeleteAsync(id, ActingUserId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/positions")]
public class PositionsController : BaseApiController
{
    private readonly IPositionService _positionService;

    public PositionsController(IPositionService positionService)
    {
        _positionService = positionService;
    }

    [HttpGet]
    [Authorize(Policy = "Position.Read")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _positionService.GetAllAsync(ct);
        return OkResponse(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Position.Read")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _positionService.GetByIdAsync(id, ct);
        return OkResponse(result);
    }

    [HttpPost]
    [Authorize(Policy = "Position.Create")]
    public async Task<IActionResult> Create([FromBody] CreatePositionDto dto, CancellationToken ct)
    {
        var result = await _positionService.CreateAsync(dto, ActingUserId, ct);
        return CreatedResponse($"/api/positions/{result.Id}", result, "Position created successfully.");
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Position.Update")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePositionDto dto, CancellationToken ct)
    {
        var result = await _positionService.UpdateAsync(id, dto, ActingUserId, ct);
        return OkResponse(result, "Position updated successfully.");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Position.Delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _positionService.DeleteAsync(id, ActingUserId, ct);
        return NoContent();
    }
}
