using System;
using System.Runtime.Serialization;

namespace Service.EducationRetry.Grpc.ServiceBusModel
{
	/// <summary>
	///     Использование сброса результатов
	/// </summary>
	[DataContract]
	public class RetryUsedServiceBusModel
	{
		public const string TopicName = "myjeteducation-retry-used";

		[DataMember(Order = 1)]
		public Guid? UserId { get; set; }

		[DataMember(Order = 2)]
		public int Count { get; set; }
	}
}