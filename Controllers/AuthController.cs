using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RedMango_API.Data;
using RedMango_API.Models;
using RedMango_API.Models.Dto;
using RedMango_API.Utility;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace RedMango_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(
	ApplicationDbContext db,
	IConfiguration config,
	UserManager<ApplicationUser> userManager,
	RoleManager<IdentityRole> roleManager) : ControllerBase
{
	private readonly ApplicationDbContext db = db;
	private readonly UserManager<ApplicationUser> userManager = userManager;
	private readonly RoleManager<IdentityRole> roleManager = roleManager;
	private ApiResponse response = new();
	private string secretKey = config["AppSettings:Secret"];

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
	{
		var userFromDb = await db.ApplicationUsers
			.FirstOrDefaultAsync(u => u.UserName.ToLower() == model.Username.ToLower());
		var validPassword = await userManager.CheckPasswordAsync(userFromDb, model.Password);
		if (!validPassword)
		{
			response.Result = new LoginResponseDto();
			response.IsSuccess = false;
			response.StatusCode = HttpStatusCode.BadRequest;
			response.ErrorMessages.Add("Invalid username or password");
			return BadRequest(response);
		}

		var roles = await userManager.GetRolesAsync(userFromDb);
		var tokenHandler = new JwtSecurityTokenHandler();
		var key = Encoding.ASCII.GetBytes(secretKey);
		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(new []
			{
				new Claim("fullName", userFromDb.Name),
				new Claim("id", userFromDb.Id),
				new Claim(ClaimTypes.Email, userFromDb.UserName),
				new Claim(ClaimTypes.Role, roles.FirstOrDefault())
			}),
			Expires = DateTime.UtcNow.AddDays(1),
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
		};

		var token = tokenHandler.CreateToken(tokenDescriptor);

		var loginResponse = new LoginResponseDto
		{
			Email = userFromDb.Email,
			Token = tokenHandler.WriteToken(token)
		};

		if (loginResponse.Email == null || string.IsNullOrEmpty(loginResponse.Token))
		{
			response.Result = new LoginResponseDto();
			response.IsSuccess = false;
			response.StatusCode = HttpStatusCode.BadRequest;
			response.ErrorMessages.Add("Invalid username or password");
			return BadRequest(response);
		}

		response.StatusCode = HttpStatusCode.OK;
		response.IsSuccess = true;
		response.Result = loginResponse;
		return Ok(response);
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterRequestDto model)
	{
		var userFromDb = await db.ApplicationUsers
			.FirstOrDefaultAsync(u => u.UserName.ToLower() == model.Username.ToLower());
		if (userFromDb != null)
		{
			response.IsSuccess = false;
			response.StatusCode = HttpStatusCode.BadRequest;
			response.ErrorMessages.Add("User already exists");
			return BadRequest(response);
		}

		var newUser = new ApplicationUser
		{
			Email = model.Username,
			UserName = model.Username,
			NormalizedEmail = model.Username.ToUpper(),
			Name = model.Name
		};

		try
		{
			var result = await userManager.CreateAsync(newUser, model.Password);
			if (result.Succeeded)
			{
				if (!await roleManager.RoleExistsAsync(SD.Role_Admin))
				{
					await roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
					await roleManager.CreateAsync(new IdentityRole(SD.Role_Customer));
				}

				if (model.Role.ToLower() == SD.Role_Admin.ToLower())
				{
					await userManager.AddToRoleAsync(newUser, SD.Role_Admin);
				}
				else
				{
					await userManager.AddToRoleAsync(newUser, SD.Role_Customer);
				}

				response.Result = new { UserId = newUser.Id };
				response.StatusCode = HttpStatusCode.OK;
				return Ok(response);
			}
		}
		catch (Exception e)
		{
			response.ErrorMessages.Add($"User creation failed for {model.Username}\n{e.Message}");
		}

		response.IsSuccess = false;
		response.StatusCode = HttpStatusCode.BadRequest;
		response.ErrorMessages.Add($"User creation failed for {model.Username}, please check the data provided");
		return BadRequest(response);
	}
}