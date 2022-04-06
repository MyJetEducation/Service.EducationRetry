using System;
using System.Threading.Tasks;
using Service.Core.Client.Models;
using Service.EducationRetry.Domain.Models;
using Service.EducationRetry.Models;

namespace Service.EducationRetry.Services
{
	public interface IRetryRepository
	{
		ValueTask<EducationRetryTaskDto[]> GetEducationRetryTasks(string userId);
		ValueTask<EducationRetryCountDto> GetEducationRetryCount(string userId);
		ValueTask<EducationRetryLastDateDto> GetEducationRetryLastDate(string userId);

		ValueTask<T> Get<T>(Func<string> keyFunc, string userId) where T : class;
		ValueTask<CommonGrpcResponse> Set<T>(Func<string> keyFunc, string userId, T dto);
		ValueTask<CommonGrpcResponse> Delete(Func<string> keyFunc, string userId);
	}
}