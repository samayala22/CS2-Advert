using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Core;

using Tomlyn.Extensions.Configuration;

namespace Advert;

class MainConfigModel {
    public string ChatPrefix { get; set; } = "[blue][Server][default]";
    public float AdInterval { get; set; } = 60.0f;
    public List<string> Ads { get; set; } = new List<string> {
        "Use [red]!nominate[default] <mapname> / <workshopid> to nominate maps!",
        "Use [red]!rtv[default] to rock the vote for map change!",
        "Use [red]!ws[default] to change knife or skins!"
    };
}

[PluginMetadata(
    Id = "Advert",
    #if WORKFLOW
        Version = "WORKFLOW_VERSION",
    #else
        Version = "Local",
    #endif
    Name = "Advert",
    Author = "Praetor",
    Description = "Shows adverts periodically in chat"
)]
public class Advert : BasePlugin {
    private ServiceProvider? m_provider;
    private MainConfigModel m_config = new();
    private CancellationTokenSource? m_advert_timer_token;
    private int m_current_ad_index = 0;

    public Advert(ISwiftlyCore core) : base(core) {
    }

    public override void Load(bool hotReload) {
        // Load configuration
        Core.Configuration
            .InitializeTomlWithModel<MainConfigModel>("config.toml", "Main")
            .Configure(builder => {
                builder.AddTomlFile("config.toml", optional: false, reloadOnChange: true);
            });

        ServiceCollection services = new();
        services
            .AddSwiftly(Core)
            .AddOptionsWithValidateOnStart<MainConfigModel>()
            .BindConfiguration("Main");

        m_provider = services.BuildServiceProvider();
        m_config = m_provider.GetRequiredService<IOptions<MainConfigModel>>().Value;

        // Start the advert timer
        StartAdvertTimer();

        Console.WriteLine($"[Advert] Loaded with {m_config.Ads.Count} ads, interval: {m_config.AdInterval}s");
    }

    public override void Unload() {
        m_advert_timer_token?.Cancel();
        m_advert_timer_token = null;
        m_provider?.Dispose();
        Console.WriteLine("[Advert] Unloaded");
    }

    private void StartAdvertTimer() {
        m_advert_timer_token?.Cancel();
        
        if (m_config.Ads.Count == 0) {
            Console.WriteLine("[Advert] No ads configured, timer not started");
            return;
        }

        m_advert_timer_token = Core.Scheduler.RepeatBySeconds(m_config.AdInterval, ShowNextAd);
    }

    private void ShowNextAd() {
        if (m_config.Ads.Count == 0) return;

        string message = m_config.Ads[m_current_ad_index];
        string formattedMessage = FormatMessage(m_config.ChatPrefix, message);
        
        Core.PlayerManager.SendChat(formattedMessage);

        m_current_ad_index = (m_current_ad_index + 1) % m_config.Ads.Count;
    }

    private string FormatMessage(string prefix, string message) {
        string fullMessage = string.IsNullOrEmpty(prefix) ? message : $"{prefix} {message}";
        return Helper.Colored(fullMessage);
    }
}
