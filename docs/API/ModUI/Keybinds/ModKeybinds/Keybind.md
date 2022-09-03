# Keybind

*Namespace: [ModUI.Keybinds](API/ModUI/Keybinds.md).[ModKeybinds](API/ModUI/Keybinds/ModKeybinds.md)*

### Public Variables

| Name     | Description             | Type                |
| -------- | ----------------------- | ------------------- |
| ID       | Your Keybind's ID       | <value v="string"/> |
| Name     | Your Keybind's Name     | <value v="string"/> |
| Key      | Your Keybind's Key      | <enum e="KeyCode"/> |
| Modifier | Your Keybind's Modifier | <enum e="KeyCode"/> |

### Public Functions

| Name                        | Description                         | Returns           |
| --------------------------- | ----------------------------------- | ----------------- |
| <method m="GetKeybind">     | Returns if Keybind is being Pressed | <value v="bool"/> |
| <method m="GetKeybindDown"> | Returns if Keybind was Pressed      | <value v="bool"/> |
| <method m="GetKeybindUp">   | Returns if Keybind was Released     | <value v="bool"/> |