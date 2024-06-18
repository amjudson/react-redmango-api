using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMango_API.Data;
using RedMango_API.Models;
using Stripe;
using System.Net;

namespace RedMango_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController(IConfiguration config, ApplicationDbContext db) : ControllerBase
{
	private readonly ApiResponse response = new();

	[HttpPost]
	public async Task<ActionResult<ApiResponse>> MakePayment(string userId)
	{
		var shoppingCart = await db.ShoppingCarts
			.Include(c => c.CartItems)
			.ThenInclude(m => m.MenuItem)
			.FirstOrDefaultAsync(x => x.UserId == userId);

		if (shoppingCart == null || shoppingCart.CartItems == null || shoppingCart.CartItems.Count == 0)
		{
			response.ErrorMessages.Add("Empty Cart");
			response.Success = false;
			response.StatusCode = HttpStatusCode.BadRequest;
			return BadRequest(response);
		}

		// Create Payment intent
		StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];
		shoppingCart.CartTotal = shoppingCart.CartItems.Sum(x => x.Quantity * x.MenuItem.Price);

		var options = new PaymentIntentCreateOptions
		{
			Amount = (int)shoppingCart.CartTotal * 100,
			Currency = "usd",
			PaymentMethodTypes =
			[
				"card"
			],
		};
		var service = new PaymentIntentService();
		var stripeResponse = service.Create(options);
		shoppingCart.StripePaymentIntentId = stripeResponse.Id;
		shoppingCart.ClientSecret = stripeResponse.ClientSecret;

		// End Payment intent

		response.Success = true;
		response.StatusCode = HttpStatusCode.OK;
		response.Result = shoppingCart;
		return Ok(response);
	}
}