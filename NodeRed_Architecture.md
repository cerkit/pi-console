# Pi Calculus Orchestration Architecture

## Overview
This system uses a client-initiated Pi Calculus architecture over MQTT. The orchestration layer is handled by Node-RED, which dynamically negotiates private session channels with clients. 
The .NET solution utilizes a shared code architecture to support two distinct clients:
1. `pi-console` (Spectre.Console terminal client)
2. `pi-wasm` (Blazor WebAssembly client)

**Core Rule:** All core Pi Calculus models, MQTT handlers, and session managers must reside in the shared library. Only client-specific rendering logic goes into the individual client projects.

## MQTT Topic Structure
* **Startup Announcement:** `pi-console/client/startup`
* **Public Handshake:** `pi-console/handshake/{clientId}`
* **Private Dynamic Session (νz):** `pi-console/session/setup/{clientId}/{uuid}`
* **Client Action Triggers:** `pi-console/action/{actionName}`

## Interaction Sequence & Schemas

### 1. Client Startup
* **Client publishes:** `{"clientId": "{ClientId}"}` to the Startup Announcement topic.
* **Client subscribes:** To its specific Public Handshake topic.

### 2. The Handshake (νz)
* **Node-RED publishes:**
```json
{
  "action": "INITIATE_SESSION",
  "channel": "pi-console/session/setup/{clientId}/{uuid}"
}
```
* **Client action:** Extracts the `channel`, dynamically subscribes to it, and publishes `{"status": "READY"}` to that same dynamic channel.

### 3. Session Continuation (UI & Menu)
* **Node-RED publishes UiConfig:** `{"messageType": "UiConfig", "data": { ... }}`
* **Client action:** Renders the layout/panels and publishes `{"status": "UI_READY"}` back to the dynamic channel.
* **Node-RED publishes Menu:** `{"messageType": "Menu", "data": [ { "id": 1, "actionTopic": "...", ... } ]}`
* **Client action:** Injects the menu items into the custom input loop.

### 4. Remote Actions & Live Updates
* **Client action:** On menu selection, publishes to the item's `actionTopic` with payload: `{"sessionChannel": "pi-console/session/setup/{clientId}/{uuid}"}`.
* **Node-RED action:** Processes the request and publishes a live update back to the `sessionChannel`:
```json
{
  "messageType": "PanelUpdate",
  "data": { "targetPanel": "outputPanel", "content": "..." }
}
```

