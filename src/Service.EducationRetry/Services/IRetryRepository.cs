using System;
using System.Threading.Tasks;
using Service.Core.Client.Models;
using Service.EducationRetry.Models;

namespace Service.EducationRetry.Services
{
	public interface IRetryRepository
	{
		ValueTask<EducationRetryTaskDto[]> GetEducationRetryTasks(Guid? userId);
		ValueTask<EducationRetryCountDto> GetEducationRetryCount(Guid? userId);
		ValueTask<EducationRetryLastDateDto> GetEducationRetryLastDate(Guid? userId);

		ValueTask<T> Get<T>(Func<string> keyFunc, Guid? userId) where T : class;
		ValueTask<CommonGrpcResponse> Set<T>(Func<string> keyFunc, Guid? userId, T dto);
		ValueTask<CommonGrpcResponse> Delete(Func<string> keyFunc, Guid? userId);
	}
}