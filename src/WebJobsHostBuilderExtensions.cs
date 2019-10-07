using Microsoft.Extensions.Hosting;

namespace EL.ServiceBus
{
    public static class WebJobsHostBuilderExtensions
    {
        public static IHostBuilder ConfigureELMessaging(this IHostBuilder builder)
        {
            return builder.ConfigureWebJobs(x => x.AddServiceBus());
        }
    }
}
