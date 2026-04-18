using System.ComponentModel.DataAnnotations;

namespace SapaFreshWayForStaff.DTOs.Payment
{
    /// <summary>
    /// Request DTO cho việc hủy đơn hàng
    /// </summary>
    public class CancelOrderRequest
    {
        [Required(ErrorMessage = "OrderId là bắt buộc")]
        public int OrderId { get; set; }

        public string? Reason { get; set; }
    }
}

