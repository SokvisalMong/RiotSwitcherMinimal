# RiotSwitcherMinimal

## Description
A lightweight, system-tray application for quickly switching between Riot Games accounts without relogging.  
## Description
A lightweight, system-tray application for quickly switching between Riot Games accounts without relogging.  
It works by swapping the Riot Client's configuration files (`RiotGamesPrivateSettings.yaml`) and persisting session states per profile.

*Inspired by [RiotSwitcher](https://github.com/arthiee4/RiotSwitcher).*

## Usage
To register a new account:
- In the system tray, Create new profile
- Login to account on Riot Client (enable "Stay signed in")

**Don't log out of the account on Riot Client after logging in**

To switch between accounts:
- Click on the profile you want to switch to in the system tray
- Riot Client will close and reopen with the selected account

## Development
*   **Language:** C#
*   **Framework:** .NET Framework 4.8

## Build
1.  Open `RiotSwitcherMinimal.sln` in **Visual Studio 2022**.
2.  Select **Release** configuration.
3.  **Build Solution** (Ctrl+Shift+B).