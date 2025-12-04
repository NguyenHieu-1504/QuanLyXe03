using System;

namespace QuanLyXe03.Helpers
{
    /// <summary>
    /// KZTEK Protocol Commands for KZ-E02.NET V3.0
    /// All commands follow format: STX + Command + ETX (added by UdpTools)
    /// </summary>
    public static class KZTEK_CMD
    {
        // ==================== BASIC COMMANDS ====================

        /// <summary>
        /// Get event from device (polling)
        /// Response: Card event, Input event, or NotEvent
        /// </summary>
        public static string GetEvent() => "GetEvent?/";

        /// <summary>
        /// Delete event from device memory
        /// Response: DeleteEvent?/OK or DeleteEvent?/ERR
        /// </summary>
        public static string DeleteEvent() => "DeleteEvent?/";

        /// <summary>
        /// Get firmware version
        /// Response: GetFirmwareVersion?/Version=KZ_E02.NET_V3.0
        /// </summary>
        public static string GetFirmwareVersion() => "GetFirmwareVersion?/";

        // ==================== MODE COMMANDS ====================

        /// <summary>
        /// Get current mode
        /// Response: GetMode?/Mode=1
        /// </summary>
        public static string GetMode() => "GetMode?/";

        /// <summary>
        /// Set device mode
        /// Mode=1: iParking mode, no card check, event save to ROM
        /// Mode=2: Access mode, card check, event save to RAM
        /// Mode=3: Access mode, card check, event save to ROM
        /// Mode=4: iParking mode, no card check, event save to RAM (recommended for parking)
        /// Mode=5: RS485 ZKteco QR500/QR600 mode
        /// </summary>
        public static string SetMode(int mode) => $"SetMode?/Mode={mode}";

        // ==================== RELAY COMMANDS ====================

        /// <summary>
        /// Set relay state
        /// Relay: 01-04
        /// State: ON or OFF
        /// Response: SetRelay?/Relay=01/OK or SetRelay?/Relay=01/ERR
        /// </summary>
        public static string SetRelay(int relay, string state) => $"SetRelay?/Relay={relay:D2}/State={state.ToUpper()}";

        /// <summary>
        /// Set relay delay time (auto close after ON)
        /// Time: 100 - 120000 ms (0.1s - 120s)
        /// Response: SetRelayDelayTime?/OK or SetRelayDelayTime?/ERR
        /// </summary>
        public static string SetRelayDelayTime(int timeMs) => $"SetRelayDelayTime?/Time={timeMs}";

        /// <summary>
        /// Set alarm delay time (Relay 3,4)
        /// Time: 100 - 3000000 ms (0.1s - 300s)
        /// Response: SetAlarmDelayTime?/OK or SetAlarmDelayTime?/ERR
        /// </summary>
        public static string SetAlarmDelayTime(int timeMs) => $"SetAlarmDelayTime?/Time={timeMs}";

        // ==================== USER/CARD COMMANDS ====================

        /// <summary>
        /// Download user card to device
        /// </summary>
        /// <param name="userId">Memory position (1-15000)</param>
        /// <param name="card">Card UID (4 or 7 bytes hex)</param>
        /// <param name="lenCard">Card length (4 or 7 bytes)</param>
        /// <param name="pin">8-digit PIN</param>
        /// <param name="timezone">Timezone ID (1-5)</param>
        /// <param name="door">Door permission: 00=none, 01=door1, 02=door2, 03=both</param>
        public static string DownloadUser(int userId, string card, int lenCard, string pin, int timezone, string door)
            => $"DownloadUser?/UserID={userId}/LenCard={lenCard}/Card={card}/Pin={pin}/Mode=0/TimeZone={timezone}/Door={door}";

        /// <summary>
        /// Delete user from device
        /// Response: DeleteUser?/OK or DeleteUser?/ERR
        /// </summary>
        public static string DeleteUser(int userId) => $"DeleteUser?/UserID={userId}";

        /// <summary>
        /// Get user info from device
        /// Response: GetUser?/UserID=1/LenCard=4/Card=7C19F640/... or GetUser?/UserID=NULL
        /// </summary>
        public static string GetUser(int userId) => $"GetUser?/UserID={userId}";

        /// <summary>
        /// Get all users (must call multiple times until UserID=NULL)
        /// Response: GetAllUser?/UserID=1/... or GetAllUser?/UserID=NULL (end)
        /// </summary>
        public static string GetAllUser() => "GetAllUser?/";

        // ==================== INPUT COMMANDS ====================

        /// <summary>
        /// Get input state (EXIT1, EXIT2, MSG1, MSG2)
        /// Response: GetInputState?/InputState=09 (hex bitmap)
        /// </summary>
        public static string GetInputState() => "GetInputState?/";

