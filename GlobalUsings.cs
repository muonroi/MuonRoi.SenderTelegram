global using HtmlAgilityPack;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using MuonRoi.SenderTelegram.Helpers;
global using MuonRoi.SenderTelegram.Interfaces;
global using MuonRoi.SenderTelegram.Models;
global using MuonRoi.SenderTelegram.Services;
global using Polly;
global using Polly.Retry;
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;
global using Telegram.Bot;
global using Telegram.Bot.Types;
global using Telegram.Bot.Types.Enums;
global using Telegram.Bot.Types.ReplyMarkups;
