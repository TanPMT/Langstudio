using System.Threading.Tasks;
using backend.Models;
using System.Collections.Generic;

namespace backend.Services;

public interface IWritingService
{
    Task<ResponseWritingModel> SubmitEssayAsync(string userId, CreateWritingModel model);
    Task<List<ResponseWritingModel>> GetEssayHistoryAsync(string userId, int page, int pageSize);
}