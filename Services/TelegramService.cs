using System.Linq;
using Entities.Models;
using Services.Helpers;

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

internal abstract class Program
{
    private static readonly string Token = Env.GetOrDie("TELEGRAM_BOT_TOKEN");
    private static readonly ITelegramBotClient Bot = new TelegramBotClient(Token);
    private static readonly string ReputationFilePath = Env.GetOrDie("ReputationFilePath");
    private static List<TelegramUser> _users = [];

    static async Task Main()
    {
        string currentDir = Directory.GetCurrentDirectory();
        Env.FindAndLoadEnv(currentDir);

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
            
            var targetUser = _users.FirstOrDefault(x => x.Id == target.Id);

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
            var user = _users.FirstOrDefault(x => x.Id == userId);
            int reputation = user?.Reputation ?? 0;

            await CreateTelegramUser(userId, firstName);
            await bot.SendTextMessageAsync(chatId, $"📊 Ваша репутация: {reputation}", cancellationToken: cancellationToken);
        }
        
        else if (message.Text.StartsWith("/rating"))
        {
            var list = new List<string>()
            {
                "Легенда",
                "Мастер",
                "Уравнитель"
            };

            var behruz = "Автолюбитель";
            var userDesc = _users.OrderByDescending(x => x.Reputation).Take(3).ToList();


            if (userDesc.Any(x => x.Id == 8142338825))
            {
                var index = userDesc.FindIndex(x => x.Id == 8142338825);

                if (index != 0)
                {
                    list[index] = behruz;
                }
            }

            if (userDesc.Count > 0)
            {
                string ratingMessage = $"📊1 😌 _{list[0]}_ *[{userDesc[0].FullName}](https://t.me/{userDesc[0].UserName})* \\- \\{userDesc[0].Reputation} репутации";

                if (userDesc.Count > 1)
                {
                    ratingMessage += $"\n📊2 \ud83d\ude0a _{list[1]}_ *[{userDesc[1].FullName}](https://t.me/{userDesc[1].UserName})* \\- \\{userDesc[1].Reputation} репутации";
                }

                if (userDesc.Count > 2)
                {
                    ratingMessage += $"\n📊3 \ud83d\ude03 _{list[2]}_ *[{userDesc[2].FullName}](https://t.me/{userDesc[2].UserName})* \\- \\{userDesc[2].Reputation} репутации";
                }

                await bot.SendTextMessageAsync(chatId, ratingMessage, parseMode: ParseMode.MarkdownV2, cancellationToken: cancellationToken, linkPreviewOptions: true);

            }
            else
            {
                await bot.SendTextMessageAsync(chatId, "Список пользователей пуст!", cancellationToken: cancellationToken);
            }
        }
    }

    private static Task CreateTelegramUser(long userId, string firstname)
    {
        if (_users.All(x => x.Id != userId))
        {
            TelegramUser targetUser;
            targetUser = new TelegramUser(userId, 0, firstname);
            _users.Add(targetUser);
        }
        return Task.CompletedTask;
    }
    private static Task CreateTelegramUsers(List<TelegramUser> users)
    {
        foreach (var user in users)
        {
            if (_users.All(x => x.Id != user.Id))
            {
                _users.Add(user);
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
                _users = JsonConvert.DeserializeObject<List<TelegramUser>>(json) ?? new List<TelegramUser>();
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
        var updatedJson = JsonConvert.SerializeObject(_users, Formatting.Indented);
        File.WriteAllText(ReputationFilePath, updatedJson);
        Console.WriteLine("Изменения сохранены в файл.");
    }
}
