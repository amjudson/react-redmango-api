using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMango_API.Data;
using RedMango_API.Models;
using System.Net;

namespace RedMango_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ShoppingCartController(ApplicationDbContext db) : ControllerBase
{
	private readonly ApiResponse response = new ApiResponse();

	[HttpGet("{userId}")]
	public async Task<ActionResult<ApiResponse>> GetShoppingCart(string userId)
	{
		try
		{
			//
			// if (string.IsNullOrEmpty(userId))
			// {
			// 	response.StatusCode = HttpStatusCode.BadRequest;
			// 	response.Success = false;
			// 	response.ErrorMessages.Add("Invalid User Id");
			// 	return BadRequest(response);
			// }

			var shoppingCart = !string.IsNullOrEmpty(userId)
				? await db.ShoppingCarts
				.Include(s => s.CartItems)
				.ThenInclude(c => c.MenuItem)
				.FirstOrDefaultAsync(u => u.UserId == userId)
				: new ShoppingCart();

			if (shoppingCart != null && shoppingCart.CartItems.Count > 0)
			{
				shoppingCart.CartTotal = shoppingCart.CartItems.Sum(c => c.Quantity * c.MenuItem!.Price);
			}

			response.Result = shoppingCart;
			response.StatusCode = HttpStatusCode.OK;
			response.Success = true;
			return Ok(response);
		}
		catch (Exception e)
		{
			response.StatusCode = HttpStatusCode.BadRequest;
			response.Success = false;
			response.ErrorMessages.Add(e.Message);
			return BadRequest(response);
		}
	}

	[HttpPost]
	public async Task<ActionResult<ApiResponse>> AddOrUpdateItemInCart(string userId, int menuItemId, int updateQuantityBy)
	{
		var shoppingCart = await db.ShoppingCarts
			.Include(s => s.CartItems)
			.FirstOrDefaultAsync(u => u.UserId == userId);
		var menuItem = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId);
		if (menuItem == null)
		{
			response.ErrorMessages.Add("Invalid Menu Item Request");
			response.Success = false;
			return BadRequest(response);
		}

		if (shoppingCart == null && updateQuantityBy > 0)
		{
			var newCart = new ShoppingCart
			{
				UserId = userId,
			};

			db.ShoppingCarts.Add(newCart);
			await db.SaveChangesAsync();
			var newCartItem = new CartItem
			{
				MenuItemId = menuItemId,
				Quantity = updateQuantityBy,
				ShoppingCartId = newCart.Id,
				MenuItem = null,
			};

			db.CartItems.Add(newCartItem);
			await db.SaveChangesAsync();
		}
		else
		{
			var cartItemInCart = shoppingCart!.CartItems.FirstOrDefault(u => u.MenuItemId == menuItemId);
			if (cartItemInCart == null)
			{
				var newCartItem = new CartItem
				{
					MenuItemId = menuItemId,
					Quantity = updateQuantityBy,
					ShoppingCartId = shoppingCart.Id,
					MenuItem = null,
				};

				db.CartItems.Add(newCartItem);
				await db.SaveChangesAsync();
			}
			else
			{
				var newQuantity = cartItemInCart.Quantity + updateQuantityBy;
				if (updateQuantityBy == 0 || newQuantity <= 0)
				{
					db.CartItems.Remove(cartItemInCart);
					if (shoppingCart.CartItems.Count == 1)
					{
						db.ShoppingCarts.Remove(shoppingCart);
					}

					await db.SaveChangesAsync();
				}
				else
				{
					cartItemInCart.Quantity = newQuantity;
					await db.SaveChangesAsync();
				}
			}
		}

		response.Success = true;
		response.StatusCode = HttpStatusCode.OK;
		response.Result = new KeyValuePair<string, string>("Message", "Cart added or updated successfully");
		return Ok(response);
	}
}