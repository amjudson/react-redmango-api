namespace RedMango_API.Models.Dto;
public class OrderHeaderCreateDto
{
	public string PickupName { get; set; }
	public string PickupPhoneNumber { get; set; }
	public string PickupEmail { get; set; }

	public string ApplicationUserId { get; set; }
	public double OrderTotal { get; set; }

	public string StripePaymentIntentID { get; set; }
	public string Status { get; set; }
	public int TotalItems { get; set; }

	public IEnumerable<OrderDetailsCreateDto> OrderDetailsDtos { get; set; }
}
