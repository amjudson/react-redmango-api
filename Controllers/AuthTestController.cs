using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RedMango_API.Utility;

namespace RedMango_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthTestController : ControllerBase
{
	[HttpGet]
	[Authorize]
	public async Task<ActionResult<string>> GetSomething()
  {
      return Ok("You are authenticated!");
  }

	[HttpGet("{value:int}")]
	[Authorize(Roles = SD.Role_Admin)]
	public async Task<ActionResult<string>> GetSomething(int value)
	{
		return Ok("You are authorized with the Admin role!");
	}
}