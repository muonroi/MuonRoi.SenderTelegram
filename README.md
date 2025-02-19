# TelegramSender .NET Extension

TelegramSender is an open-source .NET extension that provides services for sending text messages, documents, photos, videos, and other media types to Telegram via a Telegram Bot Client. It supports HTML formatting, automatic message splitting, media groups, message editing, retry logic, scheduled messaging, dependency injection, and callback handling.

---

## Table of Contents

1. [Features](#features)
2. [Installation](#installation)
3. [Configuration](#configuration)
4. [Usage](#usage)
   - [Registering the Service](#registering-the-service)
   - [Sending Messages and Media](#sending-messages-and-media)
   - [Message Editing and Scheduled Messaging](#message-editing-and-scheduled-messaging)
   - [Callback Handling](#callback-handling)
5. [Handling Telegram Updates](#handling-telegram-updates)
6. [Contributing](#contributing)
7. [Author](#author)
8. [Contact](#contact)
9. [License](#license)

---

## Features

- **Text Message Sending**
  - Supports HTML formatting.
  - Automatically splits messages exceeding the maximum length.
  - Customizable message formatting templates.
  
- **Media Sending**
  - Send documents, photos, videos.
  - Supports sending media groups (albums).

- **Message Editing**
  - Allows editing of previously sent messages.

- **Retry Logic**
  - Robust retry mechanism for transient failures (network issues, API errors).

- **Scheduled Messaging**
  - Schedule messages to be sent after a specified delay.

- **Dependency Injection**
  - Easily integrates with .NET’s DI container using the `AddTelegramSender` extension.

- **Callback Handling**
  - Supports dynamic button click handling with custom callback handlers.

---

## Installation

Install the package via the NuGet Package Manager:

```bash
dotnet add package MuonRoi.SenderTelegram
```

## Configuration

TelegramSender uses a Telegram section in your appsettings.json for configuration. For example:

```json
{
  "Telegram": {
    "BotToken": "your_bot_token",
    "ChannelId": "your_default_channel_id",
    "ErrorChannelId": "your_error_channel_id",
    "Formats": {
      "default": "{0}",
      "error": "[ERROR] {0}"
    },
    "MaxMessageLength": 4096,
    "MaxRetryAttempts": 3
  }
}
```

- **BotToken**: Telegram Bot API token.
- **ChannelId**: Default channel for messages.
- **ErrorChannelId**: Channel for error notifications.
- **Formats: Message** templates for different formatting (default, error).
- **MaxMessageLength**: Maximum allowed message length.
- **MaxRetryAttempts**: Maximum number of retry attempts when sending messages.

## Usage

**Registering the Service**

Register the required dependencies and the TelegramSender extension in your DI container (e.g., in Startup.cs or Program.cs):

```csharp
using MuonRoi.SenderTelegram;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public static IServiceCollection RegisterService(this IServiceCollection services, IConfiguration configuration)
    {
        // Register required dependencies
        services.AddSingleton<ITelegramBotClientWrapper, TelegramBotClientWrapper>();
        services.AddSingleton<IRetryPolicyProvider, DefaultRetryPolicyProvider>();
        services.AddSingleton<IMessageSplitter, PlainTextMessageSplitter>();
        services.AddSingleton<IHtmlMessageProcessor, HtmlMessageProcessor>();

        // Register the TelegramSender extension
        services.AddTelegramSender(configuration);
        return services;
    }
}
```

**Sending Messages and Media**

Inject ITelegramSender into your classes (e.g., services or controllers) to send messages:

```csharp
public class NotificationService
{
    private readonly ITelegramSender _telegramSender;

    public NotificationService(ITelegramSender telegramSender)
    {
        _telegramSender = telegramSender;
    }

    public async Task SendAlertAsync(string message)
    {
        bool success = await _telegramSender.SendMessageAsync(message);
        if (!success)
        {
            // Handle failure (e.g., log the error or retry)
        }
    }
}

```

For sending media (documents, photos, videos) or message groups (albums), refer to the package documentation for the appropriate methods and parameters.

**Message Editing and Scheduled Messaging**

- **Message Editing**: Use the provided method to edit a previously sent message.
- **Scheduled Messaging**: Use the scheduling feature to delay message delivery based on your application's needs.
(For specific code samples on editing or scheduling, check the detailed API documentation.)

## Callback Handling

TelegramSender supports callback queries for interactive buttons.

** Register a Custom Callback Handler **

Register a custom callback handler in your DI container:

```csharp
builder.Services.AddTelegramSender(builder.Configuration, telegramSender =>
{
    telegramSender.RegisterCallbackHandlers("view_customer_", async callbackQuery =>
    {
        string customerIdStr = callbackQuery.Data.Replace("view_customer_", "");

        if (Guid.TryParse(customerIdStr, out Guid customerId))
        {
            using var httpClient = new HttpClient();
            string apiUrl = $"https://your-api.com/api/customer/{customerId}";
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var customer = JsonConvert.DeserializeObject<CustomerResponseModel>(await response.Content.ReadAsStringAsync());
                string message = $"📋 *Customer Info:* {customer.CustomerName}, {customer.Phone}";

                await telegramSender.SendMessageAsync(message, callbackQuery.Message.Chat.Id.ToString());
            }
        }
    });

    Console.WriteLine("✅ Telegram Callback Handlers Registered");
});

```

** Sending Messages with Callback Button **

When sending messages, include buttons with callback data:

```csharp
public async Task SendCustomerInfoAsync(Customer customer)
{
    string message = $"📋 *Customer Info:* {customer.CustomerName}, {customer.Phone}";
    var buttons = new[]
    {
        new InlineKeyboardButton("View Details", $"view_customer_{customer.Id}")
    };
    await _telegramSender.SendMessageAsync(message, buttons);
}
```

** Handling Telegram Updates **

Create a service to process Telegram updates, including callback queries and messages.

** TelegramUpdateHandler.cs **
```csharp
using MuonRoi.SenderTelegram.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Telegram.Bot.Types;

public class TelegramUpdateHandler
{
    private readonly ITelegramSender _telegramSender;
    private readonly ILogger<TelegramUpdateHandler> _logger;

    public TelegramUpdateHandler(ITelegramSender telegramSender, ILogger<TelegramUpdateHandler> logger)
    {
        _telegramSender = telegramSender;
        _logger = logger;
    }

    /// <summary>
    /// Handles incoming Telegram updates.
    /// </summary>
    public async Task HandleUpdateAsync(Update update)
    {
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            _logger.LogInformation($"Processing callback query: {update.CallbackQuery.Data}");
            await _telegramSender.HandleCallbackQueryAsync(update.CallbackQuery);
        }
        else if (update.Type == UpdateType.Message && update.Message != null)
        {
            _logger.LogInformation($"Received message: {update.Message.Text}");
            await HandleMessageAsync(update.Message);
        }
    }

    /// <summary>
    /// Handles incoming text messages.
    /// </summary>
    private async Task HandleMessageAsync(Message message)
    {
        if (message.Text is null) return;

        string responseMessage = $"You said: {message.Text}";
        await _telegramSender.SendMessageAsync(responseMessage, message.Chat.Id.ToString());
    }
}

```

** Registering the Update Handler **

Register the TelegramUpdateHandler in your DI container:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<TelegramUpdateHandler>();
    }
}
```

** Processing Incoming Updates **

In your webhook or polling loop, process the updates as follows:

```csharp
public class TelegramBot
{
    private readonly TelegramUpdateHandler _updateHandler;

    public TelegramBot(TelegramUpdateHandler updateHandler)
    {
        _updateHandler = updateHandler;
    }

    public async Task ProcessUpdatesAsync(Update[] updates)
    {
        foreach (var update in updates)
        {
            await _updateHandler.HandleUpdateAsync(update);
        }
    }
}
```

** Setting the Telegram Webhook **

Run this command to set your bot’s webhook:

```curl
curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook?url=https://yourdomain.com/api/telegram/update"
```

## Contributing

Contributions are welcome! Follow these steps:

1. **Fork the Repository**: Create your own fork of the project on GitHub.
2. **Create a New Branch**: Make your changes in a separate branch.
3. **Submit a Pull Request**: Open a pull request with a clear description of your changes.
4. **Follow Coding Guidelines**: Ensure your changes adhere to the project's coding standards and include unit tests where applicable.

If you encounter any issues or have suggestions for improvements, please open an issue on the GitHub repository.

## Author

Developed and maintained by MuonRoi.
For more information, visit the [MuonRoi GitHub page](https://github.com/muonroi) or contact the maintainer through the repository's issues/discussions.

## Contact

For questions, support, or further discussion, please open an issue on the GitHub repository or use the repository's discussion board.

## License

This project is licensed under the [MIT License](https://github.com/muonroi/MuonRoi.SenderTelegram/blob/master/LICENSE.txt). See the LICENSE file for details.
