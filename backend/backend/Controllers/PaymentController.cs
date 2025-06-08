using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Models;
using backend.Services;
using backend.Data;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace backend.Controllers;

[ApiController]
[Route("/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IVnPayService _vnPayService;
    private readonly MongoDbContext _mongoContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IVnPayService vnPayService,
        MongoDbContext mongoContext,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<PaymentController> logger)
    {
        _vnPayService = vnPayService;
        _mongoContext = mongoContext;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("create-payment")]
    public IActionResult CreatePayment([FromBody] PaymentRequestModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("CreatePayment: User not authenticated");
            return BadRequest(new { Message = "User not authenticated" });
        }

        // Thêm userId vào OrderDescription
        model.OrderDescription = $"Pro Subscription for LangStudio|UserId:{userId}";
        _logger.LogInformation("CreatePayment: OrderDescription set to {OrderDescription}", model.OrderDescription);

        var paymentUrl = _vnPayService.CreatePaymentUrl(model, HttpContext);
        return Ok(new { PaymentUrl = paymentUrl });
    }

    [HttpGet("payment-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback()
    {
        var queryString = string.Join("&", Request.Query.Select(kv => $"{kv.Key}={kv.Value}"));
        _logger.LogInformation("PaymentCallback: Received query: {QueryString}", queryString);

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

        // Lấy userId từ OrderInfo hoặc MongoDB
        string userId = null;
        if (!string.IsNullOrEmpty(vnp_OrderInfo) && vnp_OrderInfo.Contains("|UserId:"))
        {
            userId = vnp_OrderInfo.Split("|UserId:")[1];
            _logger.LogInformation("PaymentCallback: Extracted userId from OrderInfo: {UserId}", userId);
        }
        else
        {
            var collection = _mongoContext.GetCollection<PaymentTransaction>("PaymentTransactions");
            var transaction = await collection.Find(t => t.TransactionId == vnp_TxnRef).FirstOrDefaultAsync();
            userId = transaction?.UserId;
            _logger.LogInformation("PaymentCallback: Retrieved userId from MongoDB: {UserId}", userId);
        }

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("PaymentCallback: Cannot determine user ID for TxnRef: {TxnRef}", vnp_TxnRef);
            return BadRequest(new { Message = "Cannot determine user ID" });
        }

        var response = await _vnPayService.PaymentExecuteAsync(Request.Query, userId);
        if (response.Success)
        {
            _logger.LogInformation("PaymentCallback: Payment successful for userId: {UserId}, TxnRef: {TxnRef}", userId, vnp_TxnRef);
            return Ok(new { Message = "Payment successful", Data = response });
        }

        _logger.LogWarning("PaymentCallback: Payment failed: {Message}", response.Message);
        return BadRequest(new { Message = response.Message });
    }

    [HttpPost("ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentIpn()
    {
        var queryString = string.Join("&", Request.Query.Select(kv => $"{kv.Key}={kv.Value}"));
        _logger.LogInformation("PaymentIpn: Received query: {QueryString}", queryString);

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
            _logger.LogError("PaymentIpn: Missing vnp_SecureHash");
            return BadRequest(new { RspCode = "97", Message = "Missing vnp_SecureHash" });
        }

        var vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
        var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
        var vnp_Amount = vnpay.GetResponseData("vnp_Amount");
        var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

        if (!double.TryParse(vnp_Amount, out double amount))
        {
            _logger.LogError("PaymentIpn: Invalid amount: {Amount}", vnp_Amount);
            return BadRequest(new { RspCode = "99", Message = "Invalid amount" });
        }

        var collection = _mongoContext.GetCollection<PaymentTransaction>("PaymentTransactions");
        var transaction = await collection.Find(t => t.TransactionId == vnp_TxnRef).FirstOrDefaultAsync();

        if (transaction == null)
        {
            _logger.LogError("PaymentIpn: Transaction not found for TxnRef: {TxnRef}", vnp_TxnRef);
            return BadRequest(new { RspCode = "99", Message = "Transaction not found" });
        }

        if (vnpay.ValidateSignature(vnp_SecureHash, _configuration["Vnpay:HashSecret"]))
        {
            if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
            {
                var user = await _userManager.FindByIdAsync(transaction.UserId);
                if (user != null && !user.IsPro) // Chỉ cập nhật nếu IsPro chưa được đặt
                {
                    user.IsPro = true;
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        _logger.LogError("PaymentIpn: Failed to update IsPro for userId: {UserId}, Errors: {Errors}", transaction.UserId, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    }
                    else
                    {
                        _logger.LogInformation("PaymentIpn: Updated IsPro to true for userId: {UserId}", transaction.UserId);
                    }
                }
                transaction.Status = "Success";
                await collection.ReplaceOneAsync(t => t.Id == transaction.Id, transaction);
                _logger.LogInformation("PaymentIpn: Transaction updated to Success for TxnRef: {TxnRef}", vnp_TxnRef);
                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }
            else
            {
                transaction.Status = "Failed";
                await collection.ReplaceOneAsync(t => t.Id == transaction.Id, transaction);
                _logger.LogWarning("PaymentIpn: Payment failed for TxnRef: {TxnRef}, ResponseCode: {ResponseCode}, TransactionStatus: {TransactionStatus}", vnp_TxnRef, vnp_ResponseCode, vnp_TransactionStatus);
                return Ok(new { RspCode = vnp_ResponseCode, Message = $"Payment Failed: ResponseCode={vnp_ResponseCode}, TransactionStatus={vnp_TransactionStatus}" });
            }
        }
        _logger.LogError("PaymentIpn: Invalid signature for TxnRef: {TxnRef}", vnp_TxnRef);
        return BadRequest(new { RspCode = "97", Message = "Invalid signature" });
    }
}