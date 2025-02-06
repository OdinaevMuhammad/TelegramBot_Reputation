using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Entities.Models;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace Services;

class Program2
{
    private static readonly string Token = "7941883332:AAE4SgDROW3N_IlPX0ieOfDBF5uu6P492O4";
    private static readonly ITelegramBotClient Bot = new TelegramBotClient(Token);
    private static readonly string ReputationFilePath = @"C:\Users\mukhammad.odinaev\RiderProjects\TelegramBot_Donishgoh\Services\Dbjson\reputation.json";
    private static List<TelegramUser> Users = [];

    private static async Task Main2()
    {
        try
        {
           await Bot.SendTextMessageAsync(6763328340, "Баха шалава!");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}