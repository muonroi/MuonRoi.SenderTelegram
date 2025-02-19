# TelegramSender .NET Extension

TelegramSender is an open-source .NET extension that provides services for sending messages, documents, photos, videos, and other media types to Telegram via a Telegram Bot Client. It is designed to support text messages (including HTML formatting), message splitting when exceeding character limits, media sending, message editing, retry logic, and scheduling message delivery.

## Features

- **Text Message Sending**
  - Supports HTML formatting.
  - Automatically splits messages exceeding the configured maximum length.
  - Applies customizable message formatting templates.

- **Media Sending**
  - Supports sending documents, photos, and videos.
  - Enables sending media groups (albums) with multiple media items.

- **Message Editing**
  - Allows editing of previously sent messages.

- **Retry Logic**
  - Integrates with a robust retry mechanism to handle transient failures such as network issues or API errors.

- **Scheduled Messaging**
  - Supports scheduling messages to be sent after a specified delay.

- **Dependency Injection**
  - Easily integrates with .NET’s DI container using the provided `AddTelegramSender` extension method.

## Installation

### NuGet Package

Install the package via NuGet Package Manager:

dotnet add package MuonRoi.SenderTelegram

# Configuration

TelegramSender uses a Telegram section in your appsettings.json for configuration. Below is an example configuration:

```bash
{
  "Telegram": {
    "ChannelId": "your_default_channel_id",
    "ErrorChannelId": "your_error_channel_id",
    "Formats": {
      "default": "{0}",
      "error": "[ERROR] {0}"
    },
    "MaxMessageLength": 4096
  }
}
```

- **ChannelId**: The default channel for sending messages.
- **ErrorChannelId**: The channel for sending error notifications.
- **Formats**: Message templates for different formatting types (e.g., default, error).
- **MaxMessageLength**: The maximum allowed message length. Messages longer than this will be automatically split.

# Usage
## Registering the Service
Register the TelegramSender service and its required dependencies in your DI container (e.g., in Startup.cs or Program.cs):

```bash
using MuonRoi.SenderTelegram;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register required dependencies
        services.AddSingleton<ITelegramBotClientWrapper, TelegramBotClientWrapper>();
        services.AddSingleton<IRetryPolicyProvider, RetryPolicyProvider>();
        services.AddSingleton<IMessageSplitter, MessageSplitter>();
        services.AddSingleton<IHtmlMessageProcessor, HtmlMessageProcessor>();

        // Register the TelegramSender extension
        services.AddTelegramSender(Configuration);
    }
}
```

## Injecting and Using ITelegramSender
You can inject ITelegramSender into your classes (such as services or controllers) to send messages to Telegram:

```bash
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

## Dependency Injection Details

The extension requires the following dependencies to be registered in your DI container:

- **ITelegramBotClientWrapper**
- **IRetryPolicyProvider**
- **IMessageSplitter**
- **IHtmlMessageProcessor**

Ensure these services are registered before adding the TelegramSender extension so that all required dependencies can be resolved.

## Contributing
Contributions are welcome! To contribute to this project, please follow these steps:

1. **Fork the Repository**: Create your own fork of the project on GitHub.
2. **Create a New Branch**: Make your changes in a separate branch.
3. **Submit a Pull Request**: Open a pull request with a clear description of your changes and improvements.
4. **Follow Coding Guidelines**: Ensure that your changes adhere to the project's coding standards and include unit tests where applicable.

If you encounter any issues or have suggestions for improvements, please open an issue on the GitHub repository.

# Contact

For questions, support, or further discussion, please open an issue on the GitHub repository or contact the project maintainers via the repository's discussion board.

# License

This project is licensed under the MIT License.