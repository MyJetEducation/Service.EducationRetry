using System.Runtime.Serialization;

namespace Service.EducationRetry.Grpc.Models
{
	[DataContract]
	public class RetryCountGrpcResponse
	{
		[DataMember(Order = 1)]
		public int Count { get; set; }
	}
}