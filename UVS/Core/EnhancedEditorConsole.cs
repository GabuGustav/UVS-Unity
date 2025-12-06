using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UVS.Editor.Core
{
    /// <summary>
    /// Enhanced console system for the vehicle editor
    /// </summary>
    public class EnhancedEditorConsole
    {
        private readonly ScrollView _scrollView;
        private readonly List<LogEntry> _logEntries = new List<LogEntry>();
        private readonly int _maxLogEntries = 1000;
        private bool _autoScroll = true;
        
        public event Action<LogEntry> OnLogEntryAdded;
        
        public EnhancedEditorConsole(ScrollView scrollView)
        {
            _scrollView = scrollView ?? throw new ArgumentNullException(nameof(scrollView));
            SetupConsole();
        }
        
        private void SetupConsole()
        {
            _scrollView.Clear();
            _scrollView.style.flexGrow = 1;
            _scrollView.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            _scrollView.style.paddingLeft = 5;
            _scrollView.style.paddingRight = 5;
            _scrollView.style.paddingTop = 5;
            _scrollView.style.paddingBottom = 5;
        }
        
        public void LogMessage(string message, LogLevel level = LogLevel.Info)
        {
            var entry = new LogEntry
            {
                Message = message,
                Level = level,
                Timestamp = DateTime.Now
            };
            
            AddLogEntry(entry);
        }
        
        public void LogInfo(string message) => LogMessage(message, LogLevel.Info);
        public void LogWarning(string message) => LogMessage(message, LogLevel.Warning);
        public void LogError(string message) => LogMessage(message, LogLevel.Error);
        public void LogSuccess(string message) => LogMessage(message, LogLevel.Success);
        
        public void LogException(Exception exception, string context = "")
        {
            var message = string.IsNullOrEmpty(context) 
                ? $"Exception: {exception.Message}" 
                : $"{context}: {exception.Message}";
                
            LogError(message);
            
            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                LogError($"Stack Trace: {exception.StackTrace}");
            }
        }
        
        public void Clear()
        {
            _logEntries.Clear();
            _scrollView.Clear();
        }
        
        public void SetAutoScroll(bool enabled)
        {
            _autoScroll = enabled;
        }
        
        public List<LogEntry> GetLogEntries(LogLevel? level = null)
        {
            if (level.HasValue)
            {
                return _logEntries.Where(e => e.Level == level.Value).ToList();
            }
            return new List<LogEntry>(_logEntries);
        }
        
        public void ExportLogs(string filePath)
        {
            try
            {
                var lines = _logEntries.Select(e => $"[{e.Timestamp:HH:mm:ss}] {e.Level}: {e.Message}");
                System.IO.File.WriteAllLines(filePath, lines);
                LogSuccess($"Logs exported to: {filePath}");
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to export logs");
            }
        }
        
        private void AddLogEntry(LogEntry entry)
        {
            _logEntries.Add(entry);
            
            // Remove old entries if we exceed the limit
            while (_logEntries.Count > _maxLogEntries)
            {
                _logEntries.RemoveAt(0);
            }
            
            // Create UI element for the log entry
            var logElement = CreateLogElement(entry);
            _scrollView.Add(logElement);
            
            // Auto-scroll to bottom if enabled
            if (_autoScroll)
            {
                _scrollView.schedule.Execute(() =>
                {
                    _scrollView.scrollOffset = new Vector2(0, _scrollView.contentContainer.layout.height);
                });
            }
            
            OnLogEntryAdded?.Invoke(entry);
        }
        
        private VisualElement CreateLogElement(LogEntry entry)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginBottom = 2;
            
            // Timestamp
            var timestampLabel = new Label($"[{entry.Timestamp:HH:mm:ss}]");
            timestampLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            timestampLabel.style.fontSize = 10;
            timestampLabel.style.width = 60;
            container.Add(timestampLabel);
            
            // Level indicator
            var levelLabel = new Label($"{entry.Level}:");
            levelLabel.style.fontSize = 10;
            levelLabel.style.width = 60;
            levelLabel.style.color = GetLevelColor(entry.Level);
            container.Add(levelLabel);
            
            // Message
            var messageLabel = new Label(entry.Message);
            messageLabel.style.fontSize = 10;
            messageLabel.style.flexGrow = 1;
            messageLabel.style.color = GetLevelColor(entry.Level);
            container.Add(messageLabel);
            
            return container;
        }
        
        private Color GetLevelColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => Color.white,
                LogLevel.Warning => Color.yellow,
                LogLevel.Error => Color.red,
                LogLevel.Success => Color.green,
                _ => Color.white
            };
        }
    }
    
    /// <summary>
    /// Log entry data structure
    /// </summary>
    [Serializable]
    public class LogEntry
    {
        public string Message { get; set; }
        public LogLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Success
    }
}