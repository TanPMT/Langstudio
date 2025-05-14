using System.Threading.Tasks;
using backend.Models;
using System.Collections.Generic;

namespace backend.Services;


public interface IListeningService
{
    Task<ResponseListeningModel> SubmitDictationAsync(CreateListeningModel model);
    Task<ResponseListeningModel> GetDictationAsync(CreateListeningModel model);
    Task<List<ResponseListeningModel>> GetTopicAsync(string Topic, int page, int pageSize);
}

