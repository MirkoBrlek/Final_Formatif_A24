﻿using BackgroundServiceMath.Data;
using BackgroundServiceMath.Models;
using BackgroundServiceVote.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackgroundServiceMath.Services;

public class UserData
{
    public int Choice { get; set; } = -1;
    public int NbConnections { get; set; } = 0;
}

public class MathBackgroundService : BackgroundService
{
    public const int DELAY = 20 * 1000;

    private Dictionary<string, UserData> _data = new();

    private IHubContext<MathQuestionsHub> _mathQuestionHub;

    private MathQuestion? _currentQuestion;
    private readonly IServiceScopeFactory _scopeFactory;
    public MathQuestion? CurrentQuestion => _currentQuestion;

    private MathQuestionsService _mathQuestionsService;

    public MathBackgroundService(IHubContext<MathQuestionsHub> mathQuestionHub, MathQuestionsService mathQuestionsService, IServiceScopeFactory serviceScopeFactory)
    {
        _mathQuestionHub = mathQuestionHub;
        _mathQuestionsService = mathQuestionsService;
        _scopeFactory = serviceScopeFactory;
    }

    public void AddUser(string userId)
    {
        if (!_data.ContainsKey(userId))
        { 
            _data[userId] = new UserData();
        }
        _data[userId].NbConnections++;
    }

    public void RemoveUser(string userId)
    {
        if (!_data.ContainsKey(userId))
        {
            _data[userId].NbConnections--;
            if(_data[userId].NbConnections <= 0)
                _data.Remove(userId);
        }
    }

    public async void SelectChoice(string userId, int choice)
    {
        if (_currentQuestion == null)
            return;

        UserData userData = _data[userId];
            
        if (userData.Choice != -1)
            throw new Exception("A user cannot change is choice!");

        userData.Choice = choice;

        _currentQuestion.PlayerChoices[choice]++;

        // TODO: Notifier les clients qu'un joueur a choisi une réponse
        await _mathQuestionHub.Clients.All.SendAsync("IncreasePlayersChoices",choice);
    }

    private async Task EvaluateChoices()
    {
        // TODO: La méthode va avoir besoin d'un scope
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BackgroundServiceContext>();

            var rightPlayers = new List<string>();

            // TODO: Notifier les clients pour les bonnes et mauvaises réponses
            await _mathQuestionHub.Clients.All.SendAsync("Answer", _currentQuestion!.RightAnswerIndex, _currentQuestion.Answers[_currentQuestion.RightAnswerIndex]);
            foreach (var userId in _data.Keys)
            {
                var userData = _data[userId];                
                
                // TODO: Modifier et sauvegarder le NbRightAnswers des joueurs qui ont la bonne réponse
                if (userData.Choice == _currentQuestion!.RightAnswerIndex)
                {
                    rightPlayers.Add(userId);
                }
                else
                {
                }

            }
            foreach (var id in rightPlayers)
            {
                var player = context.Player.Where(p=>p.UserId == id).First();
                if (player != null)
                {
                    player.NbRightAnswers++;
                }
            }

            await context.SaveChangesAsync();
            // Reset
            foreach (var key in _data.Keys)
            {
                _data[key].Choice = -1;
            }
        }
    }

    private async Task Update(CancellationToken stoppingToken)
    {
        if (_currentQuestion != null)
        {
            await EvaluateChoices();
        }

        _currentQuestion = _mathQuestionsService.CreateQuestion();

        await _mathQuestionHub.Clients.All.SendAsync("CurrentQuestion", _currentQuestion);
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Update(stoppingToken);
            await Task.Delay(DELAY, stoppingToken);
        }
    }
}