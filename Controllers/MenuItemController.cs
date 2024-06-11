using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMango_API.Data;
using RedMango_API.Models;
using RedMango_API.Models.Dto;
using RedMango_API.Services;
using RedMango_API.Utility;
using System.Net;

namespace RedMango_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MenuItemController : ControllerBase
{
	private readonly ApplicationDbContext db;
	private readonly IBlobService blobService;
	private ApiResponse response;

	public MenuItemController(ApplicationDbContext db, IBlobService blobService)
	{
		this.db = db;
		this.blobService = blobService;
		response = new ApiResponse();
	}

	[HttpGet]
	public async Task<IActionResult> GetMenuItems()
	{
		response.Result = await db.MenuItems.ToListAsync();
		response.StatusCode = HttpStatusCode.OK;
		return Ok(response);
	}

	[HttpGet("{id:int}", Name = "GetMenuItem")]
	public async Task<IActionResult> GetMenuItem(int id)
	{
		if (id <= 0)
		{
			response.IsSuccess = false;
			response.StatusCode = HttpStatusCode.BadRequest;
			response.ErrorMessages.Add($"Invalid Id: {id}");
			return BadRequest(response);
		}

		var menuItem = await db.MenuItems.FirstOrDefaultAsync(x => x.Id == id);
		if (menuItem == null)
		{
			response.IsSuccess = false;
			response.StatusCode = HttpStatusCode.BadRequest;
			response.ErrorMessages.Add($"Menu Item '{id}' not found");
			return BadRequest(response);
		}

		response.Result = menuItem;
		response.StatusCode = HttpStatusCode.OK;
		return Ok(response);
	}

	[HttpPost]
	public async Task<ActionResult<ApiResponse>> CreateMenuItem([FromForm] MenuItemCreateDto menuItemCreateDto)
	{
		try
		{
			if (ModelState.IsValid)
			{
				if (menuItemCreateDto.File == null || menuItemCreateDto.File.Length == 0)
				{
					return BadRequest();
				}

				var fileName = $"{Guid.NewGuid()}{Path.GetExtension(menuItemCreateDto.File.FileName)}";
				var menuitemToCreate = new MenuItem
				{
					Name = menuItemCreateDto.Name,
					Description = menuItemCreateDto.Description,
					SpecialTag = menuItemCreateDto.SpecialTag,
					Category = menuItemCreateDto.Category,
					Price = menuItemCreateDto.Price,
					Image = await blobService.UploadBlob(fileName, SD.SD_Storage_Container, menuItemCreateDto.File)
				};

				db.MenuItems.Add(menuitemToCreate);
				await db.SaveChangesAsync();
				response.Result = menuitemToCreate;
				response.StatusCode = HttpStatusCode.Created;
				return CreatedAtRoute("GetMenuItem", new { id = menuitemToCreate.Id }, response);
			}
			else
			{
				response.IsSuccess = false;
				response.StatusCode = HttpStatusCode.BadRequest;
				response.ErrorMessages.Add("Invalid Model State");
			}
		}
		catch (Exception e)
		{
			response.IsSuccess = false;
			response.ErrorMessages.Add(e.ToString());
		}

		return response;
	}

	[HttpPut("{id:int}")]
	public async Task<ActionResult<ApiResponse>> UpdateMenuItem(int id, [FromForm] MenuItemUpdateDto menuItemUpdateDto)
	{
		try
		{
			if (ModelState.IsValid)
			{
				if (menuItemUpdateDto == null || id != menuItemUpdateDto.Id)
				{
					response.IsSuccess = false;
					response.StatusCode = HttpStatusCode.BadRequest;
					response.ErrorMessages.Add($"Menu Item '{id}' invalid");
					return BadRequest();
				}

				var menuItemFromDb = await db.MenuItems.FindAsync(id);
				if (menuItemFromDb == null)
				{
					response.IsSuccess = false;
					response.StatusCode = HttpStatusCode.NotFound;
					response.ErrorMessages.Add("Menu Item not found");
					return NotFound(response);
				}

				menuItemFromDb.Name = menuItemUpdateDto.Name;
				menuItemFromDb.Description = menuItemUpdateDto.Description;
				menuItemFromDb.SpecialTag = menuItemUpdateDto.SpecialTag;
				menuItemFromDb.Category = menuItemUpdateDto.Category;
				menuItemFromDb.Price = menuItemUpdateDto.Price;

				if (menuItemUpdateDto.File is {Length: > 0})
				{
					await blobService.DeleteBlob(menuItemFromDb.Image.Split('/').Last(), SD.SD_Storage_Container);
					var fileName = $"{Guid.NewGuid()}{Path.GetExtension(menuItemUpdateDto.File.FileName)}";
					menuItemFromDb.Image = await blobService.UploadBlob(fileName, SD.SD_Storage_Container, menuItemUpdateDto.File);
				}

				db.MenuItems.Update(menuItemFromDb);
				await db.SaveChangesAsync();
				response.StatusCode = HttpStatusCode.NoContent;
				return Ok(response);
			}
		}
		catch (Exception e)
		{
			response.IsSuccess = false;
			response.ErrorMessages.Add(e.ToString());
		}

		return response;
	}

	[HttpDelete("{id:int}")]
	public async Task<ActionResult<ApiResponse>> DeleteMenuItem(int id)
	{
		try
		{
				if (id == 0)
				{
					response.IsSuccess = false;
					response.StatusCode = HttpStatusCode.BadRequest;
					response.ErrorMessages.Add($"Menu Item '{id}' invalid");
					return BadRequest();
				}

				var menuItemFromDb = await db.MenuItems.FindAsync(id);
				if (menuItemFromDb == null)
				{
					response.IsSuccess = false;
					response.StatusCode = HttpStatusCode.NotFound;
					response.ErrorMessages.Add($"Menu Item '{id}' not found");
					return NotFound(response);
				}

				await blobService.DeleteBlob(menuItemFromDb.Image.Split('/').Last(), SD.SD_Storage_Container);
				db.MenuItems.Remove(menuItemFromDb);
				await db.SaveChangesAsync();
				response.StatusCode = HttpStatusCode.NoContent;
				return Ok(response);
		}
		catch (Exception e)
		{
			response.IsSuccess = false;
			response.ErrorMessages.Add(e.ToString());
		}

		return response;
	}
}