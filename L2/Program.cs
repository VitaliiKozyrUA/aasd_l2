using Newtonsoft.Json.Linq;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

Console.Write("Username: ");
var apiUsername = Console.ReadLine();

Console.Write("Password: ");
string apiPassword = null;
while (true)
{
    var key = System.Console.ReadKey(true);
    if (key.Key == ConsoleKey.Enter)
        break;
    apiPassword += key.KeyChar;
}
Console.WriteLine();

var apiClient = new ApiClient();
var isAuthenticated = await apiClient.Authenticate(apiUsername, apiPassword);
if(!isAuthenticated)
{
    Console.WriteLine("Wrong username or password.");
    return;
}
Console.WriteLine("Successfully authenticated to the API.");

var botClient = new TelegramBotClient("7120845339:AAFOYNlQMgvFkUKD1gc5MlGk8SOIzHQ7T4I");

using CancellationTokenSource cts = new();

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    cancellationToken: cts.Token
);

Console.ReadLine();

cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;

    if (update.Message.Type == MessageType.Photo)
    {
        var photoFileId = update.Message.Photo!.First().FileId;

        await botClient.SendPhotoAsync(
            chatId: update.Message.Chat.Id,
            photo: InputFile.FromFileId(photoFileId),
            caption: "Here's your image!"
        );
    }

    if (message.Text is not { } messageText)
    {
        return;
    }

    if (IsWeekday(messageText))
    {
        var schedule = await apiClient.GetSchedule(100, GetDayNumber(messageText), 35);
        var chatId = message.Chat.Id;

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: FormatJsonSchedule(schedule),
            cancellationToken: cancellationToken);


        string imageUrl = $"https://picsum.photos/id/{GetDayNumber(messageText)}/200/300";
        using var httpClient = new HttpClient();
        var imageStream = await httpClient.GetStreamAsync(imageUrl);

        await botClient.SendPhotoAsync(chatId, InputFile.FromStream(imageStream), cancellationToken: cancellationToken);
    }
    else
    {
       await SendWeekdayButtons(botClient, message.Chat.Id, cancellationToken);
    }
}

string FormatJsonSchedule(string json)
{
    JArray array = JArray.Parse(json);
    string formatted = "";

    foreach (var item in array)
    {
        var id = item["id"];
        var group = item["group"];
        var groupNumber = group["number"];
        var classroom = item["classroom"];
        var classroomName = classroom["name"];
        var teacher = item["teacher"];
        var teacherFirstName = teacher["firstName"];
        var teacherLastName = teacher["lastName"];
        var subject = item["subject"];
        var subjectName = subject["name"];
        var lessonNumber = item["lessonNumber"];

        formatted += $"{lessonNumber}. {subjectName} ➡️ {teacherFirstName} {teacherLastName} ➡️ {classroomName} ауд.\n";
    }

    return formatted;
}

bool IsWeekday(string text)
{
    string[] weekdays = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
    return weekdays.Contains(text);
}

int GetDayNumber(string weekday)
{
    string[] weekdays = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
    return Array.IndexOf(weekdays, weekday) + 1;
}

async Task SendWeekdayButtons(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
{
    var buttons = new[]
    {
        new[] { new KeyboardButton("Monday"), new KeyboardButton("Tuesday") },
        new[] { new KeyboardButton("Wednesday"), new KeyboardButton("Thursday") },
        new[] { new KeyboardButton("Friday") }
    };

    var replyMarkup = new ReplyKeyboardMarkup(buttons);

    await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: "Select a day to get the schedule:",
        replyMarkup: replyMarkup,
        cancellationToken: cancellationToken);
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

