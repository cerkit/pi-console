using System;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;
using PiConsole.Models;

namespace PiConsole
{
    public class Engine : IUiService
    {
        private MenuItem[] _menuItems = Array.Empty<MenuItem>();
        private string[] _activeChannels = Array.Empty<string>();
        private readonly object _channelsLock = new object();

        private int _selectedIndex = 0;
        private bool _isRunning = true;
        private readonly MqttService _mqttService;
        
        // Refresh actions
        private Action? _refreshUi;
        private Action? _refreshOutput;
        private Action? _refreshOperations;
        private Action? _refreshStatus;
        private Action? _refreshHeader;
        private Action? _refreshMenu;

        private string _lastOutputContent = "";
        private string _lastOperationsContent = "";
        private string _lastStatusContent = "System idle.";
        private string _lastHeaderContent = "";
        private string _lastMenuContent = "";
        private UiConfigData? _uiConfig;
        
        // Track the current active dynamic session channel for executing Actions
        private string _currentSessionChannel = string.Empty;

        public Engine(MqttService mqttService)
        {
            _mqttService = mqttService;
        }

        public void UpdateUiConfig(UiConfigData config)
        {
            _uiConfig = config;
            _refreshUi?.Invoke();
        }

        public void UpdateMenu(MenuItem[] items)
        {
            _menuItems = items;
            if (_selectedIndex >= _menuItems.Length) _selectedIndex = 0;
            _refreshUi?.Invoke();
        }