---
## Current Node-RED Flow Export
*(Developer Note: Keep the latest Node-RED workspace JSON exported below this line so Antigravity can analyze the server-side logic and generate corresponding C# models).*

```json
[
    {
        "id": "3d75251ef0c130f7",
        "type": "tab",
        "label": "Flow 1",
        "disabled": false,
        "info": "",
        "env": []
    },
    {
        "id": "inject_pi_menu",
        "type": "inject",
        "z": "3d75251ef0c130f7",
        "name": "Initiate Menu Handshake",
        "props": [
            {
                "p": "payload"
            }
        ],
        "repeat": "",
        "crontab": "",
        "once": false,
        "onceDelay": 0.1,
        "topic": "",
        "payload": "",
        "payloadType": "date",
        "x": 250,
        "y": 140,
        "wires": [
            [
                "func_menu_handshake"
            ]
        ]
    },
    {
        "id": "func_menu_handshake",
        "type": "function",
        "z": "3d75251ef0c130f7",
        "name": "Inititate Session Channel (νz)",
        "func": "// Node Name: Initiate Session Channel (νz)\n\n// Fallback to \"default\" if the client doesn't provide an ID\nconst clientId = msg.payload.clientId || \"default\";\n\n// Embed the clientId into the private channel path\nconst dynamicChannel = `pi-console/session/setup/${clientId}/${msg._msgid}`;\n\n// Target the handshake specifically to this client\nmsg.topic = `pi-console/handshake/${clientId}`;\nmsg.payload = {\n    action: \"INITIATE_SESSION\",\n    channel: dynamicChannel\n};\nreturn msg;",
        "outputs": 1,
        "timeout": "",
        "noerr": 0,
        "initialize": "",
        "finalize": "",
        "libs": [],
        "x": 510,
        "y": 140,
        "wires": [
            [
                "mqtt_out_public",
                "7a585de1b22c886e"
            ]
        ]
    },
    {
        "id": "mqtt_out_public",
        "type": "mqtt out",
        "z": "3d75251ef0c130f7",
        "name": "Publish Handshake",
        "topic": "",
        "qos": "0",
        "retain": "false",
        "respTopic": "",
        "contentType": "",
        "userProps": "",
        "correl": "",
        "expiry": "",
        "broker": "26de6532b290a5e4",
        "x": 770,
        "y": 160,
        "wires": []
    },
    {
        "id": "mqtt_in_startup",
        "type": "mqtt in",
        "z": "3d75251ef0c130f7",
        "name": "Listen for Client Startup",
        "topic": "pi-console/client/startup",
        "qos": "0",
        "datatype": "auto-detect",
        "broker": "26de6532b290a5e4",
        "nl": false,
        "rap": false,
        "inputs": 0,
        "x": 240,
        "y": 100,
        "wires": [
            [
                "func_menu_handshake"
            ]
        ]
    },
    {
        "id": "4636064a1df786f1",
        "type": "mqtt out",
        "z": "3d75251ef0c130f7",
        "d": true,
        "name": "Publish to Session",
        "topic": "",
        "qos": "0",
        "retain": "false",
        "respTopic": "",
        "contentType": "",
        "userProps": "",
        "correl": "",
        "expiry": "",
        "broker": "26de6532b290a5e4",
        "x": 900,
        "y": 280,
        "wires": []
    },
    {
        "id": "1bcd76286657216e",
        "type": "function",
        "z": "3d75251ef0c130f7",
        "d": true,
        "name": "Generate UI Config",
        "func": "msg.payload = {\n    messageType: \"UiConfig\",\n    data: {\n        headerPanel: { title: \"Pi-Console\", borderColor: \"white\", titleColor: \"white\" },\n        operationsPanel: { title: \"Operations\", borderColor: \"green\" },\n        statusPanel: { title: \"Status\", borderColor: \"yellow\" },\n        menuPanel: { title: \"Main Menu\", borderColor: \"purple\" },\n        outputPanel: { title: \"System Output\", borderColor: \"orange1\" }\n    }\n};\nreturn msg;",
        "outputs": 1,
        "timeout": "",
        "noerr": 0,
        "initialize": "",
        "finalize": "",
        "libs": [],
        "x": 660,
        "y": 240,
        "wires": [
            [
                "4636064a1df786f1"
            ]
        ]
    },
    {
        "id": "83159ae676231ac9",
        "type": "function",
        "z": "3d75251ef0c130f7",
        "d": true,
        "name": "Generate Menu",
        "func": "msg.payload = {\n    messageType: \"Menu\",\n    data: [\n        { id: 1, label: \"System Status\", icon: \"\", color: \"green\", actionTopic: \"pi-console/action/status\" },\n        { id: 2, label: \"Network Config\", icon: \"\", color: \"blue\", actionTopic: \"pi-console/action/network\" },\n        { id: 3, label: \"Sensor Logs\", icon: \"\", color: \"yellow\", actionTopic: \"pi-console/action/sensors\" },\n        { id: 4, label: \"Device Settings\", icon: \"\", color: \"purple\", actionTopic: \"pi-console/action/settings\" },\n        { id: 5, label: \"Restart\", icon: \"\", color: \"red\", actionTopic: \"pi-console/action/restart\" }\n    ]\n};\nreturn msg;",
        "outputs": 1,
        "timeout": "",
        "noerr": 0,
        "initialize": "",
        "finalize": "",
        "libs": [],
        "x": 650,
        "y": 320,
        "wires": [
            [
                "4636064a1df786f1"
            ]
        ]
    },
    {
        "id": "30e80a4684fc1547",
        "type": "switch",
        "z": "3d75251ef0c130f7",
        "d": true,
        "name": "Check Client State",
        "property": "payload.status",
        "propertyType": "msg",
        "rules": [
            {
                "t": "eq",
                "v": "READY",
                "vt": "str"
            },
            {
                "t": "eq",
                "v": "UI_READY",
                "vt": "str"
            }
        ],
        "checkall": "true",
        "repair": false,
        "outputs": 2,
        "x": 420,
        "y": 280,
        "wires": [
            [
                "1bcd76286657216e"
            ],
            [
                "83159ae676231ac9"
            ]
        ]
    },
    {
        "id": "b6c9aea8e401bacf",
        "type": "mqtt in",
        "z": "3d75251ef0c130f7",
        "d": true,
        "name": "Listen to Session Channel",
        "topic": "pi-console/session/#",
        "qos": "0",
        "datatype": "json",
        "broker": "26de6532b290a5e4",
        "nl": false,
        "rap": false,
        "inputs": 0,
        "x": 180,
        "y": 280,
        "wires": [
            [
                "30e80a4684fc1547"
            ]
        ]
    },
    {
        "id": "mqtt_in_actions",
        "type": "mqtt in",
        "z": "3d75251ef0c130f7",
        "name": "Listen for Actions",
        "topic": "pi-console/action/#",
        "qos": "0",
        "datatype": "json",
        "broker": "26de6532b290a5e4",
        "nl": false,
        "rap": false,
        "inputs": 0,
        "x": 90,
        "y": 480,
        "wires": [
            [
                "switch_action_router"
            ]
        ]
    },
    {
        "id": "switch_action_router",
        "type": "switch",
        "z": "3d75251ef0c130f7",
        "name": "Route Action",
        "property": "topic",
        "propertyType": "msg",
        "rules": [
            {
                "t": "eq",
                "v": "pi-console/action/status",
                "vt": "str"
            },
            {
                "t": "eq",
                "v": "pi-console/action/network",
                "vt": "str"
            },
            {
                "t": "eq",
                "v": "pi-console/action/sensors",
                "vt": "str"
            },
            {
                "t": "eq",
                "v": "pi-console/action/settings",
                "vt": "str"
            },
            {
                "t": "eq",
                "v": "pi-console/action/restart",
                "vt": "str"
            }
        ],
        "checkall": "true",
        "repair": false,
        "outputs": 5,
        "x": 300,
        "y": 480,
        "wires": [
            [
                "func_process_status"
            ],
            [
                "func_process_network"
            ],
            [
                "bf85e6680d616390"
            ],
            [
                "ddcd28dbe14a7aee"
            ],
            [
                "3ab8b165edc077f2"
            ]
        ]
    },
    {
        "id": "func_process_status",
        "type": "function",
        "z": "3d75251ef0c130f7",
        "name": "Process System Status",
        "func": "// Extract the private channel sent by the client\nconst replyChannel = msg.payload.sessionChannel;\n\n// Execute your arbitrary logic here (e.g., API calls, reading sensors)\nconst systemData = \"[green]System Status: ONLINE[/]\\nCPU Usage: 42%\\nMemory: 2.1GB / 8.0GB\\nUptime: 14 Days, 2 Hrs\";\n\n// Route the response back to the specific client session\nmsg.topic = replyChannel;\nmsg.payload = {\n    messageType: \"PanelUpdate\",\n    data: {\n        targetPanel: \"outputPanel\",\n        content: systemData\n    }\n};\nreturn msg;",
        "outputs": 1,
        "timeout": "",
        "noerr": 0,
        "initialize": "",
        "finalize": "",
        "libs": [],
        "x": 550,
        "y": 440,
        "wires": [
            [
                "mqtt_out_action_reply"
            ]
        ]
    },
    {
        "id": "func_process_network",
        "type": "function",
        "z": "3d75251ef0c130f7",
        "name": "Process Network Config",
        "func": "const replyChannel = msg.payload.sessionChannel;\n\nmsg.topic = replyChannel;\nmsg.payload = {\n    messageType: \"PanelUpdate\",\n    data: {\n        targetPanel: \"operationsPanel\",\n        content: \"[blue]Network Interfaces:[/]\\neth0: localhost\\nwlan0: Disconnected\"\n    }\n};\nreturn msg;",
        "outputs": 1,
        "timeout": "",
        "noerr": 0,
        "initialize": "",
        "finalize": "",
        "libs": [],
        "x": 560,
        "y": 480,
        "wires": [
            [
                "mqtt_out_action_reply"
            ]
        ]
    },
    {
        "id": "mqtt_out_action_reply",
        "type": "mqtt out",
        "z": "3d75251ef0c130f7",
        "name": "Send Panel Update to Session",
        "topic": "",
        "qos": "0",
        "retain": "false",
        "respTopic": "",
        "contentType": "",
        "userProps": "",
        "correl": "",
        "expiry": "",
        "broker": "26de6532b290a5e4",
        "x": 940,
        "y": 520,
        "wires": []
    },
    {
        "id": "bf85e6680d616390",
        "type": "function",
        "z": "3d75251ef0c130f7",
        "name": "Process Sensor Status",
        "func": "// Extract the private channel sent by the client\nconst replyChannel = msg.payload.sessionChannel;\n\n// Execute your arbitrary logic here (e.g., API calls, reading sensors)\nconst systemData = \"[green]Sensor Status: ENABLED[/]\\nHumidity: 12%\\nTemperature: 31℉\";\n\n// Route the response back to the specific client session\nmsg.topic = replyChannel;\nmsg.payload = {\n    messageType: \"PanelUpdate\",\n    data: {\n        targetPanel: \"outputPanel\",\n        content: systemData\n    }\n};\nreturn msg;",
        "outputs": 1,
        "timeout": "",
        "noerr": 0,
        "initialize": "",
        "finalize": "",
        "libs": [],
        "x": 550,
        "y": 520,
        "wires": [
            [
                "mqtt_out_action_reply"
            ]
        ]
    },
    {
        "id": "ddcd28dbe14a7aee",
        "type": "function",
        "z": "3d75251ef0c130f7",
        "name": "Process Settings",
        "func": "// Extract the private channel sent by the client\nconst replyChannel = msg.payload.sessionChannel;\n\n// Execute your arbitrary logic here (e.g., API calls, reading sensors)\nconst systemData = \"[green]Node-RED: CONFIGURED[/]\\nClient-UI: CONFIGURED\";\n\n// Route the response back to the specific client session\nmsg.topic = replyChannel;\nmsg.payload = {\n    messageType: \"PanelUpdate\",\n    data: {\n        targetPanel: \"outputPanel\",\n        content: systemData\n    }\n};\nreturn msg;",
        "outputs": 1,
        "timeout": "",
        "noerr": 0,
        "initialize": "",
        "finalize": "",
        "libs": [],
        "x": 540,
        "y": 560,
        "wires": [
            [
                "mqtt_out_action_reply"
            ]
        ]
    },
    {
        "id": "3ab8b165edc077f2",
        "type": "function",
        "z": "3d75251ef0c130f7",
        "name": "Process Restart",
        "func": "// Extract the private channel sent by the client\nconst replyChannel = msg.payload.sessionChannel;\n\n// Execute your arbitrary logic here (e.g., API calls, reading sensors)\nconst systemData = \"RESTART\";\n\n// Route the response back to the specific client session\nmsg.topic = replyChannel;\nmsg.payload = {\n    messageType: \"PanelUpdate\",\n    data: {\n        targetPanel: \"commandProcessor\",\n        content: systemData\n    }\n};\nreturn msg;",
        "outputs": 1,
        "timeout": "",
        "noerr": 0,
        "initialize": "",
        "finalize": "",
        "libs": [],
        "x": 530,
        "y": 600,
        "wires": [
            [
                "mqtt_out_action_reply"
            ]
        ]
    },
    {
        "id": "7a585de1b22c886e",
        "type": "debug",
        "z": "3d75251ef0c130f7",
        "name": "debug 1",
        "active": true,
        "tosidebar": true,
        "console": false,
        "tostatus": false,
        "complete": "false",
        "statusVal": "",
        "statusType": "auto",
        "x": 740,
        "y": 80,
        "wires": []
    },
    {
        "id": "7e17443609a729b4",
        "type": "comment",
        "z": "3d75251ef0c130f7",
        "name": "Startup",
        "info": "When an app first launches, it sends a message on this topic and awaits commands.",
        "x": 150,
        "y": 60,
        "wires": []
    },
    {
        "id": "mqtt_in_session",
        "type": "mqtt in",
        "z": "3d75251ef0c130f7",
        "name": "Listen to Session Channel",
        "topic": "pi-console/session/#",
        "qos": "0",
        "datatype": "json",
        "broker": "26de6532b290a5e4",
        "nl": false,
        "rap": false,
        "inputs": 0,
        "x": 230,
        "y": 680,
        "wires": [
            [
                "func_extract_client"
            ]
        ]
    },
    {
        "id": "func_extract_client",
        "type": "function",
        "z": "3d75251ef0c130f7",
        "name": "Extract Client ID",
        "func": "// Extract topic parts: pi-console/session/setup/{clientId}/{uuid}\nconst parts = msg.topic.split('/');\nmsg.clientId = parts[3] || \"default\";\n\n// Save the original payload status (READY or UI_READY)\nmsg.clientState = msg.payload.status;\n\nreturn msg;",
        "outputs": 1,
        "timeout": "",
        "noerr": 0,
        "initialize": "",
        "finalize": "",
        "libs": [],
        "x": 450,
        "y": 680,
        "wires": [
            [
                "read_config_file"
            ]
        ]
    },
    {
        "id": "read_config_file",
        "type": "file in",
        "z": "3d75251ef0c130f7",
        "name": "Load configs.json",
        "filename": "/Users/YOUR_USERNAME/.node-red/pi-console-configs.json",
        "filenameType": "str",
        "format": "utf8",
        "chunk": false,
        "sendError": false,
        "encoding": "none",
        "allProps": false,
        "x": 650,
        "y": 680,
        "wires": [
            [
                "parse_json"
            ]
        ]
    },
    {
        "id": "parse_json",
        "type": "json",
        "z": "3d75251ef0c130f7",
        "name": "Parse JSON",
        "property": "payload",
        "action": "obj",
        "pretty": false,
        "x": 830,
        "y": 680,
        "wires": [
            [
                "switch_state_router"
            ]
        ]
    },
    {
        "id": "switch_state_router",
        "type": "switch",
        "z": "3d75251ef0c130f7",
        "name": "Route State",
        "property": "clientState",
        "propertyType": "msg",
        "rules": [
            {
                "t": "eq",
                "v": "READY",
                "vt": "str"
            },
            {
                "t": "eq",
                "v": "UI_READY",
                "vt": "str"
            }
        ],
        "checkall": "true",
        "repair": false,
        "outputs": 2,
        "x": 230,
        "y": 780,
        "wires": [
            [
                "func_build_ui"
            ],
            [
                "func_build_menu"
            ]
        ]
    },
    {
        "id": "func_build_ui",
        "type": "function",
        "z": "3d75251ef0c130f7",
        "name": "Serve Targeted UI Config",
        "func": "const configs = msg.payload;\nconst clientId = msg.clientId;\n\n// Fallback to default if client ID isn't in the JSON file\nconst clientConfig = configs[clientId] || configs[\"default\"];\n\nmsg.payload = {\n    messageType: \"UiConfig\",\n    data: clientConfig.uiConfig\n};\n\n// msg.topic is already the correct private session channel\nreturn msg;",
        "outputs": 1,
        "timeout": "",
        "noerr": 0,
        "initialize": "",
        "finalize": "",
        "libs": [],
        "x": 450,
        "y": 760,
        "wires": [
            [
                "mqtt_out_session"
            ]
        ]
    },
    {
        "id": "func_build_menu",
        "type": "function",
        "z": "3d75251ef0c130f7",
        "name": "Serve Targeted Menu",
        "func": "const configs = msg.payload;\nconst clientId = msg.clientId;\n\n// Fallback to default if client ID isn't in the JSON file\nconst clientConfig = configs[clientId] || configs[\"default\"];\n\nmsg.payload = {\n    messageType: \"Menu\",\n    data: clientConfig.menu\n};\n\n// msg.topic is already the correct private session channel\nreturn msg;",
        "outputs": 1,
        "timeout": "",
        "noerr": 0,
        "initialize": "",
        "finalize": "",
        "libs": [],
        "x": 440,
        "y": 820,
        "wires": [
            [
                "mqtt_out_session"
            ]
        ]
    },
    {
        "id": "mqtt_out_session",
        "type": "mqtt out",
        "z": "3d75251ef0c130f7",
        "name": "Publish to Session",
        "topic": "",
        "qos": "0",
        "retain": "false",
        "respTopic": "",
        "contentType": "",
        "userProps": "",
        "correl": "",
        "expiry": "",
        "broker": "26de6532b290a5e4",
        "x": 710,
        "y": 780,
        "wires": []
    },
    {
        "id": "26de6532b290a5e4",
        "type": "mqtt-broker",
        "name": "Local MQTT",
        "broker": "mosquitto",
        "port": 1883,
        "clientid": "",
        "autoConnect": true,
        "usetls": false,
        "protocolVersion": "5",
        "keepalive": 60,
        "cleansession": true,
        "autoUnsubscribe": true,
        "birthTopic": "",
        "birthQos": "0",
        "birthRetain": "false",
        "birthPayload": "",
        "birthMsg": {},
        "closeTopic": "",
        "closeQos": "0",
        "closeRetain": "false",
        "closePayload": "",
        "closeMsg": {},
        "willTopic": "",
        "willQos": "0",
        "willRetain": "false",
        "willPayload": "",
        "willMsg": {},
        "userProps": "",
        "sessionExpiry": ""
    }
]```