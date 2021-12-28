using System;
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
		private static readonly string KeyEducationRetryCount = Program.Settings.KeyEducationRetryCount;
		private static readonly string KeyEducationRetryTask = Program.Settings.KeyEducationRetryTask;

		private readonly IServerKeyValueService _serverKeyValueService;
		private readonly ILogger<EducationRetryService> _logger;

		public EducationRetryService(ILogger<EducationRetryService> logger, IServerKeyValueService serverKeyValueService)
		{
			_logger = logger;
			_serverKeyValueService = serverKeyValueService;
		}

		public async ValueTask<CommonGrpcResponse> IncreaseRetryCountAsync(IncreaseRetryCountGrpcRequest request)
		{
			var retryCountDto = await Get<EducationRetryCountDto>(KeyEducationRetryCount, request.UserId);

			retryCountDto.Count += request.Value;

			return await Set(KeyEducationRetryCount, request.UserId, retryCountDto);
		}

		public async ValueTask<CommonGrpcResponse> DecreaseRetryCountAsync(DecreaseRetryCountGrpcRequest request)
		{
			Guid? userId = request.UserId;

			var countDto = await Get<EducationRetryCountDto>(KeyEducationRetryCount, userId);
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

			var taskDto = await Get<EducationRetryTaskDto>(KeyEducationRetryTask, userId);

			if (TaskInRetry(request.Tutorial, request.Unit, request.Task, taskDto))
			{
				_logger.LogError("Error while decrease retry count. Tutorial: {tutorial}, unit: {unit}, task: {task} already in retry mode (UserId: {user}).", request.Tutorial, request.Unit, request.Task, request.UserId);

				//Rollback count
				countDto.Count = originalValue;
				Set(KeyEducationRetryCount, userId, countDto).AsTask().Wait();
				return CommonGrpcResponse.Fail;
			}

			taskDto.Dtos.Add(new EducationRetryTaskItemDto
			{
				Tutorial = request.Tutorial,
				Unit = request.Unit,
				Task = request.Task
			});

			return await Set(KeyEducationRetryTask, userId, taskDto);
		}

		private async ValueTask<T> Get<T>(string key, Guid? userId) where T : class, new()
		{
			ItemsGrpcResponse getResponse = await _serverKeyValueService.Get(new ItemsGetGrpcRequest
			{
				Keys = new[] {key},
				UserId = userId
			});

			string value = getResponse.Items?.FirstOrDefault(model => model.Key == key)?.Value;

			return value == null
				? new T()
				: JsonSerializer.Deserialize<T>(value) ?? new T();
		}

		private async ValueTask<CommonGrpcResponse> Set<T>(string key, Guid? userId, T dto) => await _serverKeyValueService.Put(new ItemsPutGrpcRequest
		{
			UserId = userId,
			Items = new[]
			{
				new KeyValueGrpcModel
				{
					Key = key,
					Value = JsonSerializer.Serialize(dto)
				}
			}
		});

		public async ValueTask<RetryCountGrpcResponse> GetRetryCountAsync(GetRetryCountGrpcRequest request)
		{
			var countDto = await Get<EducationRetryCountDto>(KeyEducationRetryCount, request.UserId);

			return new RetryCountGrpcResponse
			{
				Count = countDto.Count
			};
		}

		public async ValueTask<TaskRetryStateGrpcResponse> GetTaskRetryStateAsync(GetTaskRetryStateGrpcRequest request)
		{
			var taskDto = await Get<EducationRetryTaskDto>(KeyEducationRetryTask, request.UserId);

			return new TaskRetryStateGrpcResponse
			{
				InRetry = TaskInRetry(request.Tutorial, request.Unit, request.Task, taskDto)
			};
		}

		private static bool TaskInRetry(EducationTutorial tutorial, int unit, int task, EducationRetryTaskDto taskDto) => taskDto.Dtos
			.Where(dto => dto.Tutorial == tutorial)
			.Where(dto => dto.Unit == unit)
			.Any(dto => dto.Task == task);

		public async ValueTask<CommonGrpcResponse> ClearTaskRetryStateAsync(ClearTaskRetryStateGrpcRequest request)
		{
			Guid? userId = request.UserId;

			var taskDto = await Get<EducationRetryTaskDto>(KeyEducationRetryTask, userId);

			EducationRetryTaskItemDto item = taskDto.Dtos
				.Where(dto => dto.Tutorial == request.Tutorial)
				.Where(dto => dto.Unit == request.Unit)
				.FirstOrDefault(dto => dto.Task == request.Task);

			if (item == null)
				return CommonGrpcResponse.Success;

			taskDto.Dtos.Remove(item);

			return await Set(KeyEducationRetryTask, userId, taskDto);
		}
	}
}