using Service.Core.Domain.Models.Education;

namespace Service.EducationRetry.Domain.Models
{
	public class EducationRetryTaskDto
	{
		public EducationTutorial Tutorial { get; set; }

		public int Unit { get; set; }

		public int Task { get; set; }
	}
}