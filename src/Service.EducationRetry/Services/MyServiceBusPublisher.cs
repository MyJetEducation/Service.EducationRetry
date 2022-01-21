using System.Threading.Tasks;
using DotNetCoreDecorators;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.TcpClient;
using Service.EducationRetry.Domain.Models;

namespace Service.EducationRetry.Services
{
	public class MyServiceBusPublisher : IPublisher<UpdateRetryUsedCountInfoServiceBusModel>
	{
		private readonly MyServiceBusTcpClient _client;

		public MyServiceBusPublisher(MyServiceBusTcpClient client)
		{
			_client = client;
			_client.CreateTopicIfNotExists(UpdateRetryUsedCountInfoServiceBusModel.TopicName);
		}

		public ValueTask PublishAsync(UpdateRetryUsedCountInfoServiceBusModel valueToPublish)
		{
			byte[] bytesToSend = valueToPublish.ServiceBusContractToByteArray();

			Task task = _client.PublishAsync(UpdateRetryUsedCountInfoServiceBusModel.TopicName, bytesToSend, false);

			return new ValueTask(task);
		}
	}
}