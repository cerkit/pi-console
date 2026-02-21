using System;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PiConsole
{
    public class Engine
    {
        private MenuItem[] _menuItems = Array.Empty<MenuItem>();
        private string[] _activeChannels = Array.Empty<string>();
        private readonly object _channelsLock = new object();

        private int _selectedIndex = 0;
        private bool _isRunning = true;
        private readonly MqttService _mqttService;
        private Action _refreshUi;

        public Engine(MqttService mqttService)
        {
            _mqttService = mqttService;
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
                    new Layout("Operations", CreatePanel("Operations Panel", "")).Ratio(2),
                    new Layout("MiddleBottom")
                        .SplitColumns(
                            new Layout("Menu", CreateMenuPanel(_menuItems, _selectedIndex)).Ratio(1),
                            new Layout("Output", CreatePanel("Output Panel", "")).Ratio(2)
                        ).Ratio(1),
                    new Layout("Footer", CreatePanel("Status Panel", "[orange1]System idle.[/]")).Size(3)
                );

            AnsiConsole.Live(layout)
                .Overflow(VerticalOverflow.Crop)
                .Start(ctx =>
                {
                    _refreshUi = () => 
                    {
                        layout["Operations"].Update(CreateOperationsPanel());
                        ctx.Refresh();
                    };

                    _mqttService.MessageReceived += (sender, msg) =>
                    {
                        layout["Footer"].Update(CreatePanel("Status Panel", $"[orange1]{Markup.Escape(msg)}[/]"));
                        ctx.Refresh();
                    };

                    _mqttService.MenuItemsReceived += (sender, items) =>
                    {
                        _menuItems = items;
                        if (_selectedIndex >= _menuItems.Length) _selectedIndex = 0;
                        layout["Menu"].Update(CreateMenuPanel(_menuItems, _selectedIndex));
                        ctx.Refresh();
                    };

                    _ = Task.Run(async () => 
                    {
                        try 
                        {
                            await _mqttService.StartAsync();
                            await Task.Delay(500); // Give subscriptions a moment to establish
                            await _mqttService.PublishAsync("pi-console/client/startup", "");
                        }
                        catch (Exception ex) 
                        {
                            layout["Footer"].Update(CreatePanel("Status Panel", $"[orange1]MQTT connection failed:[/] {Markup.Escape(ex.Message)}"));
                            ctx.Refresh();
                        }
                    });

                    while (_isRunning)
                    {
                        // Refresh layout elements
                        layout["Operations"].Update(CreateOperationsPanel());
                        layout["Menu"].Update(CreateMenuPanel(_menuItems, _selectedIndex));
                        ctx.Refresh();

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
                                break;
                            case ConsoleKey.DownArrow:
                                _selectedIndex++;
                                if (_selectedIndex >= _menuItems.Length) _selectedIndex = 0;
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
                                        layout["Output"].Update(CreatePanel("Output Panel", $"[orange1]Selected: [/][bold orange1]{Markup.Escape(item.Label)}[/]"));
                                        ctx.Refresh();
                                    }
                                }
                                break;
                        }
                    }
                });
        }

        private IRenderable CreateBanner()
        {
            var figlet = new FigletText("PI-CONSOLE")
                .Centered()
                .Color(Color.Orange1);

            var subtitle = new Markup("[black on orange1]v0.1-beta[/]").Centered();

            var grid = new Grid()
                .AddColumn(new GridColumn().Centered())
                .AddRow(figlet)
                .AddRow(subtitle);

            return new Panel(grid)
                .BorderColor(Color.Orange1)
                .Padding(1, 1);
        }

        private Panel CreateOperationsPanel()
        {
            string[] channels;
            lock (_channelsLock)
            {
                channels = _activeChannels;
            }

            var table = new Table()
                .Expand()
                .Border(TableBorder.None)
                .AddColumn(new TableColumn("[orange1]Active Pi-Calculus Channels[/]").Centered());

            if (channels.Length == 0)
            {
                table.AddRow("[orange1]No active channels...[/]");
            }
            else
            {
                foreach (var channel in channels)
                {
                    table.AddRow($"[orange1]{Markup.Escape(channel)}[/]");
                }
            }

            return new Panel(table)
                .Expand()
                .Border(BoxBorder.Square)
                .BorderColor(Color.Orange1);
        }

        private Panel CreatePanel(string title, string content)
        {
            var alignableContent = new Align(new Markup(content), HorizontalAlignment.Center, VerticalAlignment.Middle);

            if (string.IsNullOrEmpty(content))
            {
                alignableContent = new Align(new Markup($"[orange1]{Markup.Escape(title)}[/]"), HorizontalAlignment.Center, VerticalAlignment.Middle);
            }

            var panel = new Panel(alignableContent)
                .Expand()
                .Border(BoxBorder.Square);

            switch (title)
            {
                case "Menu":
                    panel.BorderColor(Color.Orange1);
                    break;
                case "Output Panel":
                    panel.BorderColor(Color.Orange1);
                    break;
                case "Status Panel":
                    panel.BorderColor(Color.Orange1);
                    break;
                default:
                    panel.BorderColor(Color.Orange1);
                    break;
            }

            return panel;
        }

        private Panel CreateMenuPanel(MenuItem[] items, int selectedIndex)
        {
            var grid = new Grid().AddColumn(new GridColumn());

            for (int i = 0; i < items.Length; i++)
            {
                string color = !string.IsNullOrEmpty(items[i].Color) ? items[i].Color : "orange1";
                // Fix common invalid Spectre colors
                if (color.ToLower() == "orange") color = "orange1";

                try 
                {
                    if (i == selectedIndex)
                    {
                        grid.AddRow(new Markup($"[black on {color}]> {Markup.Escape(items[i].Label)} [/]"));
                    }
                    else
                    {
                        grid.AddRow(new Markup($"[{color}]  {Markup.Escape(items[i].Label)}[/]"));
                    }
                }
                catch 
                {
                    // Fallback to orange1 if the color name is invalid in Spectre.Console
                    if (i == selectedIndex)
                    {
                        grid.AddRow(new Markup($"[black on orange1]> {Markup.Escape(items[i].Label)} [/]"));
                    }
                    else
                    {
                        grid.AddRow(new Markup($"[orange1]  {Markup.Escape(items[i].Label)}[/]"));
                    }
                }
            }

            return new Panel(new Align(grid, HorizontalAlignment.Center, VerticalAlignment.Middle))
                .Header("[orange1]Menu[/]", Justify.Center)
                .Expand()
                .Border(BoxBorder.Square)
                .BorderColor(Color.Orange1);
        }
    }
}
