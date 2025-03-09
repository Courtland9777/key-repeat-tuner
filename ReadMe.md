# StarCraft Key Manager
Are you tired of constantly adjusting your keyboard settings every time you play StarCraft II? Many players manually adjust their key repeat speed and delay to optimize their gameplay, only to reset them afterward—or worse, live with inconveniently fast settings when they're not playing. **StarCraft Key Manager** is here to solve this hassle and make your gaming experience smoother and more convenient.
Inspired by PiG's *Bronze to GM* series, this application automates the process of managing your key repeat settings, so you can focus on enjoying the game without worrying about tedious adjustments.
**What does StarCraft Key Manager do?**
- **Automatic Key Repeat Adjustment**: The app dynamically applies your preferred key repeat speed and delay settings whenever StarCraft II is running. No more manual changes before and after every session!
- **Seamless Integration**: When you're done playing, StarCraft Key Manager automatically restores your default keyboard settings, ensuring your system behaves as expected outside of the game.
- **Convenience and Simplicity**: With StarCraft Key Manager, you can set it and forget it. The app runs in the background, monitoring the game process and applying the right settings at the right time.
**Why use StarCraft Key Manager?**
While StarCraft Key Manager doesn't directly improve your gameplay mechanics, it eliminates a common annoyance for players who frequently adjust their keyboard settings. By automating this process, the app saves you time and effort, letting you focus on what really matters—playing StarCraft II.
Whether you're a casual player or a competitive ladder grinder, StarCraft Key Manager ensures your keyboard is always optimized for the game without disrupting your workflow or daily computer use. It's a small but impactful quality-of-life improvement for any StarCraft II enthusiast.
**Say goodbye to manual adjustments and hello to hassle-free gaming with StarCraft Key Manager!**

# Setup and Installation Instructions
Follow these steps to install and configure **StarCraft Key Manager** on your system. This guide will help you set up the application to run on startup with administrator privileges as a background service and configure it to your preferences.
---
## Step 1: Download the Application
1. Go to the [Releases](https://github.com/Courtland9777/StarCraftKeyManager/releases) page of the GitHub repository.
2. Download the latest release `.zip` file (e.g., `StarCraftKeyManager-v1.0.0.zip`) from the **Assets** section.
3. Extract the contents of the `.zip` file to a folder on your computer (e.g., `C:\StarCraftKeyManager`).
---
## Step 2: Modify the `appsettings.json` File
1. Open the extracted folder and locate the `appsettings.json` file.
2. Open the file in a text editor (e.g., Notepad or Visual Studio Code).      
3. Modify the settings to your desired values. For example:
```json
{
  "ProcessMonitor": {
    "ProcessName": "starcraft.exe"
  },
  "KeyRepeat": {
    "Default": {
      "RepeatSpeed": 31,
      "RepeatDelay": 1000
    },
    "FastMode": {
      "RepeatSpeed": 20,
      "RepeatDelay": 500
    }
  }
}
```
   - **ProcessName**: The name of the StarCraft II process (default: `starcraft.exe`).
   - **KeyRepeat Settings**:
     - `RepeatSpeed`: Adjust the speed of key repeats (0–31).
     - `RepeatDelay`: Adjust the delay before key repeats (250–1000 ms).
4. Save the file after making your changes.
---
## Step 3: Set Up the Application to Run on Startup
To ensure the application starts automatically with administrator privileges:
### Option 1: Using Task Scheduler
1. Press `Win + S` and search for **Task Scheduler**. Open it.
2. Click **Create Task** in the right-hand menu.
3. In the **General** tab:
   - Enter a name for the task (e.g., `StarCraft Key Manager`).
   - Check **Run with highest privileges**.
   - Select **Windows 11** under **Configure for**.
4. In the **Triggers** tab:
   - Click **New**.
   - Set the trigger to **At log on** and click **OK**.
5. In the **Actions** tab:
   - Click **New**.
   - Set the action to **Start a program**.
   - Browse to the location of `StarCraftKeyManager.exe` (e.g., `C:\StarCraftKeyManager\StarCraftKeyManager.exe`).
   - Click **OK**.
6. In the **Conditions** tab:
   - Uncheck **Start the task only if the computer is on AC power** (optional).
7. Click **OK** to save the task.
### Option 2: Using a Shortcut in the Startup Folder
1. Right-click on `StarCraftKeyManager.exe` and select **Create Shortcut**.
2. Press `Win + R`, type `shell:startup`, and press Enter to open the Startup folder.
3. Move the shortcut to the Startup folder.
4. Right-click the shortcut in the Startup folder, select **Properties**, and:
   - Go to the **Shortcut** tab.
   - Click **Advanced**.
   - Check **Run as administrator** and click **OK**.
---
## Step 4: Run the Application
1. Start the application manually for the first time by double-clicking `StarCraftKeyManager.exe`.
2. Verify that the application is running in the background (check the Task Manager or logs for confirmation).
---
## Step 5: Verify Functionality
1. Launch StarCraft II and ensure that the key repeat settings are applied as configured in `appsettings.json`.
2. Exit the game and verify that the default key repeat settings are restored.
3. To compare the behavior, you can manually check your key repeat settings in Windows 11:
   - Press `Win + S` and search for **Control Panel**. Open it.
   - Navigate to **Hardware and Sound > Devices and Printers**.
   - Right-click on your keyboard and select **Keyboard Settings**.
   - Adjust the **Repeat delay** and **Repeat rate** sliders to see how they affect your keyboard behavior.
   - Note: StarCraft Key Manager automates these adjustments for you, so you don't need to change them manually every time you play.
---
## Notes
- If you encounter any issues, check the application logs for error messages.
- You can update the `appsettings.json` file at any time to adjust the settings.
Enjoy a hassle-free gaming experience with **StarCraft Key Manager**!
---
## Contributing
Contributions, bug reports, and feature requests are welcome! Feel free to open an issue or submit a pull request to help improve StarCraft Key Manager.



