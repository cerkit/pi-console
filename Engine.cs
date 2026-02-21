using System;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PiConsole
{
    public class Engine
    {
        private MenuItem[] _menuItems = new MenuItem[] {
            new MenuItem { Id = 0, Label = "Waiting for menu items...", Color = "grey" }
        };
        private int _selectedIndex = 0;
        private bool _isRunning = true;

        public void Run()
        {
            var mqttService = new MqttService();
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
                    new Layout("Footer", CreatePanel("Status Panel", "System idle.")).Size(3)
                );

            AnsiConsole.Live(layout)
                .Overflow(VerticalOverflow.Crop)
                .Start(ctx =>
                {
                    mqttService.MessageReceived += (sender, msg) =>
                    {
                        layout["Footer"].Update(CreatePanel("Status Panel", msg));
                        ctx.Refresh();
                    };

                    mqttService.MenuItemsReceived += (sender, items) =>
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
                            await mqttService.StartAsync();
                            await mqttService.PublishAsync("pi-console/initialize", "7f5407aa-cac5-4952-80ca-c73863d78fc4");
                        }
                        catch (Exception ex) 
                        {
                            layout["Footer"].Update(CreatePanel("Status Panel", $"[red]MQTT connection failed:[/] {Markup.Escape(ex.Message)}"));
                            ctx.Refresh();
                        }
                    });

                    while (_isRunning)
                    {
                        // Refresh layout elements
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
                            case ConsoleKey.Enter:
                                // Functionality disabled for now as requested
                                /*
                                if (_menuItems.Length > 0)
                                {
                                    if (_menuItems[_selectedIndex].Label == "Logoff")
                                    {
                                        _isRunning = false;
                                    }
                                    else
                                    {
                                        layout["Output"].Update(CreatePanel("Output Panel", $"Selected: {_menuItems[_selectedIndex].Label}"));
                                    }
                                }
                                */
                                break;
                        }
                    }
                });
        }

        private IRenderable CreateBanner()
        {
            var figlet = new FigletText("PI-CONSOLE")
                .Centered()
                .Color(Color.White);

            var subtitle = new Markup("v0.1-beta").Centered();

            var grid = new Grid()
                .AddColumn(new GridColumn().Centered())
                .AddRow(figlet)
                .AddRow(subtitle);

            return new Panel(grid)
                .BorderColor(Color.Purple)
                .Padding(1, 1);
        }

        private Panel CreatePanel(string title, string content)
        {
            var alignableContent = new Align(new Markup(content), HorizontalAlignment.Center, VerticalAlignment.Middle);

            if (string.IsNullOrEmpty(content))
            {
                alignableContent = new Align(new Markup(title), HorizontalAlignment.Center, VerticalAlignment.Middle);
            }

            var panel = new Panel(alignableContent)
                .Expand()
                .Border(BoxBorder.Square);

            switch (title)
            {
                case "Operations":
                    panel.BorderColor(Color.Orange1);
                    break;
                case "Menu":
                    panel.BorderColor(Color.Blue);
                    break;
                case "Output Panel":
                    panel.BorderColor(Color.Green);
                    break;
                default:
                    panel.BorderColor(Color.Grey);
                    break;
            }

            return panel;
        }

        private Panel CreateMenuPanel(MenuItem[] items, int selectedIndex)
        {
            var grid = new Grid().AddColumn(new GridColumn());
            grid.AddRow(new Markup("[blue]Menu[/]").Centered());
            grid.AddRow(new Text("")); 

            for (int i = 0; i < items.Length; i++)
            {
                string color = !string.IsNullOrEmpty(items[i].Color) ? items[i].Color : "white";
                if (i == selectedIndex)
                {
                    grid.AddRow(new Markup($"[black on {color}]> {items[i].Label} [/]"));
                }
                else
                {
                    grid.AddRow(new Markup($"[{color}]  {items[i].Label}[/]"));
                }
            }

            return new Panel(new Align(grid, HorizontalAlignment.Center, VerticalAlignment.Middle))
                .Expand()
                .Border(BoxBorder.Square)
                .BorderColor(Color.Blue);
        }
    }
}
