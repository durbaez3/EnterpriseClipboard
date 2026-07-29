# User Guide - Enterprise Clipboard Manager

Welcome to **Enterprise Clipboard Manager**, a high-performance clipboard management utility.

## Key Bindings

| Hotkey | Action |
| :--- | :--- |
| **Ctrl + Shift + V** | Open the Quick Popup (at cursor location) |
| **Ctrl + Shift + H** | Open the main history management window |
| **Escape** (inside popup) | Close the Quick Popup |
| **Up / Down Arrow Keys** | Navigate between items |
| **Enter** (inside popup) | Paste selected item into the active application |

---

## Operating in Background (System Tray)

Upon closing the main window, the application automatically minimizes to the Windows System Tray (near the clock) to keep running in the background and logging clipboard updates.

Right-click the icon to open the quick actions menu:
- **Abrir Historial**: Displays the main search window.
- **Pausar Captura / Reanudar Captura**: Temporarily suspends/resumes clipboard monitoring.
- **Salir**: Fully shuts down the application.

---

## Filtering Sensitive Content
When you copy data matching sensitive regular expressions (passwords, JWT keys, credit cards), the manager will automatically flag it and encrypt it. The preview pane will show `[CONTENIDO SENSIBLE CIFRADO]` and prevent unauthorized shoulder-surfing.
