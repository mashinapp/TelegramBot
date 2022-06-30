using System.Collections.Generic;
using System.Net.Sockets;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Client;
using TelegramBot.Model;

namespace TelegramBot
{
    struct BotUpdate
    {
        public string text;
        public long id;
        public string? username;
    }
    class Program
    {
        static TelegramBotClient Bot = new TelegramBotClient("5198618460:AAE8UiVeYm3wSM4OuM3JPkbVJb0C69rWKtA");


        private static List<string> jobsList = new() { "barista", "software engineer" };
        private static Dictionary<long, int> position = new();
        private static Dictionary<long, int> enteringData = new();
        private static Dictionary<long, int> vacancyCount = new();
        private static Dictionary<long, int> enterType = new();
        private static BotClient _jobClient = new();
        private static JobsResult newJob = new();
        static void Main(string[] args)
        {
            //Read all saved updates

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.EditedMessage,
                    UpdateType.CallbackQuery
                }
            };

            Bot.StartReceiving(UpdateHandler, HandleErrorAsync, receiverOptions);

            Console.ReadLine();
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private static async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken arg3)
        {
            long id;
            if (update.Type == UpdateType.CallbackQuery)
            {
                id = update.CallbackQuery.Message.Chat.Id;
            }
            else
            {
                id = update.Message.Chat.Id;
            }

            if (!position.ContainsKey(id))
            {
                position.Add(id, 0);
                enteringData.Add(id, 0);
                vacancyCount.Add(id, 0);
                enterType.Add(id, 0);
            }

            if (enteringData[id] != 0)
            {
                switch (enteringData[id])
                {
                    case 1:
                        if (update.Type == UpdateType.Message)
                        {
                            newJob.title = update.Message.Text!;
                            await bot.SendTextMessageAsync(id,
                                $"Введіть назву компанії"
                            );
                            enteringData[id]++;
                        }
                        return;

                    case 2:
                        if (update.Type == UpdateType.Message)
                        {
                            newJob.company_name = update.Message.Text!;
                            await bot.SendTextMessageAsync(id,
                                $"Введіть локацію"
                            );
                            enteringData[id]++;
                        }
                        return;


                    case 3:
                        if (update.Type == UpdateType.Message)
                        {
                            newJob.location = update.Message.Text!;
                            await bot.SendTextMessageAsync(id,
                                $"Введіть від кого"
                            );
                            enteringData[id]++;
                        }
                        return;


                    case 4:
                        if (update.Type == UpdateType.Message)
                        {
                            newJob.via = update.Message.Text!;
                            await bot.SendTextMessageAsync(id,
                                $"Введіть опис"
                            );
                            enteringData[id]++;
                        }
                        return;

                    case 5:
                        if (update.Type == UpdateType.Message)
                        {
                            newJob.description = update.Message.Text!;
                            if (enterType[id] == 0)
                            {
                                position[id] = vacancyCount[id];
                                vacancyCount[id] = _jobClient.PostJob(newJob, id).Result.jobs_results.Count;
                            }
                            else
                            {
                                await _jobClient.PostEditJob(position[id], newJob, id);
                            }
                            var vacancy = _jobClient.GetJobByPosition(position[id], id).Result;
                            await ShowVacancy(id, vacancy);
                            await ChooseVacancy(id, $"Вакансія номер {position[id] + 1}");
                            newJob = new JobsResult();
                            enteringData[id] = 0;
                        }
                        return;

                }
            }

            if (update.Type == UpdateType.CallbackQuery)
            {
                CallbackQuery callbackQuery = update.CallbackQuery;

                if (jobsList.Contains(callbackQuery.Data))
                {
                    vacancyCount[id] = _jobClient.GetJobByName(callbackQuery.Data, id).Result!.jobs_results.Count;
                    if (vacancyCount[id] != 0)
                    {
                        position[id] = 0;
                        await bot.SendTextMessageAsync(id,
                        $"Запит виконано успішно, знайдено {vacancyCount[id]} вакансій"
                        );
                        var vacancy = _jobClient.GetJobByPosition(position[id], id).Result;
                        await ShowVacancy(id, vacancy);
                        await ChooseVacancy(id, $"Вакансія номер {position[id] + 1}");
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(id,
                            "За данним запитом відсутні вакансії"
                        );
                        await ChooseProfession(id);
                    }
                }
                else
                {
                    JobsResult? vacancy;
                    switch (callbackQuery.Data)
                    {
                        case "next":
                            if (vacancyCount[id] > 1)
                            {
                                position[id]++;
                                if (position[id] >= vacancyCount[id]) position[id] = 0;
                                vacancy = _jobClient.GetJobByPosition(position[id], id).Result;
                                await ShowVacancy(id, vacancy);
                                await ChooseVacancy(id, $"Вакансія номер {position[id] + 1}");
                            }
                            break;

                        case "prev":
                            if (vacancyCount[id] > 1)
                            {
                                position[id]--;
                                if (position[id] < 0) position[id] = vacancyCount[id] - 1;
                                vacancy = _jobClient.GetJobByPosition(position[id], id).Result;
                                await ShowVacancy(id, vacancy);
                                await ChooseVacancy(id, $"Вакансія номер {position[id] + 1}");
                            }
                            break;

                        case "details":
                            vacancy = _jobClient.GetJobByPosition(position[id], id).Result;
                            await bot.SendTextMessageAsync(id,
                                $"{vacancy.description}"
                            );
                            await ChooseVacancy(id, $"Вакансія номер {position[id] + 1}");
                            break;

                        case "back":
                            vacancyCount[id] = 0;
                            position[id] = 0;
                            await ChooseProfession(id);
                            break;


                        case "add":
                            await bot.SendTextMessageAsync(id,
                                $"Введіть назву вакансії"
                            );
                            enterType[id] = 0;
                            enteringData[id] = 1;
                            break;

                        case "edit":
                            await bot.SendTextMessageAsync(id,
                                $"Введіть назву вакансії"
                            );
                            enterType[id] = 1;
                            enteringData[id] = 1;
                            break;

                        case "del":
                            if (vacancyCount[id] > 1)
                            {
                                await _jobClient.DeleteJob(position[id], id);
                                vacancyCount[id]--;
                                if (position[id] >= vacancyCount[id])
                                {
                                    position[id] = vacancyCount[id] - 1;
                                }
                                vacancy = _jobClient.GetJobByPosition(position[id], id).Result;
                                await ShowVacancy(id, vacancy);
                                await ChooseVacancy(id, $"Вакансія номер {position[id] + 1}");
                            }
                            break;
                    }
                }
            }

            if (update.Type == UpdateType.Message)
            {
                if (update.Message.Type == MessageType.Text)
                {

                    //write an update
                    var _botUpdate = new BotUpdate
                    {
                        text = update.Message.Text,
                        id = update.Message.Chat.Id,
                        username = update.Message.Chat.Username
                    };

                    if (_botUpdate.text == "/start")
                    {
                        vacancyCount[id] = 0;
                        position[id] = 0;
                        await ChooseProfession(_botUpdate.id);
                        return;
                    }

                }
            }
            async Task ChooseProfession(long id)
            {
                InlineKeyboardMarkup inlineKeyboardMarkup = new
                (
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Вакансії бариста", $"barista"),
                            InlineKeyboardButton.WithCallbackData("Вакансії програміста", $"software engineer")
                        }
                    }
                );
                await bot.SendTextMessageAsync(id, "Оберіть вакансію", replyMarkup: inlineKeyboardMarkup);
            }

            async Task ChooseVacancy(long id, string text)
            {
                InlineKeyboardMarkup inlineKeyboardMarkup = new
                (
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Попередня", $"prev"),
                            InlineKeyboardButton.WithCallbackData("Детальніше", $"details"),
                            InlineKeyboardButton.WithCallbackData("Наступна", $"next")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Додати вакансію", $"add"),
                            InlineKeyboardButton.WithCallbackData("Редагувати вакансію", $"edit"),
                            InlineKeyboardButton.WithCallbackData("Прибрати вакансію", $"del")
                        },
                        new[]
                        {
                        InlineKeyboardButton.WithCallbackData("Повернутися назад", $"back")
                        }
                    }
                );
                await bot.SendTextMessageAsync(id, $"{text}", replyMarkup: inlineKeyboardMarkup);
            }

            async Task ShowVacancy(long id, JobsResult vacancy)
            {
                await bot.SendTextMessageAsync(id,
                    $"Назва вакансії: {vacancy.title}\n" +
                    $"Назва компанії: {vacancy.company_name}\n" +
                    $"Локація: {vacancy.location}\n" +
                    $"Від: {vacancy.via}\n"
                );
            }
        }
    }
}
