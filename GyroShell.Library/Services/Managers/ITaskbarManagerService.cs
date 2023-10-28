﻿namespace GyroShell.Library.Services.Managers
{
    /// <summary>
    /// Defines a service interface for managing the Windows taskbar.
    /// </summary>
    public interface ITaskbarManagerService
    {
        /// <summary>
        /// Initializes the service's public components.
        /// </summary>
        public void Initialize();

        /// <summary>
        /// Shows the taskbar.
        /// </summary>
        public void ShowTaskbar();

        /// <summary>
        /// Hides the taskbar.
        /// </summary>
        public void HideTaskbar();

        /// <summary>
        /// Toggles the Windows start menu.
        /// </summary>
        public void ToggleStartMenu();

        /// <summary>
        /// Toggles the Windows control center.
        /// </summary>
        /// <remarks>Only works under Windows 11.</remarks>
        public void ToggleControlCenter();

        /// <summary>
        /// Toggles the Windows action center.
        /// </summary>
        /// <remarks>Only works under Windows 10.</remarks>
        public void ToggleActionCenter();

        /// <summary>
        /// Toggles auto hiding for the Windows taskbar.
        /// </summary>
        /// <param name="doHide">If the taskbar should auto-hide or not.</param>
        public void ToggleAutoHideExplorer(bool doHide);

        /// <summary>
        /// Notifies winlogon that the shell has already loaded, hiding the login screen.
        /// </summary>
        public void NotifyWinlogonShowShell();
    }
}