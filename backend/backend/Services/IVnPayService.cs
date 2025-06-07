using backend.Models;

namespace backend.Services;

public interface IVnPayService
{
    string CreatePaymentUrl(PaymentRequestModel model, HttpContext context);
    Task<PaymentResponseModel> PaymentExecuteAsync(IQueryCollection collections, string userId);
}