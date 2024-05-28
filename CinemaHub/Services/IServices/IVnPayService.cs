using CinemaHub.ViewModels;

namespace CinemaHub.Services.IServices
{
    public interface IVnPayService
    {
        public string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model);
        VnPaymentResponseModel PaymentExecute(IQueryCollection collection);
    }
}
