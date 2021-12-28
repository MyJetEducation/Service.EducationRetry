using System.Collections.Generic;

namespace Service.EducationRetry.Domain.Models
{
	public class EducationRetryTaskDto
	{
		public EducationRetryTaskDto()
		{
			Dtos = new List<EducationRetryTaskItemDto>();
		}

		public List<EducationRetryTaskItemDto> Dtos { get; set; }
	}
}