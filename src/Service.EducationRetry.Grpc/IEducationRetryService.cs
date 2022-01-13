using System.ServiceModel;
using System.Threading.Tasks;
using Service.Core.Grpc.Models;
using Service.EducationRetry.Grpc.Models;

namespace Service.EducationRetry.Grpc
{
	[ServiceContract]
	public interface IEducationRetryService
	{
		[OperationContract]
		ValueTask<CommonGrpcResponse> IncreaseRetryCountAsync(IncreaseRetryCountGrpcRequest request);

		[OperationContract]
		ValueTask<CommonGrpcResponse> DecreaseRetryCountAsync(DecreaseRetryCountGrpcRequest request);

		[OperationContract]
		ValueTask<CommonGrpcResponse> DecreaseRetryDateAsync(DecreaseRetryDateGrpcRequest request);

		[OperationContract]
		ValueTask<RetryCountGrpcResponse> GetRetryCountAsync(GetRetryCountGrpcRequest request);

		[OperationContract]
		ValueTask<TaskRetryStateGrpcResponse> GetTaskRetryStateAsync(GetTaskRetryStateGrpcRequest request);

		[OperationContract]
		ValueTask<CommonGrpcResponse> ClearTaskRetryStateAsync(ClearTaskRetryStateGrpcRequest request);

		[OperationContract]
		ValueTask<RetryLastDateGrpcResponse> GetRetryLastDateAsync(GetRetryLastDateGrpcRequest request);
	}
}