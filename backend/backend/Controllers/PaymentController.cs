using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Models;
using backend.Services;
using backend.Data;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;

namespace backend.Controllers;

[ApiController]
[Route("/[controller]")]
[Authorize] // Giữ Authorize cho các endpoint khác
public class PaymentController : ControllerBase
{
    private readonly IVnPayService _vnPayService;
    private readonly MongoDbContext _mongoContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public PaymentController(
        IVnPayService vnPayService,
        MongoDbContext mongoContext,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _vnPayService = vnPayService;
        _mongoContext = mongoContext;
        _userManager = userManager;
        _configuration = configuration;
    }

    [HttpPost("create-payment")]
    public IActionResult CreatePayment([FromBody] PaymentRequestModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { Message = "User not authenticated" });
        }

        // Lưu userId vào OrderDescription để sử dụng trong callback
        model.OrderDescription = $"Pro Subscription for LangStudio|UserId:{userId}";

        var paymentUrl = _vnPayService.CreatePaymentUrl(model, HttpContext);
        return Ok(new { PaymentUrl = paymentUrl });
    }

    [HttpGet("payment-callback")]
    [AllowAnonymous] // Cho phép truy cập mà không cần xác thực
    public async Task<IActionResult> PaymentCallback()
    {
        var vnpay = new VnPayLibrary();
        foreach (var (key, value) in Request.Query)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(key, value);
            }
        }

        var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");
        var vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");

        // Lấy userId từ OrderInfo hoặc database
        string userId = null;
        if (!string.IsNullOrEmpty(vnp_OrderInfo) && vnp_OrderInfo.Contains("|UserId:"))
        {
            userId = vnp_OrderInfo.Split("|UserId:")[1];
        }
        else
        {
            var collection = _mongoContext.GetCollection<PaymentTransaction>("PaymentTransactions");
            var transaction = await collection.Find(t => t.TransactionId == vnp_TxnRef).FirstOrDefaultAsync();
            userId = transaction?.UserId;
        }

        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { Message = "Cannot determine user ID" });
        }

        var response = await _vnPayService.PaymentExecuteAsync(Request.Query, userId);
        if (response.Success)
        {
            return Ok(new { Message = "Payment successful", Data = response });
        }
        return BadRequest(new { Message = response.Message });
    }

    [HttpPost("ipn")]
    [AllowAnonymous] // Đảm bảo IPN không yêu cầu xác thực
    public async Task<IActionResult> PaymentIpn()
    {
        var vnpay = new VnPayLibrary();
        foreach (var (key, value) in Request.Query)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(key, value);
            }
        }

        var vnp_SecureHash = Request.Query["vnp_SecureHash"];
        if (string.IsNullOrEmpty(vnp_SecureHash))
        {
            return BadRequest(new { RspCode = "97", Message = "Missing vnp_SecureHash" });
        }

        var vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
        var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
        var vnp_Amount = vnpay.GetResponseData("vnp_Amount");
        var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

        if (!double.TryParse(vnp_Amount, out double amount))
        {
            return BadRequest(new { RspCode = "99", Message = "Invalid amount" });
        }

        var collection = _mongoContext.GetCollection<PaymentTransaction>("PaymentTransactions");
        var transaction = await collection.Find(t => t.TransactionId == vnp_TxnRef).FirstOrDefaultAsync();

        if (transaction == null)
        {
            return BadRequest(new { RspCode = "99", Message = "Transaction not found" });
        }

        if (vnpay.ValidateSignature(vnp_SecureHash, _configuration["Vnpay:HashSecret"]))
        {
            if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
            {
                var user = await _userManager.FindByIdAsync(transaction.UserId);
                if (user != null)
                {
                    user.IsPro = true;
                    await _userManager.UpdateAsync(user);
                }
                transaction.Status = "Success";
                await collection.ReplaceOneAsync(t => t.Id == transaction.Id, transaction);
                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }
            else
            {
                transaction.Status = "Failed";
                await collection.ReplaceOneAsync(t => t.Id == transaction.Id, transaction);
                return Ok(new { RspCode = vnp_ResponseCode, Message = $"Payment Failed: ResponseCode={vnp_ResponseCode}, TransactionStatus={vnp_TransactionStatus}" });
            }
        }
        return BadRequest(new { RspCode = "97", Message = "Invalid signature" });
    }
}