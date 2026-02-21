# pi-console

pi-console is a .NET 10 console application that emulates the look and feel of a classic Bulletin Board System layout, built with a modern static approach using `Spectre.Console`.

## Features
- **Static Layout**: Uses a rendering loop via `AnsiConsole.Live` to display panels without scrolling or breaking the terminal layout.
- **Interactive Menu**: Fully navigatable using the Up/Down Arrow keys. Press "Enter" to make a selection, which prints to the Output panel.
- **Dynamic Channels (Pi Calculus)**: Uses MQTT "channel mobility" to dynamically subscribe to new communication sessions established via handshakes.
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
   Create a directory named `.secrets` in the root of the project, then create a file named `secrets.json` inside it to configure your MQTT Broker IP Address and Port. It should look like this:
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
- Use the **Up/Down Arrow keys** to scroll through menu items.
- Press **Enter** on a menu item to push its label to the Output panel.
- Press **Q** or **Escape** (or press Enter on an item labeled "Logoff" or "Exit") to log off and exit the application safely.

### The Pi Calculus Architecture
This application utilizes a "channel mobility" system for MQTT communication. All application UI configuration occurs dynamically. 

- **Startup**: When the app starts, it publishes an empty payload to the `pi-console/client/startup` topic to announce its presence.

- **Session Handshakes**: The app listens on the `pi-console/handshake` topic for new connection instructions. Handshakes are formatted in JSON:
  ```json
  {"action": "CONNECT", "replyToChannel": "session_id"}
  ```
  or
  ```json
  {"action": "PROVIDE_MENU", "channel": "session_id"}
  ```
  When the application receives a handshake, it reads the dynamic channel string, instantly opens a subscription to that active channel, and registers it in the live "Operations" screen.

- **Dynamic Menus**: If a `PROVIDE_MENU` handshake is requested, the application publishes a `{"status": "READY"}` payload back to the dynamic channel. It then waits for a JSON array of `MenuItem` objects on that channel. 
  - Expected JSON format for a menu array:
    ```json
    [
      { "id": 1, "label": "System Status", "icon": "info", "color": "green" },
      { "id": 2, "label": "Device Settings", "icon": "settings", "color": "purple" }
    ]
    ```

- **Global Messages**: If messages are published to `pi-console/status` on your MQTT broker, they will appear dynamically in the System Status panel at the bottom.

## Technology Stack
- **.NET 10**: Console Framework.
- **Spectre.Console**: Used for layout management, UI widgets, and styling.
- **MQTTnet**: Used to subscribe to the remote telemetry broker.
