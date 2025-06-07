using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using backend.Data;
using backend.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.Identity;

namespace backend.Services;

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _configuration;
    private readonly MongoDbContext _mongoContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public VnPayService(IConfiguration configuration, MongoDbContext mongoContext, UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _mongoContext = mongoContext;
        _userManager = userManager;
    }

    public string CreatePaymentUrl(PaymentRequestModel model, HttpContext context)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var tick = timeNow.Ticks.ToString();
        var vnpay = new VnPayLibrary();

        vnpay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]);
        vnpay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]);
        vnpay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]);
        vnpay.AddRequestData("vnp_Amount", (model.Amount * 100).ToString());
        vnpay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]);
        vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
        vnpay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]);
        vnpay.AddRequestData("vnp_OrderInfo", model.OrderDescription);
        vnpay.AddRequestData("vnp_OrderType", model.OrderType);
        vnpay.AddRequestData("vnp_ReturnUrl", _configuration["Vnpay:ReturnUrl"]);
        vnpay.AddRequestData("vnp_TxnRef", tick);

        return vnpay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"], _configuration["Vnpay:HashSecret"]);
    }

    public async Task<PaymentResponseModel> PaymentExecuteAsync(IQueryCollection collections, string userId)
    {
        var vnpay = new VnPayLibrary();
        foreach (var (key, value) in collections)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(key, value);
            }
        }

        var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var vnp_TransactionNo = vnpay.GetResponseData("vnp_TransactionNo");
        var vnp_SecureHash = collections["vnp_SecureHash"];
        var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");
        var vnp_Amount = vnpay.GetResponseData("vnp_Amount");

        bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _configuration["Vnpay:HashSecret"]);
        if (!checkSignature)
        {
            return new PaymentResponseModel
            {
                Success = false,
                Message = "Invalid signature",
                TransactionId = vnp_TransactionNo,
                Amount = vnp_Amount,
                OrderDescription = vnp_OrderInfo
            };
        }

        var transaction = new PaymentTransaction
        {
            UserId = userId,
            TransactionId = vnp_TransactionNo,
            Amount = Convert.ToDouble(vnp_Amount) / 100,
            OrderDescription = vnp_OrderInfo,
            Status = vnp_ResponseCode == "00" ? "Success" : "Failed"
        };

        var collection = _mongoContext.GetCollection<PaymentTransaction>("PaymentTransactions");
        await collection.InsertOneAsync(transaction);

        if (vnp_ResponseCode == "00")
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsPro = true;
                await _userManager.UpdateAsync(user);
            }
        }

        return new PaymentResponseModel
        {
            Success = vnp_ResponseCode == "00",
            Message = vnp_ResponseCode == "00" ? "Payment successful" : "Payment failed",
            TransactionId = vnp_TransactionNo,
            Amount = vnp_Amount,
            OrderDescription = vnp_OrderInfo
        };
    }
}