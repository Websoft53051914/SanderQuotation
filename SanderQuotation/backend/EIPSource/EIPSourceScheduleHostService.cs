using Business.BusinessLogic;
using backend.Common;

namespace backend.EIPSource
{
    public class EIPSourceScheduleHostService : IHostedService
    {
        private readonly HangfireSchedulerHelper _hangfireSchedulerHelper;
        public EIPSourceScheduleHostService(HangfireSchedulerHelper hangfireSchedulerHelper)
        {
            _hangfireSchedulerHelper = hangfireSchedulerHelper;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                EsScheduleCycleBL bl = BLFactory.GetInstanceBackGround<EsScheduleCycleBL>();
                var data = bl.GetAllActive();

                foreach (var item in data)
                {
                    _hangfireSchedulerHelper.InsertRecurringJob(item.ScheduleCycleCode, item.CronExpression);
                }

            }
            catch (Exception ex)
            {
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {

            return Task.CompletedTask;
        }
    }
}
