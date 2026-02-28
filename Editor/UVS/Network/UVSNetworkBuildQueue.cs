using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UVS.Editor.Network
{
    public readonly struct UVSNetworkBuildTask
    {
        public readonly string Label;
        public readonly Action Action;

        public UVSNetworkBuildTask(string label, Action action)
        {
            Label = label;
            Action = action;
        }
    }

    public static class UVSNetworkBuildQueue
    {
        private static readonly Queue<UVSNetworkBuildTask> Tasks = new();
        private static string _title = "UVS Build";
        private static int _total;
        private static int _completed;
        private static bool _cancelled;
        private static Action<bool> _onFinished;
        private static bool _isSubscribed;
        private const int TasksPerUpdate = 1;

        public static bool IsRunning => _isSubscribed;

        public static void Start(string title, IEnumerable<UVSNetworkBuildTask> tasks, Action<bool> onFinished = null)
        {
            Cancel();

            _title = string.IsNullOrWhiteSpace(title) ? "UVS Build" : title;
            _onFinished = onFinished;
            _cancelled = false;
            _completed = 0;
            Tasks.Clear();

            if (tasks != null)
            {
                foreach (var task in tasks)
                {
                    if (task.Action != null)
                        Tasks.Enqueue(task);
                }
            }

            _total = Tasks.Count;
            if (_total == 0)
            {
                _onFinished?.Invoke(true);
                _onFinished = null;
                return;
            }

            EditorApplication.update += ProcessQueue;
            _isSubscribed = true;
        }

        public static void Cancel()
        {
            if (!_isSubscribed)
            {
                Tasks.Clear();
                _onFinished = null;
                return;
            }

            _cancelled = true;
            Finish(false);
        }

        private static void ProcessQueue()
        {
            if (!_isSubscribed)
                return;

            string label = Tasks.Count > 0 ? Tasks.Peek().Label : "Finalizing";
            float progress = _total > 0 ? Mathf.Clamp01((float)_completed / _total) : 1f;
            if (EditorUtility.DisplayCancelableProgressBar(_title, label, progress))
            {
                _cancelled = true;
                Finish(false);
                return;
            }

            int processedThisTick = 0;
            while (Tasks.Count > 0 && processedThisTick < TasksPerUpdate)
            {
                var task = Tasks.Dequeue();
                try
                {
                    task.Action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UVSNetworkBuildQueue] Task failed '{task.Label}': {ex}");
                }

                _completed++;
                processedThisTick++;
            }

            if (Tasks.Count == 0)
                Finish(!_cancelled);
        }

        private static void Finish(bool success)
        {
            EditorUtility.ClearProgressBar();

            if (_isSubscribed)
            {
                EditorApplication.update -= ProcessQueue;
                _isSubscribed = false;
            }

            Tasks.Clear();
            var finished = _onFinished;
            _onFinished = null;
            _total = 0;
            _completed = 0;
            _cancelled = false;

            finished?.Invoke(success);
        }
    }
}
