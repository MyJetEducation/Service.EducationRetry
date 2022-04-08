using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.ServiceBus;
using Service.Core.Client.Models;
using Service.Core.Client.Services;
using Service.Education.Constants;
using Service.Education.Helpers;
using Service.Education.Structure;
using Service.EducationProgress.Grpc;
using Service.EducationProgress.Grpc.Models;
using Service.EducationRetry.Domain.Models;
using Service.EducationRetry.Grpc;
using Service.EducationRetry.Grpc.Models;
using Service.EducationRetry.Models;
using Service.ServiceBus.Models;

namespace Service.EducationRetry.Services
{
	public class EducationRetryService : IEducationRetryService
	{
		private static Func<string> KeyEducationRetryUsedCount => Program.ReloadedSettings(model => model.KeyEducationRetryUsedCount);
		private static Func<string> KeyEducationRetryCount => Program.ReloadedSettings(model => model.KeyEducationRetryCount);
		private static Func<string> KeyEducationRetryLastDate => Program.ReloadedSettings(model => model.KeyEducationRetryLastDate);
		private static Func<string> KeyEducationRetryTask => Program.ReloadedSettings(model => model.KeyEducationRetryTask);

		private readonly ILogger<EducationRetryService> _logger;
		private readonly ISystemClock _systemClock;
		private readonly IServiceBusPublisher<RetryUsedServiceBusModel> _publisher;
		private readonly IEducationProgressService _educationProgressService;
		private readonly IRetryRepository _retryRepository;

		public EducationRetryService(ILogger<EducationRetryService> logger,
			ISystemClock systemClock,
			IServiceBusPublisher<RetryUsedServiceBusModel> publisher,
			IEducationProgressService educationProgressService, IRetryRepository retryRepository)
		{
			_logger = logger;
			_systemClock = systemClock;
			_publisher = publisher;
			_educationProgressService = educationProgressService;
			_retryRepository = retryRepository;
		}

		public async ValueTask<CommonGrpcResponse> DecreaseRetryCountAsync(DecreaseRetryCountGrpcRequest request) => await DecreaseRetryAsync(request, async userId =>
		{
			EducationRetryCountDto countDto = await _retryRepository.GetEducationRetryCount(userId);

			countDto.Count--;

			if (countDto.Count >= 0)
			{
				CommonGrpcResponse saved = await _retryRepository.Set(KeyEducationRetryCount, request.UserId, countDto);

				return saved.IsSuccess;
			}

			_logger.LogError("Error while decrease retry count. User ({user}) has no free retry count to decrease.", request.UserId);

			return false;
		});

		public async ValueTask<CommonGrpcResponse> DecreaseRetryDateAsync(DecreaseRetryDateGrpcRequest request) => await DecreaseRetryAsync(request, async userId =>
		{
			EducationRetryLastDateDto lastDateDto = await _retryRepository.GetEducationRetryLastDate(userId);

			DateTime? date = lastDateDto.Date;
			if (date == null || OneDayGone(date.Value))
			{
				lastDateDto.Date = _systemClock.Now;

				CommonGrpcResponse saved = await _retryRepository.Set(KeyEducationRetryLastDate, request.UserId, lastDateDto);

				return saved.IsSuccess;
			}

			_logger.LogError("Error while set new last retry date. User ({user}) has last retry date less than a day ago ({date}).", request.UserId, date);

			return false;
		});

		private async ValueTask<CommonGrpcResponse> DecreaseRetryAsync(IDecreaseRetryRequest request, Func<string, ValueTask<bool>> reserveFunc)
		{
			string userId = request.UserId;

			//Task has invalid progress value
			if (await InvalidProgress(request))
				return CommonGrpcResponse.Fail;

			List<EducationRetryTaskDto> taskDto = (await _retryRepository.GetEducationRetryTasks(userId)).ToList();

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
			CommonGrpcResponse response = await _retryRepository.Set(KeyEducationRetryTask, userId, taskDto.ToArray());
			if (response.IsSuccess)
				await UpdateRetryUsedCount(request.UserId);

			return response;
		}

