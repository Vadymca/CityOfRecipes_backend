namespace CityOfRecipes_backend.Services
{
    public class ContestResultScheduler : BackgroundService
    {
        private readonly ILogger<ContestResultScheduler> _logger;
        private readonly ContestService _contestService;
        private Timer? _timer;

        public ContestResultScheduler(ILogger<ContestResultScheduler> logger, ContestService contestService)
        {
            _logger = logger;
            _contestService = contestService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ContestResultScheduler запущено.");

            // Виконуємо першу перевірку одразу
            await DoWork(null);

            // Обчислюємо час до наступної перевірки о 00:00 UTC
            var now = DateTime.Now;
            var nextRunTime = now.Date.AddDays(1); // Завтра о 00:00 UTC
            var initialDelay = nextRunTime - now;

            _logger.LogInformation($"Наступна перевірка запланована через {initialDelay.TotalHours:F2} годин (о 00:00 UTC).");

            // Запускаємо таймер на виконання кожну добу о 00:00 UTC
            _timer = new Timer(async _ => await DoWork(null), null, initialDelay, TimeSpan.FromDays(1));
        }

        private async Task DoWork(object? state) 
        {
            try
            {
                var now = DateTime.Now;
                _logger.LogInformation($"Перевірка завершених конкурсів. Поточний UTC час: {now}");

                var finishedContests = await _contestService.GetFinishedContestsAsync();
                _logger.LogInformation($"Знайдено {finishedContests.Count} завершених конкурсів.");

                foreach (var contest in finishedContests)
                {
                    _logger.LogInformation($"Обробляємо конкурс {contest.Id}. Дата завершення: {contest.EndDate}");

                    if (contest.WinningRecipes != null && contest.WinningRecipes.Any())
                    {
                        _logger.LogInformation($"Конкурс {contest.Id} вже має переможців. Пропускаємо.");
                        continue;
                    }

                    await _contestService.DetermineContestWinnersAsync(contest.Id);
                    _logger.LogInformation($"Підбиті підсумки конкурсу {contest.Id}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка у ContestResultScheduler.");
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ContestResultScheduler зупинено.");
            _timer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(stoppingToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}
