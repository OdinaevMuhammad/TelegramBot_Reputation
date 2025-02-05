using System.Linq;
using Entities.Models;

namespace Services;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;

class Program
{
    private static readonly string Token = "а что ты думал что в сказку попал?";
    private static readonly ITelegramBotClient Bot = new TelegramBotClient(Token);
    private static readonly string ReputationFilePath = @"C:\Users\mukhammad.odinaev\RiderProjects\TelegramBot_Donishgoh\Services\Dbjson\reputation.json";
    private static List<TelegramUser> Users = [];

    static async Task Main()
    {
        LoadReputation();
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions { AllowedUpdates = { } };

        Bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cts.Token);

        Console.WriteLine("Бот запущен. Нажми Ctrl+C для выхода.");
        await Task.Delay(-1); // Бесконечное ожидание
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message == null)
            return;
        
        var message = update.Message;
        var chatId = message.Chat.Id;
        var userId = message.From!.Id;
        var firstName = message.From.FirstName;
        var userName = message.From.Username;
        var target = message.ReplyToMessage?.From;
        
        if (message.Text!.StartsWith("/rep"))
        {
            if (target == null)
            {
                await bot.SendTextMessageAsync(chatId, "Ответьте на сообщение человека, которому хотите изменить репутацию.", cancellationToken: cancellationToken);
                return;
            }
        
            if (target.Id == userId)
            {
                await bot.SendTextMessageAsync(chatId, "Вы не можете изменить свою репутацию.", cancellationToken: cancellationToken);
                return;
            }
        
            var currentUsers = new List<TelegramUser>() { new(userId, firstName, userName), new(target.Id, target.FirstName, target.Username) };
            await CreateTelegramUsers(currentUsers);
            
            var targetUser = Users.FirstOrDefault(x => x.Id == target.Id);

            var text = "";
            if (message.Text.Contains("+"))
            {
                targetUser.Reputation += 1;
                text = "повысил";
            }
            else if (message.Text.Contains("-"))
            {
                if (targetUser.Reputation <= 0)
                {
                    text = "уже некуда понижать, пытался понизить";
                }
                else
                {
                    targetUser.Reputation -= 1;
                    text = "понизил";   
                }
            }
            else
            {
                await bot.SendTextMessageAsync(chatId, "Неправильный аргумент команды", cancellationToken: cancellationToken);
                return;
            }
            string telegramLinkTarget = $"<a href=\"https://t.me/{target.Username}\">{target.FirstName}</a>";
            string telegramLinkCurrentUser = $"<a href=\"https://t.me/{userName}\">{firstName}</a>";
            
            string repMessage = $"🔹 {telegramLinkTarget}, вашу репутацию {text} {telegramLinkCurrentUser}. Теперь ваша репутация: 🏆 {targetUser.Reputation}." +
                                $"\n\nПримечание: Ваша репутация - это разница между положительными и отрицательными отзывами." +
                                $" Для просмотра своей репутации используйте команду:\n/myrep";

            await bot.SendTextMessageAsync(chatId, repMessage, cancellationToken: cancellationToken, parseMode: ParseMode.Html, linkPreviewOptions: true);
            SaveReputation();
        }
        else if (message.Text.StartsWith("/myrep"))
        {
            var user = Users.FirstOrDefault(x => x.Id == userId);
            int reputation = user?.Reputation ?? 0;

            CreateTelegramUser(userId, firstName);
            await bot.SendTextMessageAsync(chatId, $"📊 Ваша репутация: {reputation}", cancellationToken: cancellationToken);
        }
        
        else if (message.Text.StartsWith("/rating"))
        {
            var userDesc = Users.OrderByDescending(x => x.Reputation).Take(3).ToList();
    
            if (userDesc.Count > 0)
            {
                string ratingMessage = $"📊1 😌 _Легенда_ *[{userDesc[0].FullName}](https://t.me/{userDesc[0].UserName})* \\- \\{userDesc[0].Reputation} репутации";

                if (userDesc.Count > 1)
                {
                    ratingMessage += $"\n📊2 \ud83d\ude0a _Мастер_ *[{userDesc[1].FullName}](https://t.me/{userDesc[1].UserName})* \\- \\{userDesc[1].Reputation} репутации";
                }

                if (userDesc.Count > 2)
                {
                    ratingMessage += $"\n📊3 \ud83d\ude03 _Уравнитель_*[{userDesc[2].FullName}](https://t.me/{userDesc[2].UserName})* \\- \\{userDesc[2].Reputation} репутации";
                }

                await bot.SendTextMessageAsync(chatId, ratingMessage, parseMode: ParseMode.MarkdownV2, cancellationToken: cancellationToken, linkPreviewOptions: true);

            }
            else
            {
                await bot.SendTextMessageAsync(chatId, "Список пользователей пуст!", cancellationToken: cancellationToken);
            }
        }
    }

    private static TelegramUser CreateTelegramUser(long userId, string firstname)
    {
        TelegramUser targetUser;
        targetUser = new TelegramUser(userId, 0, firstname);
        Users.Add(targetUser);
        return targetUser;
    }
    private static Task CreateTelegramUsers(List<TelegramUser> users)
    {
        foreach (var user in users)
        {
            if (Users.All(x => x.Id != user.Id))
            {
                Users.Add(user);
            } 
        }

        return Task.CompletedTask;
    }

    private static string EscapeMarkdownV2(string text)
    {
        string[] specialChars = { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
        foreach (var ch in specialChars)
        {
            text = text.Replace(ch, "\\" + ch);
        }
        return text;
    }



    private static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }

    // Метод для загрузки репутации из файла
    private static void LoadReputation()
    {
        if (File.Exists(ReputationFilePath))
        {
            var json = File.ReadAllText(ReputationFilePath);
            if (!string.IsNullOrWhiteSpace(json))
            {
                Users = JsonConvert.DeserializeObject<List<TelegramUser>>(json) ?? new List<TelegramUser>();
            }
        }
        else
        {
            File.WriteAllText(ReputationFilePath, "[]"); // Создаем пустой JSON, а не пустой файл
        }
    }

    // Метод для сохранения репутации в файл
    private static void SaveReputation()
    {
        var updatedJson = JsonConvert.SerializeObject(Users, Formatting.Indented);
        File.WriteAllText(ReputationFilePath, updatedJson);
        Console.WriteLine("Изменения сохранены в файл.");
    }
}
