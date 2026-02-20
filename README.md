# pi-console

pi-console is a .NET 10 console application that emulates the look and feel of a classic Bulletin Board System layout, built with a modern static approach using `Spectre.Console`.

## Features
- **Static Layout**: Uses a rendering loop via `AnsiConsole.Live` to display panels without scrolling or breaking the terminal layout.
- **Interactive Menu**: Fully navigatable using the Up/Down Arrow keys. Press "Enter" to make a selection.
- **Dynamic Output**: Display panel updates immediately when selecting items.
- **Real-time Status Updates**: Subscribes to an MQTT broker to display real-time signal messages at the bottom status bar.

## Requirements
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An MQTT broker (Optional but recommended to test the status signals).

## Installation and Execution

1. Clone the repository:
   ```bash
   git clone https://github.com/cerkit/pi-console.git
   ```

2. Navigate into the directory:
   ```bash
   cd pi-console
   ```

3. **Configure Secrets**:
   Create a file named `secrets.json` in the root of the project to configure your MQTT Broker IP Address and Port. It should look like this:
   ```json
   {
     "MqttIpAddress": "[IP_ADDRESS]",
     "MqttPort": 1883
   }
   ```
   *Note: This file is ignored by git.*

4. Run the application:
   ```bash
   dotnet run
   ```

## Usage

When the application is running:
- Use the **Up/Down Arrow keys** to switch menu items.
- Press **Enter** to select an item. It will be printed in the Output Panel.
- Choose **Logoff** to exit the BBS application safely.
- If messages are published to `test/signal` on your MQTT broker, they will appear dynamically in the System Status panel at the bottom.

## Technology Stack
- **.NET 10**: Console Framework.
- **Spectre.Console**: Used for layout management, UI widgets, and styling.
- **MQTTnet**: Used to subscribe to the remote telemetry broker.
