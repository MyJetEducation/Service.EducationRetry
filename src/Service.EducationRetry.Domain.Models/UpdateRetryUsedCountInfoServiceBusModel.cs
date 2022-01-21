using System;
using System.Runtime.Serialization;

namespace Service.EducationRetry.Domain.Models
{
	[DataContract]
	public class UpdateRetryUsedCountInfoServiceBusModel
	{
		public const string TopicName = "myjeteducation-update-retry-used-count";

		[DataMember(Order = 1)]
		public Guid? UserId { get; set; }

		[DataMember(Order = 2)]
		public int Count { get; set; }
	}
}