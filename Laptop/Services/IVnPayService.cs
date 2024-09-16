using LaptopShop.ModelViews;

namespace LaptopShop.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model);
        VnPaymentResponseVModel PaymentExecute(IQueryCollection collections);
    }
}
