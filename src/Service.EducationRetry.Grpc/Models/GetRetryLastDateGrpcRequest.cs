using System;
using System.Runtime.Serialization;

namespace Service.EducationRetry.Grpc.Models
{
	[DataContract]
	public class GetRetryLastDateGrpcRequest
	{
		[DataMember(Order = 1)]
		public Guid? UserId { get; set; }
	}
}