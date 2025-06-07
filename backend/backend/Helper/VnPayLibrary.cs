using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace backend.Services;

public class VnPayLibrary
{
    private SortedList<string, string> requestData = new SortedList<string, string>(new VnPayCompare());
    private SortedList<string, string> responseData = new SortedList<string, string>(new VnPayCompare());

    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            requestData.Add(key, value);
        }
    }

    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            responseData.Add(key, value);
        }
    }

    public string GetResponseData(string key)
    {
        return responseData.TryGetValue(key, out var value) ? value : string.Empty;
    }

    public string CreateRequestUrl(string baseUrl, string hashSecret)
    {
        var data = string.Join("&", requestData.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var checksum = HmacSHA512(hashSecret, data);
        return $"{baseUrl}?{data}&vnp_SecureHash={checksum}";
    }

    public bool ValidateSignature(string inputHash, string hashSecret)
    {
        var data = string.Join("&", responseData
            .Where(kvp => !kvp.Key.Equals("vnp_SecureHash", StringComparison.InvariantCultureIgnoreCase))
            .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var checkSum = HmacSHA512(hashSecret, data);
        return checkSum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
    }

    private string HmacSHA512(string key, string inputData)
    {
        var hash = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(inputData));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}

public class VnPayCompare : IComparer<string>
{
    public int Compare(string x, string y)
    {
        return string.CompareOrdinal(x, y);
    }
}

public static class Utils
{
    public static string GetIpAddress(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
        {
            ipAddress = "127.0.0.1";
        }
        return ipAddress;
    }
}