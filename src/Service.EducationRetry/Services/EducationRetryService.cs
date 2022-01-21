using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.Core.Domain.Models;
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
		private static Func<string> KeyEducationRetryLastDate => Program.ReloadedSettings(model => model.KeyEducationRetryLastDate);
		private static Func<string> KeyEducationRetryTask => Program.ReloadedSettings(model => model.KeyEducationRetryTask);
		private static Func<string> KeyEducationRetryUsedCount => Program.ReloadedSettings(model => model.KeyEducationRetryUsedCount);

		private readonly IServerKeyValueService _serverKeyValueService;
		private readonly ILogger<EducationRetryService> _logger;
		private readonly ISystemClock _systemClock;
		private readonly IPublisher<UpdateRetryUsedCountInfoServiceBusModel> _publisher;

		public EducationRetryService(ILogger<EducationRetryService> logger, 
			IServerKeyValueService serverKeyValueService, 
			ISystemClock systemClock, 
			IPublisher<UpdateRetryUsedCountInfoServiceBusModel> publisher)
		{
			_logger = logger;
			_serverKeyValueService = serverKeyValueService;
			_systemClock = systemClock;
			_publisher = publisher;
		}

		public async ValueTask<CommonGrpcResponse> DecreaseRetryCountAsync(DecreaseRetryCountGrpcRequest request) => await DecreaseRetryAsync(request, async userId =>
		{
			EducationRetryCountDto countDto = await GetEducationRetryCount(userId);

			countDto.Count--;

			if (countDto.Count >= 0)
			{
				CommonGrpcResponse saved = await Set(KeyEducationRetryCount, request.UserId, countDto);

				return saved.IsSuccess;
			}

			_logger.LogError("Error while decrease retry count. User ({user}) has no free retry count to decrease.", request.UserId);

			return false;
		});

		public async ValueTask<CommonGrpcResponse> DecreaseRetryDateAsync(DecreaseRetryDateGrpcRequest request) => await DecreaseRetryAsync(request, async userId =>
		{
			EducationRetryLastDateDto lastDateDto = await GetEducationRetryLastDate(userId);

			DateTime? date = lastDateDto.Date;
			if (date == null || OneDayGone(date.Value))
			{
				lastDateDto.Date = _systemClock.Now;

				CommonGrpcResponse saved = await Set(KeyEducationRetryLastDate, request.UserId, lastDateDto);

				return saved.IsSuccess;
			}

			_logger.LogError("Error while set new last retry date. User ({user}) has last retry date less than a day ago ({date}).", request.UserId, date);

			return false;
		});

		private async ValueTask<CommonGrpcResponse> DecreaseRetryAsync(IDecreaseRetryRequest request, Func<Guid?, ValueTask<bool>> reserveFunc)
		{
			Guid? userId = request.UserId;
			List<EducationRetryTaskDto> taskDto = (await GetEducationRetryTasks(userId)).ToList();

			//Already in retry state
			if (TaskInRetry(request.Tutorial, request.Unit, request.Task, taskDto))
				return CommonGrpcResponse.Fail;

			//Try to set reservation
			if (!await reserveFunc.Invoke(userId))
				return CommonGrpcResponse.Fail;

			taskDto.Add(new EducationRetryTaskDto
			{
				Tutorial = request.Tutorial,
				Unit = request.Unit,
				Task = request.Task
			});

			//Set task to retry state
			CommonGrpcResponse response = await Set(KeyEducationRetryTask, userId, taskDto.ToArray());
			if (response.IsSuccess)
				await UpdateRetryUsedCount(request.UserId);

			return response;
		}

		private async Task UpdateRetryUsedCount(Guid? userId)
		{
			EducationRetryUsedCountDto usedCountDto = await Get<EducationRetryUsedCountDto>(KeyEducationRetryUsedCount, userId)
				?? new EducationRetryUsedCountDto();

			usedCountDto.Count++;

			CommonGrpcResponse response = await Set(KeyEducationRetryUsedCount, userId, usedCountDto);
			if (!response.IsSuccess)
			{
				_logger.LogError("Error while update used retry count for user: {user}.", userId);
				return;
			}

			await _publisher.PublishAsync(new UpdateRetryUsedCountInfoServiceBusModel
			{
				UserId = userId,
				Count = usedCountDto.Count
			});
		}

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

		public async ValueTask<CommonGrpcResponse> IncreaseRetryCountAsync(IncreaseRetryCountGrpcRequest request)
		{
			EducationRetryCountDto retryCountDto = await GetEducationRetryCount(request.UserId);

			retryCountDto.Count += request.Value;

			return await Set(KeyEducationRetryCount, request.UserId, retryCountDto);
		}

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

		public async ValueTask<RetryLastDateGrpcResponse> GetRetryLastDateAsync(GetRetryLastDateGrpcRequest request)
		{
			EducationRetryLastDateDto lastDateDto = await GetEducationRetryLastDate(request.UserId);

			return new RetryLastDateGrpcResponse
			{
				Date = lastDateDto.Date
			};
		}

		private static bool TaskInRetry(EducationTutorial tutorial, int unit, int task, IEnumerable<EducationRetryTaskDto> taskDto) => taskDto
			.Where(dto => dto.Tutorial == tutorial)
			.Where(dto => dto.Unit == unit)
			.Any(dto => dto.Task == task);

		private async ValueTask<EducationRetryTaskDto[]> GetEducationRetryTasks(Guid? userId) =>
			await Get<EducationRetryTaskDto[]>(KeyEducationRetryTask, userId) ?? Array.Empty<EducationRetryTaskDto>();

		private async ValueTask<EducationRetryCountDto> GetEducationRetryCount(Guid? userId) =>
			await Get<EducationRetryCountDto>(KeyEducationRetryCount, userId) ?? new EducationRetryCountDto();

		private async ValueTask<EducationRetryLastDateDto> GetEducationRetryLastDate(Guid? userId) =>
			await Get<EducationRetryLastDateDto>(KeyEducationRetryLastDate, userId) ?? new EducationRetryLastDateDto();

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

		private bool OneDayGone(DateTime date) => _systemClock.Now.Subtract(date).TotalDays >= 1;
	}
}