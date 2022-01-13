using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.EducationRetry.Settings
{
	public class SettingsModel
	{
		[YamlProperty("EducationRetry.SeqServiceUrl")]
		public string SeqServiceUrl { get; set; }

		[YamlProperty("EducationRetry.ZipkinUrl")]
		public string ZipkinUrl { get; set; }

		[YamlProperty("EducationRetry.ElkLogs")]
		public LogElkSettings ElkLogs { get; set; }

		[YamlProperty("EducationRetry.ServerKeyValueServiceUrl")]
		public string ServerKeyValueServiceUrl { get; set; }

		[YamlProperty("EducationRetry.KeyEducationRetryCount")]
		public string KeyEducationRetryCount { get; set; }

		[YamlProperty("EducationRetry.KeyEducationRetryLastDate")]
		public string KeyEducationRetryLastDate { get; set; }

		[YamlProperty("EducationRetry.KeyEducationRetryTask")]
		public string KeyEducationRetryTask { get; set; }
	}
}