using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Models;
using backend.Services;
using System.Security.Claims;
using backend.Data;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;

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

        var paymentUrl = _vnPayService.CreatePaymentUrl(model, HttpContext);
        return Ok(new { PaymentUrl = paymentUrl });
    }

    [HttpGet("payment-callback")]
    public async Task<IActionResult> PaymentCallback()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var response = await _vnPayService.PaymentExecuteAsync(Request.Query, userId);
        if (response.Success)
        {
            return Ok(new { Message = "Payment successful", Data = response });
        }
        return BadRequest(new { Message = response.Message });
    }

    [HttpPost("ipn")]
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
        var vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
        var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var vnp_Amount = Convert.ToDouble(vnpay.GetResponseData("vnp_Amount")) / 100;
        var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

        var collection = _mongoContext.GetCollection<PaymentTransaction>("PaymentTransactions");
        var transaction = await collection.Find(t => t.TransactionId == vnp_TxnRef).FirstOrDefaultAsync();

        if (transaction == null)
        {
            return BadRequest(new { RspCode = "99", Message = "Transaction not found" });
        }

        if (vnpay.ValidateSignature(vnp_SecureHash, _configuration["Vnpay:HashSecret"]))
        {
            if (vnp_ResponseCode == "00")
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
                return Ok(new { RspCode = vnp_ResponseCode, Message = "Payment Failed" });
            }
        }
        return BadRequest(new { RspCode = "97", Message = "Invalid signature" });
    }
}