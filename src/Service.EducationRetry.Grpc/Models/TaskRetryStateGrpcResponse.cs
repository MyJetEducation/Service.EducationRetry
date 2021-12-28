using System.Runtime.Serialization;

namespace Service.EducationRetry.Grpc.Models
{
	[DataContract]
	public class TaskRetryStateGrpcResponse
	{
		[DataMember(Order = 1)]
		public bool InRetry { get; set; }
	}
}