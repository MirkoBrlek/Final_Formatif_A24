﻿using BackgroundServiceMath.Data;
using BackgroundServiceMath.DTOs;
using BackgroundServiceMath.Models;
using BackgroundServiceMath.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BackgroundServiceVote.Hubs;

[Authorize]
public class MathQuestionsHub : Hub
{
    private MathBackgroundService _matchBackgroundService;
    private BackgroundServiceContext _backgroundServiceContext;

    public MathQuestionsHub(MathBackgroundService matchBackgroundService, BackgroundServiceContext backgroundServiceContext)
    {
        _matchBackgroundService = matchBackgroundService;
        _backgroundServiceContext = backgroundServiceContext;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        _matchBackgroundService.AddUser(Context.UserIdentifier!);

        Player player = _backgroundServiceContext.Player.Where(p => p.UserId == Context.UserIdentifier!).Single();

        await Clients.Caller.SendAsync("PlayerInfo", new PlayerInfoDTO()
        {
            NbRightAnswers = player.NbRightAnswers,
        });

        await Clients.Caller.SendAsync("CurrentQuestion", _matchBackgroundService.CurrentQuestion);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _matchBackgroundService.RemoveUser(Context.UserIdentifier!);
        await base.OnDisconnectedAsync(exception);
    }

    public void SelectChoice(int answerIndex)
    {
        _matchBackgroundService.SelectChoice(Context.UserIdentifier!, answerIndex);
    }

    public async void UpdateNbBonnesReponses(List<string> playerIDs)
    {
        foreach (var id in playerIDs)
        {
            var player = await _backgroundServiceContext.Player.FindAsync(id);
            if (player != null)
            {
                player.NbRightAnswers++;
                await _backgroundServiceContext.SaveChangesAsync();
            }
        }
    }
}