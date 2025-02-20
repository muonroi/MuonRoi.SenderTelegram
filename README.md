# TelegramSender .NET Extension

TelegramSender is an open-source .NET extension that provides services for sending text messages, documents, photos, videos, and other media types to Telegram via a Telegram Bot Client. It supports HTML formatting, automatic message splitting, media groups, message editing, retry logic, scheduled messaging, dependency injection, and callback handling.


[![Build Status](https://img.shields.io/github/actions/workflow/status/muonroi/MuonRoi.SenderTelegram/dotnet.yml?branch=master)](https://github.com/muonroi/MuonRoi.SenderTelegram/actions)
[![NuGet](https://img.shields.io/nuget/v/MuonRoi.SenderTelegram)](https://www.nuget.org/packages/MuonRoi.SenderTelegram)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/muonroi/MuonRoi.SenderTelegram/blob/master/LICENSE.txt)

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
- **Formats: Message**templates for different formatting (default, error).
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

## Callback Handling with Polling and Webhook

TelegramSender supports dynamic callback handling using custom callback handlers. You can register these callbacks during DI registration. Below are detailed examples for both polling and webhook methods.

1. **Registering a Custom Callback Handler**

When configuring the TelegramSender extension in your DI container, you can register a callback handler that processes any callback data that starts with a specific prefix (for example, "view_customer_"):

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

The callback handler above processes any callback query whose data begins with "view_customer_" and retrieves customer details accordingly.

2. **Using Callback Handling with Polling**

When using polling to receive updates, you must call StartReceiving. In this scenario, any received callback query will be handled by the registered callback handler.

**Example in Program.cs using polling**

```cshrap
builder.Services.AddSingleton<ITelegramBotClientWrapper, TelegramBotClientWrapper>();
builder.Services.AddSingleton<IRetryPolicyProvider, DefaultRetryPolicyProvider>();
builder.Services.AddSingleton<IMessageSplitter, PlainTextMessageSplitter>();
builder.Services.AddSingleton<IHtmlMessageProcessor, HtmlMessageProcessor>();


builder.Services.AddTelegramSender(builder.Configuration, telegramSender =>
{
    telegramSender.RegisterCallbackHandlers("view_customer_", async callbackQuery =>
    {
        Console.WriteLine("Callback received: " + callbackQuery.Data);
        // Custom logic to handle the callback goes here.
        await Task.CompletedTask;
    });
});


builder.Services.AddSingleton<TelegramUpdateHandler>();

----start app----

var botClientWrapper = app.Services.GetRequiredService<ITelegramBotClientWrapper>();

// Create ReceiverOptions (customize as needed)
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = { } // receive all update types
};

// Create a cancellation token source.
var cancellationTokenSource = new CancellationTokenSource();

// Start polling to receive updates.
botClientWrapper.StartReceiving(
    updateHandler: async (bot, update, token) =>
    {
        // Retrieve the TelegramUpdateHandler from DI.
        var updateHandler = app.Services.GetRequiredService<TelegramUpdateHandler>();
        await updateHandler.HandleUpdateAsync(update);
    },
    pollingErrorHandler: async (bot, exception, token) =>
    {
        // Handle errors (for example, log them).
        Console.WriteLine($"Error: {exception.Message}");
        await Task.CompletedTask;
    },
    receiverOptions: receiverOptions,
    cancellationToken: cancellationTokenSource.Token
);

app.Lifetime.ApplicationStopping.Register(() =>
{
    // Cancel polling gracefully when the application stops.
    cancellationTokenSource.Cancel();
});

```

In this polling example, after starting StartReceiving, any callback query (as well as standard messages) is passed to the TelegramUpdateHandler, which then calls the registered callback handler if the callback data matches.


3. **Using Callback Handling with Webhook**

When using webhooks, Telegram sends update payloads directly to your designated HTTPS endpoint. The registered callback handler works the same way once the update is received.

**Example Webhook Controller**

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Telegram.Bot.Types;

[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly TelegramUpdateHandler _updateHandler;

    public TelegramWebhookController(TelegramUpdateHandler updateHandler)
    {
        _updateHandler = updateHandler;
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] Update update)
    {
        if (update == null)
        {
            return BadRequest();
        }

        await _updateHandler.HandleUpdateAsync(update);
        return Ok();
    }
}

```

**Setting Up Webhook in Program.cs**

In this case, you don't need to call StartReceiving. Instead, ensure that your webhook is properly configured so that Telegram pushes updates to your endpoint.

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Telegram.Bot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Retrieve configuration and set the webhook on startup.
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        string botToken = configuration["Telegram:BotToken"];
        string webhookUrl = "https://yourdomain.com/api/telegram/update";

        var webhookConfigurator = new WebhookConfigurator(webhookUrl);
        await webhookConfigurator.ConfigureWebhookAsync();

        // Run the host to start listening for webhook requests.
        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Register required dependencies for TelegramSender.
                services.AddSingleton<ITelegramBotClientWrapper, TelegramBotClientWrapper>();
                services.AddSingleton<IRetryPolicyProvider, DefaultRetryPolicyProvider>();
                services.AddSingleton<IMessageSplitter, PlainTextMessageSplitter>();
                services.AddSingleton<IHtmlMessageProcessor, HtmlMessageProcessor>();

                // Register TelegramSender with callback webhook.
                services.AddTelegramSender(context.Configuration, telegramSender =>
                {
                    telegramSender.RegisterCallbackHandlers("view_customer_", async callbackQuery =>
                    {
                        Console.WriteLine("Callback received via webhook: " + callbackQuery.Data);
                        await Task.CompletedTask;
                    });
                });

                // Register the update handler for processing messages and callbacks.
                services.AddSingleton<TelegramUpdateHandler>();

                // Add controllers to support webhook endpoints.
                services.AddControllers();
            });
}

```

**Config Webhook Configurator**

```csharp
using Telegram.Bot;

public class WebhookConfigurator
{
    private readonly string _webhookUrl;
    private readonly ITelegramBotClientWrapper _botClient
    public WebhookConfigurator(string botToken, string webhookUrl, ITelegramBotClientWrapper botClient)
    {
        _webhookUrl = webhookUrl;
        _botClient = botClient;
    }

    public async Task ConfigureWebhookAsync()
    {
        await _botClient.SetWebhookAsync(_webhookUrl);
    }
}

```

**In the webhook setup**

    - WebhookConfigurator is used to call SetWebhookAsync and configure Telegram to send updates to your endpoint.
    - No polling method (such as StartReceiving) is needed because Telegram pushes the updates.
    - The registered callback handler will still be invoked via the TelegramUpdateHandler once an update is received at the webhook endpoint.

## Contributing

Contributions are welcome! Follow these steps:

    1. Fork the Repository: Create your own fork of the project on GitHub.
    2. Create a New Branch: Make your changes in a separate branch.
    3. Submit a Pull Request: Open a pull request with a clear description of your changes.
    4. Follow Coding Guidelines: Ensure your changes adhere to the project's coding standards and include unit tests where applicable.

If you encounter any issues or have suggestions for improvements, please open an issue on the GitHub repository.

## Author

Developed and maintained by MuonRoi.
For more information, visit the [MuonRoi GitHub page](https://github.com/muonroi) or contact the maintainer through the repository's issues/discussions.

## Contact

For questions, support, or further discussion, please open an issue on the GitHub repository or use the repository's discussion board.

## License

This project is licensed under the [MIT License](https://github.com/muonroi/MuonRoi.SenderTelegram/blob/master/LICENSE.txt). See the LICENSE file for details.