        // ==================== DATETIME COMMANDS ====================

        /// <summary>
        /// Get device date time
        /// Response: GetDateTime?/YYYYMMDDhhmmss
        /// </summary>
        public static string GetDateTime() => "GetDateTime?/";

        /// <summary>
        /// Set device date time
        /// Format: YYYYMMDDhhmmss
        /// Response: SetDateTime?/OK or SetDateTime?/ERR
        /// </summary>
        public static string SetDateTime(DateTime dateTime) => $"SetDateTime?/{dateTime:yyyyMMddHHmmss}";

        // ==================== TIMEZONE COMMANDS ====================

        /// <summary>
        /// Set timezone
        /// TZ: TZ1-TZ5
        /// Format: HHMM:hhmm (start:end)
        /// Example: SetTimeZone?/TZ2=0730:1800
        /// Response: SetTimeZone?/OK or SetTimeZone?/Err
        /// </summary>
        public static string SetTimeZone(int tzNumber, string startTime, string endTime)
            => $"SetTimeZone?/TZ{tzNumber}={startTime}:{endTime}";

        /// <summary>
        /// Get timezone
        /// Response: GetTimeZone?/TZ1=HHMM:hhmm
        /// </summary>
        public static string GetTimeZone(int tzNumber) => $"GetTimeZone?/TimeZone=TZ{tzNumber}";

        // ==================== ANTIPASSBACK COMMANDS ====================

        /// <summary>
        /// Set AntiPassBack mode
        /// AntiPassBackLock1/2: 1=enable, 0=disable
        /// Response: SetAntiPassBack?/OK
        /// </summary>
        public static string SetAntiPassBack(int lockNumber, bool enable)
            => $"SetAntiPassBack?/AntiPassBackLock{lockNumber}={(enable ? 1 : 0)}";

        /// <summary>
        /// Get AntiPassBack state
        /// Response: GetAntiPassBack?/AntiPassBackLock1=0
        /// </summary>
        public static string GetAntiPassBack(int lockNumber) => $"GetAntiPassBack?/AntiPassBackLock=Lock{lockNumber}";

        // ==================== EVENT MEMORY COMMANDS ====================

        /// <summary>
        /// Set max event count in ROM
        /// MaxEvent: 1000 - 100000
        /// Response: SetMaxEvent?/OK or SetMaxEvent?/ERR
        /// </summary>
        public static string SetMaxEvent(int maxEvent) => $"SetMaxEvent?/MaxEvent={maxEvent}";

        /// <summary>
        /// Get max event count
        /// Response: GetMaxEvent?/MaxEvent=1000
        /// </summary>
        public static string GetMaxEvent() => "GetMaxEvent?/";

        // ==================== NETWORK COMMANDS ====================

        /// <summary>
        /// Auto detect device on network
        /// Response: version/IP/Port/SubnetMask/Gateway/MAC
        /// </summary>
        public static string AutoDetect() => "AutoDetect?";

        /// <summary>
        /// Change IP address
        /// Response: ChangeIP?/OK or ChangeIP?/ERR
        /// </summary>
        public static string ChangeIP(string ip, string subnetMask, string gateway, string mac)
            => $"ChangeIP?/IP={ip}/SubnetMask={subnetMask}/DefaultGateWay={gateway}/HostMac={mac}/";

        /// <summary>
        /// Change MAC address
        /// Response: ChangeMacAddress?/OK or ChangeMacAddress?/ERR
        /// </summary>
        public static string ChangeMacAddress(string mac) => $"ChangeMacAddress?/Mac={mac}";

        // ==================== SYSTEM COMMANDS ====================

        /// <summary>
        /// Initialize card event memory (delete all)
        /// Response: InitCardEvent?/Initting → InitCardEvent?/InitComplete
        /// Device will restart after init
        /// </summary>
        public static string InitCardEvent() => "InitCardEvent?/";

        /// <summary>
        /// Initialize user memory (delete all users)
        /// Response: InitUserMemory?/Initting → InitUserMemory?/OK
        /// Device will restart after init
        /// </summary>
        public static string InitUserMemory() => "InitUserMemory?/";

        /// <summary>
        /// Initialize event memory (delete all events)
        /// Response: InitEventMemory?/Initting → InitEventMemory?/OK
        /// Device will restart after init
        /// </summary>
        public static string InitEventMemory() => "InitEventMemory?/";

        /// <summary>
        /// Reset to factory default
        /// Response: ResetDefault?/Reseting → ResetDefault?/ResetComplete
        /// Device will restart after reset
        /// </summary>
        public static string ResetDefault() => "ResetDefault?/";

        /// <summary>
        /// Restart device
        /// Response: ResetDevice?/Restarting
        /// </summary>
        public static string ResetDevice() => "ResetDevice?/";
    }
}