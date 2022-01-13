using System;
using Service.Core.Domain.Models.Education;

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