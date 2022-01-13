using System;
using System.Runtime.Serialization;

namespace Service.EducationRetry.Grpc.Models
{
	[DataContract]
	public class RetryLastDateGrpcResponse
	{
		[DataMember(Order = 1)]
		public DateTime? Date { get; set; }
	}
}