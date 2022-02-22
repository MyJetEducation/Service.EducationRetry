using System;
using System.Text.Json;
using System.Threading.Tasks;
using Service.Core.Client.Models;
using Service.EducationRetry.Models;
using Service.Grpc;
using Service.ServerKeyValue.Grpc;
using Service.ServerKeyValue.Grpc.Models;

namespace Service.EducationRetry.Services
{
	public class RetryRepository : IRetryRepository
	{
		private static Func<string> KeyEducationRetryCount => Program.ReloadedSettings(model => model.KeyEducationRetryCount);
		private static Func<string> KeyEducationRetryLastDate => Program.ReloadedSettings(model => model.KeyEducationRetryLastDate);
		private static Func<string> KeyEducationRetryTask => Program.ReloadedSettings(model => model.KeyEducationRetryTask);

		private readonly IGrpcServiceProxy<IServerKeyValueService> _serverKeyValueService;

		public RetryRepository(IGrpcServiceProxy<IServerKeyValueService> serverKeyValueService) => _serverKeyValueService = serverKeyValueService;

		public async ValueTask<EducationRetryTaskDto[]> GetEducationRetryTasks(Guid? userId) =>
			await Get<EducationRetryTaskDto[]>(KeyEducationRetryTask, userId) ?? Array.Empty<EducationRetryTaskDto>();

		public async ValueTask<EducationRetryCountDto> GetEducationRetryCount(Guid? userId) =>
			await Get<EducationRetryCountDto>(KeyEducationRetryCount, userId) ?? new EducationRetryCountDto();

		public async ValueTask<EducationRetryLastDateDto> GetEducationRetryLastDate(Guid? userId) =>
			await Get<EducationRetryLastDateDto>(KeyEducationRetryLastDate, userId) ?? new EducationRetryLastDateDto();

		public async ValueTask<T> Get<T>(Func<string> keyFunc, Guid? userId) where T : class
		{
			string value = (await _serverKeyValueService.Service.GetSingle(new ItemsGetSingleGrpcRequest
			{
				UserId = userId,
				Key = keyFunc.Invoke()
			}))?.Value;

			return value == null
				? null
				: JsonSerializer.Deserialize<T>(value);
		}

		public async ValueTask<CommonGrpcResponse> Set<T>(Func<string> keyFunc, Guid? userId, T dto) => await _serverKeyValueService.TryCall(service => service.Put(new ItemsPutGrpcRequest
		{
			UserId = userId,
			Items = new[]
			{
				new KeyValueGrpcModel
				{
					Key = keyFunc.Invoke(),
					Value = JsonSerializer.Serialize(dto)
				}
			}
		}));

		public async ValueTask<CommonGrpcResponse> Delete(Func<string> keyFunc, Guid? userId) => await _serverKeyValueService.TryCall(service => service.Delete(new ItemsDeleteGrpcRequest
		{
			UserId = userId,
			Keys = new[]
			{
				keyFunc.Invoke()
			}
		}));
	}
}