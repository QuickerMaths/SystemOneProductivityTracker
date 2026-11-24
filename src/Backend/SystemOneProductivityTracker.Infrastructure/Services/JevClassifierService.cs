using Jev.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Diagnostics;
using System.Net.Sockets;
using SystemOneProductivityTracker.Application.Interfaces;
using SystemOneProductivityTracker.Domain.Entities;
using SystemOneProductivityTracker.Infrastructure.Persistence;

namespace SystemOneProductivityTracker.Application.Services
{
    sealed class AppClassification : SystemOneResponse
    {
        //public NoulAnswer Blocked { get; set; } = null!;
        public ChoiceAnswer Classification { get; set; } = null!;
        //[OptionalAnswer] public ScoreAnswer? Urgency { get; set; }
    }

    public sealed class JevClassifierService : IJevClassficationService
    {
        private readonly IDbContextFactory<SystemOneProductivityTrackerDbContext> _dbFactory;
        private IAuthService _authService;
        private readonly ITypeSafeClient _typeSafeClient;
        private List<string> _categories;

        private PlannedBlock _currentBlock;
        
        public JevClassifierService(
            IDbContextFactory<SystemOneProductivityTrackerDbContext> dbFactory,
            IAuthService authService,
            ITypeSafeClient typeSafeClient)
        {
            _dbFactory = dbFactory;
            _authService = authService;
            _typeSafeClient = typeSafeClient;
        }

        public async Task ClassifyAppAsync(JevClassificationRequest request)
        {
            // TODO: porzebana będzie implementacja lokalnej bazy dla każdego usera
            // żeby aplikacja działała bez dostępu do internetu 
            await using var db = await _dbFactory.CreateDbContextAsync();

            // TODO: narazie wczytujemy kategorie przy każdym requescie
            // lepszy rozwiązaniem było by dodanie eventu utowrzenia nowej kategorii
            // i odświeżania kategorii znajdujących się w pamięci tylko po wywołaniu
            _categories = db.AppCategories
                .Select(x => x.Name)
                .ToList();

            _currentBlock = db.PlannedBlocks
                .FirstOrDefault()
                ?? throw new Exception("404");

            //TryCacheClassification();

            await ModelClassificationAsync(request);
        }

        private void TryCacheClassification(JevClassificationRequest request)
        {

        }

        private async Task ModelClassificationAsync(JevClassificationRequest request)
        {
            var state = $"Current block name is {_currentBlock.Title}. This block allows to run only processes related to it's name.";
            var questions = new Dictionary<string, Question>
            {
                ["classification"] = new Choice(_categories, $"Classify the {request.ProcessName} process.")
            };

            try
            {
                var result = await _typeSafeClient.SystemOneAsync<AppClassification>(state, questions);

                string classification = result.Choices["classification"].Choice;

                Debug.WriteLine(classification);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message ?? "500");
            }
        }
    }
}