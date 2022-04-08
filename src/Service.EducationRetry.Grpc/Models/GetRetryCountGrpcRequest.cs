using System.Runtime.Serialization;

namespace Service.EducationRetry.Grpc.Models
{
	[DataContract]
	public class GetRetryCountGrpcRequest
	{
		[DataMember(Order = 1)]
		public string UserId { get; set; }
	}
}