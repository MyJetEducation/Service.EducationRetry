using Service.Core.Client.Education;

namespace Service.EducationRetry.Models
{
	public class EducationRetryTaskDto
	{
		public EducationTutorial Tutorial { get; set; }

		public int Unit { get; set; }

		public int Task { get; set; }
	}
}