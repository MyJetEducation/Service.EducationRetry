using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Service.Core.Domain.Models.Education;
using Service.Core.Grpc.Models;
using Service.EducationRetry.Domain.Models;
using Service.EducationRetry.Grpc;
using Service.EducationRetry.Grpc.Models;
using Service.ServerKeyValue.Grpc;
using Service.ServerKeyValue.Grpc.Models;

namespace Service.EducationRetry.Services
{
	public class EducationRetryService : IEducationRetryService
	{
		private static Func<string> KeyEducationRetryCount => Program.ReloadedSettings(model => model.KeyEducationRetryCount);
		private static Func<string> KeyEducationRetryTask => Program.ReloadedSettings(model => model.KeyEducationRetryTask);

		private readonly IServerKeyValueService _serverKeyValueService;
		private readonly ILogger<EducationRetryService> _logger;

		public EducationRetryService(ILogger<EducationRetryService> logger, IServerKeyValueService serverKeyValueService)
		{
			_logger = logger;
			_serverKeyValueService = serverKeyValueService;
		}

		public async ValueTask<CommonGrpcResponse> IncreaseRetryCountAsync(IncreaseRetryCountGrpcRequest request)
		{
			EducationRetryCountDto retryCountDto = await GetEducationRetryCount(request.UserId);

			retryCountDto.Count += request.Value;

			return await Set(KeyEducationRetryCount, request.UserId, retryCountDto);
		}

		public async ValueTask<CommonGrpcResponse> DecreaseRetryCountAsync(DecreaseRetryCountGrpcRequest request)
		{
			Guid? userId = request.UserId;

			EducationRetryCountDto countDto = await GetEducationRetryCount(userId);
			int originalValue = countDto.Count;
			countDto.Count -= request.Value;
			if (countDto.Count <= 0)
			{
				_logger.LogError("Error while decrease retry count. User ({user}) has no free retry count to decrease.", request.UserId);

				return CommonGrpcResponse.Fail;
			}

			CommonGrpcResponse setCountResponse = await Set(KeyEducationRetryCount, userId, countDto);
			if (!setCountResponse.IsSuccess)
				return setCountResponse;

			List<EducationRetryTaskDto> taskDto = (await GetEducationRetryTasks(userId)).ToList();

			if (TaskInRetry(request.Tutorial, request.Unit, request.Task, taskDto))
			{
				_logger.LogError("Error while decrease retry count. Tutorial: {tutorial}, unit: {unit}, task: {task} already in retry mode (UserId: {user}).", request.Tutorial, request.Unit, request.Task, request.UserId);

				//Rollback count
				countDto.Count = originalValue;
				Set(KeyEducationRetryCount, userId, countDto).AsTask().Wait();
				return CommonGrpcResponse.Fail;
			}

			taskDto.Add(new EducationRetryTaskDto
			{
				Tutorial = request.Tutorial,
				Unit = request.Unit,
				Task = request.Task
			});

			return await Set(KeyEducationRetryTask, userId, taskDto.ToArray());
		}

		private async ValueTask<EducationRetryTaskDto[]> GetEducationRetryTasks(Guid? userId)
		{
			return await Get<EducationRetryTaskDto[]>(KeyEducationRetryTask, userId) ?? Array.Empty<EducationRetryTaskDto>();
		}

		private async ValueTask<EducationRetryCountDto> GetEducationRetryCount(Guid? userId)
		{
			return await Get<EducationRetryCountDto>(KeyEducationRetryCount, userId) ?? new EducationRetryCountDto();
		}

		private async ValueTask<T> Get<T>(Func<string> keyFunc, Guid? userId) where T : class
		{
			string value = (await _serverKeyValueService.GetSingle(new ItemsGetSingleGrpcRequest
			{
				UserId = userId,
				Key = keyFunc.Invoke()
			}))?.Value;

			return value == null
				? null
				: JsonSerializer.Deserialize<T>(value);
		}

		private async ValueTask<CommonGrpcResponse> Set<T>(Func<string> keyFunc, Guid? userId, T dto) => await _serverKeyValueService.Put(new ItemsPutGrpcRequest
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
		});

		public async ValueTask<RetryCountGrpcResponse> GetRetryCountAsync(GetRetryCountGrpcRequest request)
		{
			EducationRetryCountDto countDto = await GetEducationRetryCount(request.UserId);

			return new RetryCountGrpcResponse
			{
				Count = countDto.Count
			};
		}

		public async ValueTask<TaskRetryStateGrpcResponse> GetTaskRetryStateAsync(GetTaskRetryStateGrpcRequest request)
		{
			EducationRetryTaskDto[] taskDto = await GetEducationRetryTasks(request.UserId);

			return new TaskRetryStateGrpcResponse
			{
				InRetry = TaskInRetry(request.Tutorial, request.Unit, request.Task, taskDto)
			};
		}

		private static bool TaskInRetry(EducationTutorial tutorial, int unit, int task, IEnumerable<EducationRetryTaskDto> taskDto) => taskDto
			.Where(dto => dto.Tutorial == tutorial)
			.Where(dto => dto.Unit == unit)
			.Any(dto => dto.Task == task);

		public async ValueTask<CommonGrpcResponse> ClearTaskRetryStateAsync(ClearTaskRetryStateGrpcRequest request)
		{
			Guid? userId = request.UserId;

			List<EducationRetryTaskDto> taskDto = (await GetEducationRetryTasks(userId)).ToList();

			EducationRetryTaskDto item = taskDto
				.Where(dto => dto.Tutorial == request.Tutorial)
				.Where(dto => dto.Unit == request.Unit)
				.FirstOrDefault(dto => dto.Task == request.Task);

			if (item == null)
				return CommonGrpcResponse.Success;

			taskDto.Remove(item);

			return await Set(KeyEducationRetryTask, userId, taskDto.ToArray());
		}
	}
}