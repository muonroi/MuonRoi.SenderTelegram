# TelegramSender .NET Extension

TelegramSender is an open-source .NET extension that provides services for sending text messages, documents, photos, videos, and other media types to Telegram via a Telegram Bot Client. It supports HTML formatting, automatic message splitting, media groups, message editing, retry logic, scheduled messaging, dependency injection, and callback handling.


[![Build Status](https://img.shields.io/github/actions/workflow/status/muonroi/MuonRoi.SenderTelegram/dotnet.yml?branch=master)](https://github.com/muonroi/MuonRoi.SenderTelegram/actions)
[![NuGet](https://img.shields.io/nuget/v/MuonRoi.SenderTelegram)](https://www.nuget.org/packages/MuonRoi.SenderTelegram)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/muonroi/MuonRoi.SenderTelegram/blob/master/LICENSE.txt)

---

## Table of Contents

1. [Features](#features)
2. [Installation (Requires .NET 6+)](#installation)
3. [Setting Up Your Telegram Bot](#setting-up-your-telegram-bot)
4. [Configuration](#configuration)
5. [Usage](#usage)
   - [Registering the Service](#registering-the-service)
   - [Sending Messages and Media](#sending-messages-and-media)
     - [Send a Text Message](#send-a-text-message)
     - [Send an Image](#send-an-image)
     - [Send a Video](#send-a-video)
   - [Message Editing and Scheduled Messaging](#message-editing-and-scheduled-messaging)
   - [Callback Handling](#callback-handling)
6. [Handling Telegram Updates](#handling-telegram-updates)
   - [Polling](#using-polling-to-receive-updates)
   - [Webhook](#using-webhook-to-receive-updates)
7. [Troubleshooting (Common Issues)](#troubleshooting-common-issues)
8. [Contributing](#contributing)
9. [Author](#author)
10. [Contact](#contact)
11. [License](#license)


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

## Setting Up Your Telegram Bot

Before using this library, you need to create a **Telegram Bot** and obtain its API token.

### **Step 1: Register a New Bot**
1. Open Telegram and search for `@BotFather`.
2. Start a chat and send the command: /newbot
3. Follow the instructions and choose:
- A **name** for your bot (e.g., `MyNotificationBot`).
- A **username** (must end with `bot`, e.g., `MyNotifBot`).

4. After setup, **BotFather** will provide you a token like: 123456789:ABCdefGhIJKlmnopQRstUVWXyz

**Save this token**, as it is required for configuration.

---

### **Step 2: Get the Chat ID of Your Channel/Group**
If you want to send messages to a **group or channel**, follow these steps:

#### ✅ **Option 1: Using [IDBot](https://t.me/myidbot)**
1. Open Telegram and search for `@myidbot`.
2. Start a chat and send: /getid 
3. The bot will reply with your Chat ID.

#### ✅ **Option 2: Using API**
You can get the Chat ID using Telegram API:
```bash
curl -X GET "https://api.telegram.org/bot<your_bot_token>/getUpdates"
```

Find the "chat":{"id": value in the response.

Now, you are ready to configure the bot in your application.


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

## **Sending Messages and Media**

Inject `ITelegramSender` into your classes (e.g., services or controllers) to send messages:

### **Send a Simple Text Message**
```csharp
public async Task SendTextMessageAsync()
{
    bool success = await _telegramSender.SendMessageAsync("Hello, this is a test message!");
    if (!success)
    {
        Console.WriteLine("Failed to send message.");
    }
}

```

## Send an Image

```csharp
public async Task SendPhotoAsync()
{
    string photoUrl = "https://example.com/sample-image.jpg";
    await _telegramSender.SendPhotoAsync(photoUrl, "Here is an image!");
}

```

## Send a Video

```csharp
public async Task SendVideoAsync()
{
    string videoUrl = "https://example.com/sample-video.mp4";
    await _telegramSender.SendVideoAsync(videoUrl, "Check out this video!");
}
```

For sending media (documents, photos, videos) or message groups (albums), refer to the package documentation for the appropriate methods and parameters.

**Message Editing and Scheduled Messaging**

- **Message Editing**: Use the provided method to edit a previously sent message.
- **Scheduled Messaging**: Use the scheduling feature to delay message delivery based on your application's needs.

(For specific code samples on editing or scheduling, check the detailed API documentation.)

## Handling Telegram Updates (Polling vs. Webhook)

When working with Telegram bots, you have **two ways** to receive updates:

1. **Polling (Long Polling)** – The bot keeps checking Telegram for updates.
   - ✅ **Easy to set up** (good for local testing).
   - ❌ **Consumes more server resources**.
   - ❌ May miss updates if the server is down.

2. **Webhook** – Telegram **pushes updates** to your server.
   - ✅ **Efficient**, only sends data when needed.
   - ✅ **More reliable** in production.
   - ❌ Requires **public HTTPS server**.

### **Which One Should You Use?**
- **Use Polling** for local development or small bots.  
- **Use Webhook** for production applications.  

## Using Polling to Receive Updates

If you do not have a public HTTPS server, you can use **Polling**.

### **Step 1: Register the Required Services**

In `Program.cs`, register the services:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Telegram services
builder.Services.AddTelegramSender(builder.Configuration);

// Register the Update Handler
builder.Services.AddSingleton<TelegramUpdateHandler>();

var app = builder.Build();
```

### **Step 2: Start Polling**

In Program.cs, before app.Run();, add:

```csharp   
var botClientWrapper = app.Services.GetRequiredService<ITelegramBotClientWrapper>();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
};

// Start polling
var cancellationTokenSource = new CancellationTokenSource();

botClientWrapper.StartReceiving(
    async (botClient, update, cancellationToken) =>
    {
        var updateHandler = app.Services.GetRequiredService<TelegramUpdateHandler>();
        await updateHandler.HandleUpdateAsync(update);
    },
    async (botClient, exception, cancellationToken) =>
    {
        Console.WriteLine($"Polling error: {exception.Message}");
    },
    receiverOptions,
    cancellationTokenSource.Token
);

app.Lifetime.ApplicationStopping.Register(() =>
{
    cancellationTokenSource.Cancel();
});

app.Run();

```

### **Step 3: Verify Polling is Working**

Run your application and check if logs appear:

```bash 
dotnet run
```

Expected output:

```csharp 

[INFO] Bot started polling for updates...
[INFO] Received message from user: "Hello Bot!"

```


## Using Webhook to Receive Updates

Webhook is recommended for production as it allows Telegram to **push updates** to your server, reducing unnecessary requests.

### **Step 1: Set up Webhook in Telegram**
Run this command in your terminal to set up your webhook:
```bash
curl -X POST "https://api.telegram.org/bot<your_bot_token>/setWebhook?url=https://yourdomain.com/api/telegram/update"

```

### **Step 2: Register Webhook in .NET**
In Program.cs, register the necessary services:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register Telegram services
builder.Services.AddTelegramSender(builder.Configuration);
builder.Services.AddSingleton<TelegramUpdateHandler>();

// Add controllers to handle webhook updates
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();


```

### **Step 3: Create Webhook Controller**

Create a new file TelegramWebhookController.cs:

```csharp
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> ReceiveUpdate([FromBody] Update update)
    {
        if (update == null) return BadRequest();
        
        await _updateHandler.HandleUpdateAsync(update);
        return Ok();
    }
}

```

### **Step 4: Verify Webhook is Working**

Run your app and test webhook setup:

```bash
curl -X GET "https://api.telegram.org/bot<your_bot_token>/getWebhookInfo"
```

Expected output:

```json
{
  "ok": true,
  "result": {
    "url": "https://yourdomain.com/api/telegram/update",
    "pending_update_count": 0
  }
}
```

## Polling vs. Webhook: Which One to Use?

| Feature            | Polling                               | Webhook                                |
|--------------------|--------------------------------------|----------------------------------------|
| **Setup Complexity**  | Easy, works locally                 | Requires HTTPS and public server      |
| **Resource Usage** | High (constant API calls)          | Low (event-driven updates)            |
| **Reliability**    | Can miss updates if not handled properly | More reliable if webhook is set up correctly |
| **Best For**      | Local testing, small bots           | Production, high-scale applications   |

**Recommendation**: Use **Polling** for testing/development and **Webhook** for production.


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
