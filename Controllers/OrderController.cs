using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMango_API.Data;
using RedMango_API.Models;
using RedMango_API.Models.Dto;
using RedMango_API.Utility;
using System.Net;

namespace RedMango_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderController(ApplicationDbContext db) : ControllerBase
{
	private readonly ApiResponse response = new();

	[Authorize]
	[HttpGet]
	public async Task<ActionResult<ApiResponse>> GetOrders(string? userId)
	{
		try
		{
			var orderHeaders = db.OrderHeaders
				.Include(o => o.OrderDetails)
				.ThenInclude(m => m.MenuItem)
				.OrderByDescending(o => o.OrderHeaderId);
			response.Result = !string.IsNullOrEmpty(userId)
				? orderHeaders.Where(o => o.ApplicationUserId == userId)
				: orderHeaders;
			response.StatusCode = HttpStatusCode.OK;
			response.Success = true;
			return Ok(response);
		}
		catch (Exception e)
		{
			response.Success = false;
			response.StatusCode = HttpStatusCode.BadRequest;
			return BadRequest(response);
		}
	}

	[HttpGet("{id:int}")]
	public async Task<ActionResult<ApiResponse>> GetOrder(int id)
	{
		try
		{
			if (id == 0)
			{
				response.ErrorMessages.Add("Invalid Order Id");
				response.StatusCode = HttpStatusCode.BadRequest;
				response.Success = false;
				return BadRequest(response);
			}

			var orderHeader = await db.OrderHeaders
				.Include(o => o.OrderDetails)
				.ThenInclude(m => m.MenuItem)
				.FirstOrDefaultAsync(o => o.OrderHeaderId == id);
			if (orderHeader == null)
			{
				response.ErrorMessages.Add("Order not found");
				response.StatusCode = HttpStatusCode.NotFound;
				response.Success = false;
				return NotFound(response);
			}

			response.Result = orderHeader;
			response.StatusCode = HttpStatusCode.OK;
			response.Success = true;
			return Ok(response);
		}
		catch (Exception e)
		{
			response.Success = false;
			response.StatusCode = HttpStatusCode.BadRequest;
			return BadRequest(response);
		}
	}

	[HttpPost]
	public async Task<ActionResult<ApiResponse>> CreateOrder([FromBody] OrderHeaderCreateDto orderHeaderDto)
	{
		try
		{
			if (!ModelState.IsValid)
			{
				response.Success = false;
				response.StatusCode = HttpStatusCode.BadRequest;
				response.ErrorMessages.Add("Invalid Order Header Object");
				return BadRequest(response);
			}

			var order = new OrderHeader
			{
				ApplicationUserId = orderHeaderDto.ApplicationUserId,
				PickupEmail = orderHeaderDto.PickupEmail,
				PickupName = orderHeaderDto.PickupName,
				PickupPhoneNumber = orderHeaderDto.PickupPhoneNumber,
				OrderTotal = orderHeaderDto.OrderTotal,
				OrderDate = DateTime.Now,
				StripePaymentIntentID = orderHeaderDto.StripePaymentIntentID,
				TotalItems = orderHeaderDto.TotalItems,
				Status = string.IsNullOrEmpty(orderHeaderDto.Status) ? SD.Status_pending : orderHeaderDto.Status,
			};

			db.OrderHeaders.Add(order);
			await db.SaveChangesAsync();
			foreach (var detailsDto in orderHeaderDto.OrderDetailsDtos)
			{
				var details = new OrderDetails
				{
					OrderHeaderId = order.OrderHeaderId,
					MenuItemId = detailsDto.MenuItemId,
					Price = detailsDto.Price,
					ItemName = detailsDto.ItemName,
					Quantity = detailsDto.Quantity,
				};
				db.OrderDetails.Add(details);
			}

			await db.SaveChangesAsync();
			response.Result = order;
			order.OrderDetails = null;
			response.StatusCode = HttpStatusCode.Created;
			return Ok(response);
		}
		catch (Exception e)
		{
			response.Success = false;
			response.StatusCode = HttpStatusCode.BadRequest;
			response.ErrorMessages.Add(e.Message);
			return BadRequest(response);
		}
	}

	[HttpPut("{id:int}")]
	public async Task<ActionResult<ApiResponse>> UpdateOrderHeader(int id, [FromBody] OrderHeaderUpdateDto orderHeaderUpdateDto)
	{
		try
		{
			if (orderHeaderUpdateDto == null || id != orderHeaderUpdateDto.OrderHeaderId)
			{
				response.Success = false;
				response.StatusCode = HttpStatusCode.BadRequest;
				response.ErrorMessages.Add("Invalid Order Header Object");
				return BadRequest(response);
			}

			var orderFromDb = await db.OrderHeaders.FirstOrDefaultAsync(o => o.OrderHeaderId == id);
			if (orderFromDb == null)
			{
				response.Success = false;
				response.StatusCode = HttpStatusCode.NotFound;
				response.ErrorMessages.Add("Order not found");
				return NotFound(response);
			}

			if (!string.IsNullOrEmpty(orderHeaderUpdateDto.PickupName))
			{
				orderFromDb.PickupName = orderHeaderUpdateDto.PickupName;
			}

			if (!string.IsNullOrEmpty(orderHeaderUpdateDto.PickupEmail))
			{
				orderFromDb.PickupEmail = orderHeaderUpdateDto.PickupEmail;
			}

			if (!string.IsNullOrEmpty(orderHeaderUpdateDto.PickupPhoneNumber))
			{
				orderFromDb.PickupPhoneNumber = orderHeaderUpdateDto.PickupPhoneNumber;
			}

			if (!string.IsNullOrEmpty(orderHeaderUpdateDto.Status))
			{
				orderFromDb.Status = orderHeaderUpdateDto.Status;
			}

			if (!string.IsNullOrEmpty(orderHeaderUpdateDto.StripePaymentIntentID))
			{
				orderFromDb.StripePaymentIntentID = orderHeaderUpdateDto.StripePaymentIntentID;
			}

			await db.SaveChangesAsync();
			response.StatusCode = HttpStatusCode.NoContent;
			response.Success = true;
			return Ok(response);
		}
		catch (Exception e)
		{
			response.Success = false;
			response.StatusCode = HttpStatusCode.BadRequest;
			return BadRequest(response);
		}
	}
}