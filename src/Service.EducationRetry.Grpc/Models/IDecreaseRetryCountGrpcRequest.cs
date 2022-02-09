using System;
using Service.Education.Structure;

namespace Service.EducationRetry.Grpc.Models
{
	public interface IDecreaseRetryRequest
	{
		Guid? UserId { get; set; }

		EducationTutorial Tutorial { get; set; }

		int Unit { get; set; }

		int Task { get; set; }
	}
}