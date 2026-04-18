using SapaFreshWayForStaff.DTOs.Payment;

namespace SapaFreshWayForStaff.ViewModels.Payment
{
    public class PaymentConfirmViewModel
    {
        public int OrderId { get; set; }
        public PaymentSessionDto Session { get; set; } = new();
    }
}