        public void UpdatePanel(string targetPanel, string content)
        {
            if (targetPanel.Equals("commandProcessor", StringComparison.OrdinalIgnoreCase))
            {
                switch (content.ToUpperInvariant())
                {
                    case "EXIT":
                        _isRunning = false;
                        Environment.Exit(0);
                        break;
                    case "RESTART":
                        _lastStatusContent = "Restarting UI configuration sequence...";
                        _refreshStatus?.Invoke();

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var p = new { clientId = _mqttService.ClientId };
                                await _mqttService.PublishAsync("pi-console/client/startup", System.Text.Json.JsonSerializer.Serialize(p));
                            }
                            catch (Exception ex)
                            {
                                _lastStatusContent = $"[red]Restart err:[/] {Markup.Escape(ex.Message)}";
                                _refreshUi?.Invoke();
                            }
                        });
                        break;
                }
                return;
            }

            if (targetPanel.Equals("outputPanel", StringComparison.OrdinalIgnoreCase))
            {
                _lastOutputContent = content;
                _refreshOutput?.Invoke();
            }
            else if (targetPanel.Equals("operationsPanel", StringComparison.OrdinalIgnoreCase))
            {
                _lastOperationsContent = content;
                _refreshOperations?.Invoke();
            }
            else if (targetPanel.Equals("statusPanel", StringComparison.OrdinalIgnoreCase))
            {
                _lastStatusContent = content;
                _refreshStatus?.Invoke();
            }
            else if (targetPanel.Equals("headerPanel", StringComparison.OrdinalIgnoreCase))
            {
                _lastHeaderContent = content;
                _refreshHeader?.Invoke();
            }
            else if (targetPanel.Equals("menuPanel", StringComparison.OrdinalIgnoreCase))
            {
                _lastMenuContent = content;
                _refreshMenu?.Invoke();
            }
        }

        public void AddActiveChannel(string channelName)
        {
            var added = false;
            lock (_channelsLock)
            {
                var list = new System.Collections.Generic.List<string>(_activeChannels);
                if (!list.Contains(channelName))
                {
                    list.Add(channelName);
                    _activeChannels = list.ToArray();
                    added = true;
                    
                    // Track explicitly for ActionTopic payloads
                    if (channelName.StartsWith("pi-console/session/"))
                    {
                        _currentSessionChannel = channelName;
                    }
                }
            }

            if (added && _refreshUi != null)
            {
                _refreshUi.Invoke();
            }
        }

        public void Run()
        {
            Console.Title = "pi-console";
            AnsiConsole.Clear();

            var layout = new Layout("Root")
                .SplitRows(
                    new Layout("Header", CreateBanner()),
                    new Layout("Operations", CreateOperationsPanel()).Ratio(2),
                    new Layout("MiddleBottom")
                        .SplitColumns(
                            new Layout("Menu", CreateMenuPanel(_menuItems, _selectedIndex)).Ratio(1),
                            new Layout("Output", CreatePanel("Output", _lastOutputContent)).Ratio(2)
                        ).Ratio(1),
                    new Layout("Footer", CreatePanel("Status", _lastStatusContent)).Size(3)
                );

            AnsiConsole.Live(layout)
                .Overflow(VerticalOverflow.Crop)
                .Start(ctx =>
                {
                    _refreshUi = () => 
                    {
                        layout["Header"].Update(CreateBanner());
                        layout["Operations"].Update(CreateOperationsPanel());
                        layout["Menu"].Update(CreateMenuPanel(_menuItems, _selectedIndex));
                        layout["Output"].Update(CreatePanel("Output", _lastOutputContent));
                        layout["Footer"].Update(CreatePanel("Status", _lastStatusContent));
                        ctx.Refresh();
                    };

                    _refreshOutput = () =>
                    {
                        layout["Output"].Update(CreatePanel("Output", _lastOutputContent));
                        ctx.Refresh();
                    };

                    _refreshOperations = () => 
                    {
                        layout["Operations"].Update(CreateOperationsPanel());
                        ctx.Refresh();
                    };

                    _refreshStatus = () =>
                    {
                        layout["Footer"].Update(CreatePanel("Status", _lastStatusContent));
                        ctx.Refresh();
                    };

                    _refreshHeader = () =>
                    {
                        layout["Header"].Update(CreateBanner());
                        ctx.Refresh();
                    };

                    _refreshMenu = () =>
                    {
                        layout["Menu"].Update(CreateMenuPanel(_menuItems, _selectedIndex));
                        ctx.Refresh();
                    };

                    _mqttService.MessageReceived += (sender, msg) =>
                    {
                        _lastStatusContent = msg;
                        _refreshUi?.Invoke();
                    };

                    _mqttService.MenuItemsReceived += (sender, items) =>
                    {
                        // Fallback logic for backward compatibility
                        UpdateMenu(items);
                    };

                    _ = Task.Run(async () => 
                    {
                        try 
                        {
                            await _mqttService.StartAsync();
                            await Task.Delay(500); // Give subscriptions a moment to establish
                            var p = new { clientId = _mqttService.ClientId };
                            await _mqttService.PublishAsync("pi-console/client/startup", System.Text.Json.JsonSerializer.Serialize(p));
                        }
                        catch (Exception ex) 
                        {
                            _lastStatusContent = $"[red]MQTT connection failed:[/] {Markup.Escape(ex.Message)}";
                            System.IO.File.WriteAllText("mqtt_error.txt", ex.ToString());
                            _refreshUi?.Invoke();
                        }
                    });

                    while (_isRunning)
                    {
                        // Wait for key
                        if (Console.IsInputRedirected)
                        {
                            break;
                        }

                        var key = Console.ReadKey(true);
                        switch (key.Key)
                        {
                            case ConsoleKey.UpArrow:
                                _selectedIndex--;
                                if (_selectedIndex < 0) _selectedIndex = _menuItems.Length - 1;
                                _refreshUi?.Invoke();
                                break;
                            case ConsoleKey.DownArrow:
                                _selectedIndex++;
                                if (_selectedIndex >= _menuItems.Length) _selectedIndex = 0;
                                _refreshUi?.Invoke();
                                break;
                            case ConsoleKey.Q:
                            case ConsoleKey.Escape:
                                _isRunning = false;
                                break;
                            case ConsoleKey.Enter:
                                if (_menuItems.Length > 0 && _selectedIndex >= 0 && _selectedIndex < _menuItems.Length)
                                {
                                    var item = _menuItems[_selectedIndex];
                                    if (item.Label == "Logoff" || item.Label == "Exit")
                                    {
                                        _isRunning = false;
                                    }
                                    else
                                    {
                                        _lastOutputContent = $"Executing: {Markup.Escape(item.Label)}...";
                                        _refreshOutput?.Invoke();

                                        string? targetActionTopic = !string.IsNullOrEmpty(item.ActionTopic) ? item.ActionTopic : item.Action;

                                        if (!string.IsNullOrEmpty(targetActionTopic))
                                        {
                                            // Execute dynamic action off UI thread
                                            _ = Task.Run(async () =>
                                            {
                                                try
                                                {
                                                    var payload = new { sessionChannel = _currentSessionChannel };
                                                    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                                                    await _mqttService.PublishAsync(targetActionTopic, jsonPayload);
                                                }
                                                catch (Exception ex)
                                                {
                                                    _lastStatusContent = $"[red]Action err:[/] {Markup.Escape(ex.Message)}";
                                                    _refreshUi?.Invoke();
                                                }
                                            });
                                        }
                                    }
                                }
                                break;
                        }
                    }
                });
        }

        private Color GetBorderColor(string colorName)
        {
            return colorName.ToLower() switch
            {
                "black" => Color.Black,
                "red" => Color.Red,
                "green" => Color.Green,
                "yellow" => Color.Yellow,
                "blue" => Color.Blue,
                "magenta" => Color.Magenta,
                "cyan" => Color.Cyan,
                "white" => Color.White,
                "orange1" => Color.Orange1,
                "purple" => Color.Purple,
                "grey" => Color.Grey,
                "gold1" => Color.Gold1,
                "dodgerblue1" => Color.DodgerBlue1,
                _ => Color.Default
            };
        }

        private IRenderable CreateBanner()
        {
            var config = _uiConfig?.HeaderPanel;
            string title = !string.IsNullOrEmpty(config?.Title) ? config.Title : "PI-CONSOLE";
            string? borderColorName = !string.IsNullOrEmpty(config?.BorderColor) ? config.BorderColor : null;
            string? titleColorName = !string.IsNullOrEmpty(config?.TitleColor) ? config.TitleColor : null;

            string colorMarkup = titleColorName != null ? $"[{titleColorName}]" : "";
            string endMarkup = titleColorName != null ? "[/]" : "";

            var figlet = new FigletText(title).Centered();
            if (titleColorName != null) figlet.Color(GetBorderColor(titleColorName));

            string subtitleText = !string.IsNullOrEmpty(_lastHeaderContent) ? _lastHeaderContent : "v0.1-beta";
            var subtitle = new Markup($"{colorMarkup}{subtitleText}{endMarkup}").Centered();

            var grid = new Grid()
                .AddColumn(new GridColumn().Centered())
                .AddRow(figlet)
                .AddRow(subtitle);

            var panel = new Panel(grid)
                .Expand()
                .Border(BoxBorder.Square)
                .Padding(1, 1);

            if (borderColorName != null) panel.BorderColor(GetBorderColor(borderColorName));

            return panel;
        }

        private Panel CreateOperationsPanel()
        {
            var config = _uiConfig?.OperationsPanel;
            string title = !string.IsNullOrEmpty(config?.Title) ? config.Title : "Operations Panel";
            string? borderColorName = !string.IsNullOrEmpty(config?.BorderColor) ? config.BorderColor : null;
            string? titleColorName = !string.IsNullOrEmpty(config?.TitleColor) ? config.TitleColor : null;

            string colorMarkup = titleColorName != null ? $"[{titleColorName}]" : "";
            string endMarkup = titleColorName != null ? "[/]" : "";

            string[] channels;
            lock (_channelsLock)
            {
                channels = _activeChannels;
            }

            var table = new Table()
                .Expand()
                .Border(TableBorder.None)
                .AddColumn(new TableColumn($"{colorMarkup}{Markup.Escape(title)}{endMarkup}").Centered());

            if (!string.IsNullOrEmpty(_lastOperationsContent))
            {
                // Insert the dynamic payload string first
                table.AddRow(new Markup(_lastOperationsContent));
                table.AddEmptyRow();
            }

            if (channels.Length == 0)
            {
                table.AddRow($"{colorMarkup}No active channels...{endMarkup}");
            }
            else
            {
                foreach (var channel in channels)
                {
                    table.AddRow($"{colorMarkup}{Markup.Escape(channel)}{endMarkup}");
                }
            }

            var panel = new Panel(table).Expand().Border(BoxBorder.Square);
            if (borderColorName != null) panel.BorderColor(GetBorderColor(borderColorName));

            return panel;
        }

        private Panel CreatePanel(string panelKey, string content)
        {
            PanelConfig? config = null;
            if (panelKey == "Status") config = _uiConfig?.StatusPanel;
            else if (panelKey == "Output") config = _uiConfig?.OutputPanel;

            // Provide a fallback config logic generally 
            string title = !string.IsNullOrEmpty(config?.Title) ? config.Title : $"{panelKey} Panel";
            string? borderColorName = !string.IsNullOrEmpty(config?.BorderColor) ? config.BorderColor : null;
            string? titleColorName = !string.IsNullOrEmpty(config?.TitleColor) ? config.TitleColor : null;

            string colorMarkup = titleColorName != null ? $"[{titleColorName}]" : "";
            string endMarkup = titleColorName != null ? "[/]" : "";

            IRenderable alignableContent;
            
            if (panelKey == "Output")
            {
                // Left justify output panel content, center vertically
                alignableContent = new Align(new Markup(content), HorizontalAlignment.Left, VerticalAlignment.Middle);
            }
            else 
            {
                // Center align Status Panel and other panels
                alignableContent = new Align(new Markup(content), HorizontalAlignment.Center, VerticalAlignment.Middle);
            }

            var panel = new Panel(alignableContent)
                .Header($"{colorMarkup}{Markup.Escape(title)}{endMarkup}", Justify.Center)
                .Expand()
                .Border(BoxBorder.Square);

            if (borderColorName != null) panel.BorderColor(GetBorderColor(borderColorName));

            return panel;
        }

        private Panel CreateMenuPanel(MenuItem[] items, int selectedIndex)
        {
            var config = _uiConfig?.MenuPanel;
            string title = !string.IsNullOrEmpty(config?.Title) ? config.Title : "Menu";
            string? borderColorName = !string.IsNullOrEmpty(config?.BorderColor) ? config.BorderColor : null;
            string? titleColorName = !string.IsNullOrEmpty(config?.TitleColor) ? config.TitleColor : null;

            string colorMarkup = titleColorName != null ? $"[{titleColorName}]" : "";
            string endMarkup = titleColorName != null ? "[/]" : "";

            var grid = new Grid().AddColumn(new GridColumn());

            if (!string.IsNullOrEmpty(_lastMenuContent))
            {
                grid.AddRow(new Markup(_lastMenuContent));
                grid.AddEmptyRow();
            }

            for (int i = 0; i < items.Length; i++)
            {
                string? color = !string.IsNullOrEmpty(items[i].Color) ? items[i].Color : null;
                string iconStr = !string.IsNullOrEmpty(items[i].Icon) ? $"{items[i].Icon}  " : "";

                try 
                {
                    if (color != null)
                    {
                        if (i == selectedIndex)
                        {
                            grid.AddRow(new Markup($"[black on {color}]> {iconStr}{Markup.Escape(items[i].Label)} [/]"));
                        }
                        else
                        {
                            grid.AddRow(new Markup($"[{color}]  {iconStr}{Markup.Escape(items[i].Label)}[/]"));
                        }
                    } 
                    else 
                    {
                        if (i == selectedIndex)
                        {
                            grid.AddRow(new Markup($"[invert]> {iconStr}{Markup.Escape(items[i].Label)} [/]"));
                        }
                        else
                        {
                            grid.AddRow(new Markup($"  {iconStr}{Markup.Escape(items[i].Label)}"));
                        }
                    }
                }
                catch 
                {
                    // Fallback to default styling if the color name is invalid in Spectre.Console
                    if (i == selectedIndex)
                    {
                        grid.AddRow(new Markup($"[invert]> {iconStr}{Markup.Escape(items[i].Label)} [/]"));
                    }
                    else
                    {
                        grid.AddRow(new Markup($"  {iconStr}{Markup.Escape(items[i].Label)}"));
                    }
                }
            }

            var panel = new Panel(new Align(grid, HorizontalAlignment.Center, VerticalAlignment.Middle))
                .Header($"{colorMarkup}{Markup.Escape(title)}{endMarkup}", Justify.Center)
                .Expand()
                .Border(BoxBorder.Square);

            if (borderColorName != null) panel.BorderColor(GetBorderColor(borderColorName));

            return panel;
        }
    }
}