		private async Task UpdateRetryUsedCount(string userId)
		{
			EducationRetryUsedCountDto usedCountDto = await _retryRepository.Get<EducationRetryUsedCountDto>(KeyEducationRetryUsedCount, userId)
				?? new EducationRetryUsedCountDto();

			usedCountDto.Count++;

			CommonGrpcResponse response = await _retryRepository.Set(KeyEducationRetryUsedCount, userId, usedCountDto);
			if (!response.IsSuccess)
			{
				_logger.LogError("Error while update used retry count for user: {user}.", userId);
				return;
			}

			await _publisher.PublishAsync(new RetryUsedServiceBusModel
			{
				UserId = userId,
				Count = usedCountDto.Count
			});
		}

		public async ValueTask<CommonGrpcResponse> ClearTaskRetryStateAsync(ClearTaskRetryStateGrpcRequest request)
		{
			string userId = request.UserId;

			List<EducationRetryTaskDto> taskDto = (await _retryRepository.GetEducationRetryTasks(userId)).ToList();

			EducationRetryTaskDto item = taskDto
				.Where(dto => dto.Tutorial == request.Tutorial)
				.Where(dto => dto.Unit == request.Unit)
				.FirstOrDefault(dto => dto.Task == request.Task);

			if (item == null)
				return CommonGrpcResponse.Success;

			taskDto.Remove(item);

			return taskDto.Count == 0
				? await _retryRepository.Delete(KeyEducationRetryTask, userId)
				: await _retryRepository.Set(KeyEducationRetryTask, userId, taskDto.ToArray());
		}

		public async ValueTask<CommonGrpcResponse> IncreaseRetryCountAsync(IncreaseRetryCountGrpcRequest request)
		{
			EducationRetryCountDto retryCountDto = await _retryRepository.GetEducationRetryCount(request.UserId);

			retryCountDto.Count += request.Value;

			return await _retryRepository.Set(KeyEducationRetryCount, request.UserId, retryCountDto);
		}

		public async ValueTask<RetryCountGrpcResponse> GetRetryCountAsync(GetRetryCountGrpcRequest request)
		{
			EducationRetryCountDto countDto = await _retryRepository.GetEducationRetryCount(request.UserId);

			return new RetryCountGrpcResponse
			{
				Count = countDto.Count
			};
		}

		public async ValueTask<TaskRetryStateGrpcResponse> GetTaskRetryStateAsync(GetTaskRetryStateGrpcRequest request)
		{
			EducationRetryTaskDto[] taskDto = await _retryRepository.GetEducationRetryTasks(request.UserId);

			return new TaskRetryStateGrpcResponse
			{
				InRetry = TaskInRetry(request.Tutorial, request.Unit, request.Task, taskDto)
			};
		}

		public async ValueTask<RetryLastDateGrpcResponse> GetRetryLastDateAsync(GetRetryLastDateGrpcRequest request)
		{
			EducationRetryLastDateDto lastDateDto = await _retryRepository.GetEducationRetryLastDate(request.UserId);

			return new RetryLastDateGrpcResponse
			{
				Date = lastDateDto.Date
			};
		}

		private async ValueTask<bool> InvalidProgress(IDecreaseRetryRequest request)
		{
			TaskEducationProgressGrpcResponse progressResponse = await _educationProgressService.GetTaskProgressAsync(new GetTaskEducationProgressGrpcRequest
			{
				UserId = request.UserId,
				Tutorial = request.Tutorial,
				Unit = request.Unit,
				Task = request.Task
			});

			EducationStructureTask task = EducationHelper.GetTask(request.Tutorial, request.Unit, request.Task);

			int? progressValue = progressResponse?.Progress?.Value;

			return progressValue == null || progressValue == Progress.MaxProgress && task.TaskType != EducationTaskType.Game;
		}

		private static bool TaskInRetry(EducationTutorial tutorial, int unit, int task, IEnumerable<EducationRetryTaskDto> taskDto) => taskDto
			.Where(dto => dto.Tutorial == tutorial)
			.Where(dto => dto.Unit == unit)
			.Any(dto => dto.Task == task);

		private bool OneDayGone(DateTime date) => _systemClock.Now.Subtract(date).TotalDays >= 1;
	}
}