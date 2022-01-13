using Autofac;
using Microsoft.AspNetCore.Authentication;

namespace Service.EducationRetry.Modules
{
    public class SettingsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(Program.Settings).AsSelf().SingleInstance();
            builder.RegisterType<SystemClock>().AsImplementedInterfaces().SingleInstance();
        }
    }
}
