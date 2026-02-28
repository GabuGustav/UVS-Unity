using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    public class MultiplayerModule : VehicleEditorModuleBase
    {
        private Toggle _allowMultiple;
        private Toggle _allowPassengerInput;
        private IntegerField _maxOccupants;
        private FloatField _enterDistance;
        private FloatField _exitDistance;
        private FloatField _swapCooldown;
        private Toggle _enableLocalSplitScreen;
        private IntegerField _localMaxPlayers;
        private Toggle _allowSharedVehicleOccupancy;
        private Toggle _enableOnline;
        private TextField _onlineProviderId;
        private Label _backendStatus;

        private VisualElement _seatList;
        private Button _addSeatButton;
        private Button _autoGenerateButton;
        private Button _clearSeatsButton;

        public override string ModuleId => "multiplayer";
        public override string DisplayName => "Multiplayer";
        public override int Priority => 17;
        public override bool RequiresVehicle => true;

        protected override VisualElement CreateModuleUI()
        {
            var container = new VisualElement
            {
                style =
                {
                    paddingLeft = 20,
                    paddingRight = 20,
                    paddingTop = 20,
                    paddingBottom = 20
                }
            };

            var header = new Label("Multiplayer / Seats")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
            };
            container.Add(header);

            container.Add(new Label("Multiplayer Foundation")
            {
                style = { fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 }
            });

            _enableLocalSplitScreen = new Toggle("Enable Local Split Screen");
            _localMaxPlayers = new IntegerField("Local Max Players");
            _allowSharedVehicleOccupancy = new Toggle("Allow Shared Vehicle Occupancy");
            _enableOnline = new Toggle("Enable Online");
            _onlineProviderId = new TextField("Online Provider Id");
            _backendStatus = new Label();

            RegisterMultiplayerCallbacks();

            container.Add(_enableLocalSplitScreen);
            container.Add(_localMaxPlayers);
            container.Add(_allowSharedVehicleOccupancy);
            container.Add(_enableOnline);
            container.Add(_onlineProviderId);
            container.Add(_backendStatus);

            var settingsHeader = new Label("Seat Settings")
            {
                style = { fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 }
            };
            container.Add(settingsHeader);

            _allowMultiple = new Toggle("Allow Multiple Occupants");
            _allowPassengerInput = new Toggle("Allow Passenger Input");
            _maxOccupants = new IntegerField("Max Occupants");
            _enterDistance = new FloatField("Enter Distance");
            _exitDistance = new FloatField("Exit Distance");
            _swapCooldown = new FloatField("Seat Swap Cooldown");

            RegisterSettingCallbacks();

            container.Add(_allowMultiple);
            container.Add(_allowPassengerInput);
            container.Add(_maxOccupants);
            container.Add(_enterDistance);
            container.Add(_exitDistance);
            container.Add(_swapCooldown);

            container.Add(new Label("Seats")
            {
                style = { fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 12 }
            });

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 6 } };
            _addSeatButton = new Button(AddSeat) { text = "Add Seat" };
            _autoGenerateButton = new Button(AutoGenerateSeats) { text = "Auto-Generate Seats" };
            _clearSeatsButton = new Button(ClearSeats) { text = "Clear Seats" };
            buttonRow.Add(_addSeatButton);
            buttonRow.Add(_autoGenerateButton);
            buttonRow.Add(_clearSeatsButton);
            container.Add(buttonRow);

            _seatList = new ScrollView { style = { height = 280 } };
            container.Add(_seatList);

            var tip = new Label("Tip: Use Top-Down View in the preview. Drag seat gizmos to move. Hold Shift to rotate.")
            {
                style = { marginTop = 8, unityFontStyleAndWeight = FontStyle.Italic, opacity = 0.7f }
            };
            container.Add(tip);

            return container;
        }

        protected override void OnModuleActivated()
        {
            RefreshFromConfig();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            RefreshFromConfig();
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null)
                return ValidationResult.Warning("No vehicle config loaded.");

            var cfg = _context.CurrentConfig;
            if (cfg.seats == null || cfg.seats.Count == 0)
                return ValidationResult.Warning("No seats defined.");

            if (cfg.seatSettings.maxOccupants < 1)
                return ValidationResult.Error("Max occupants must be at least 1.");

            if (!cfg.seats.Any(s => s != null && s.role == VehicleConfig.SeatRole.Driver))
                return ValidationResult.Error("At least one Driver seat is required.");

            if (cfg.multiplayer.localMaxPlayers < 1 || cfg.multiplayer.localMaxPlayers > 4)
                return ValidationResult.Warning("Local max players should be within 1-4.");

            if (cfg.multiplayer.enableOnline)
            {
                if (string.IsNullOrWhiteSpace(cfg.multiplayer.onlineProviderId))
                    return ValidationResult.Warning("Online provider id is empty.");

                if (string.Equals(cfg.multiplayer.onlineProviderId, "ngo", StringComparison.OrdinalIgnoreCase) &&
                    !IsNgoAvailable())
                {
                    return ValidationResult.Warning("NGO backend selected but Netcode for GameObjects package is not available.");
                }
            }

            if (_context.SelectedPrefab != null && TryGetPrefabBounds(_context.SelectedPrefab, out var bounds))
            {
                var localCenter = _context.SelectedPrefab.transform.InverseTransformPoint(bounds.center);
                var localExtents = bounds.extents;
                foreach (var seat in cfg.seats)
                {
                    if (seat == null) continue;
                    var delta = seat.localPosition - localCenter;
                    if (Mathf.Abs(delta.x) > localExtents.x * 1.2f ||
                        Mathf.Abs(delta.y) > localExtents.y * 1.2f ||
                        Mathf.Abs(delta.z) > localExtents.z * 1.2f)
                    {
                        return ValidationResult.Warning("Some seat anchors are outside the prefab bounds.");
                    }
                }
            }

            return ValidationResult.Success();
        }

        private void RegisterMultiplayerCallbacks()
        {
            _enableLocalSplitScreen.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig(() => _context.CurrentConfig.multiplayer.enableLocalSplitScreen = evt.newValue, false);
            });
            _localMaxPlayers.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig(() => _context.CurrentConfig.multiplayer.localMaxPlayers = Mathf.Clamp(evt.newValue, 1, 4), false);
            });
            _allowSharedVehicleOccupancy.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig(() => _context.CurrentConfig.multiplayer.allowSharedVehicleOccupancy = evt.newValue, false);
            });
            _enableOnline.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig(() => _context.CurrentConfig.multiplayer.enableOnline = evt.newValue, false);
                UpdateBackendStatus();
            });
            _onlineProviderId.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig(() => _context.CurrentConfig.multiplayer.onlineProviderId = evt.newValue, false);
                UpdateBackendStatus();
            });
        }

        private void RegisterSettingCallbacks()
        {
            _allowMultiple.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig(() => _context.CurrentConfig.seatSettings.allowMultipleOccupants = evt.newValue, false);
            });
            _allowPassengerInput.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig(() => _context.CurrentConfig.seatSettings.allowPassengerInput = evt.newValue, false);
            });
            _maxOccupants.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig(() => _context.CurrentConfig.seatSettings.maxOccupants = Mathf.Max(1, evt.newValue), false);
            });
            _enterDistance.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig(() => _context.CurrentConfig.seatSettings.enterDistance = Mathf.Max(0f, evt.newValue), false);
            });
            _exitDistance.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig(() => _context.CurrentConfig.seatSettings.exitDistance = Mathf.Max(0f, evt.newValue), false);
            });
            _swapCooldown.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig(() => _context.CurrentConfig.seatSettings.seatSwapCooldown = Mathf.Max(0f, evt.newValue), false);
            });
        }

        private void RefreshFromConfig()
        {
            if (_context?.CurrentConfig == null)
                return;

            var cfg = _context.CurrentConfig;
            _allowMultiple.SetValueWithoutNotify(cfg.seatSettings.allowMultipleOccupants);
            _allowPassengerInput.SetValueWithoutNotify(cfg.seatSettings.allowPassengerInput);
            _maxOccupants.SetValueWithoutNotify(cfg.seatSettings.maxOccupants);
            _enterDistance.SetValueWithoutNotify(cfg.seatSettings.enterDistance);
            _exitDistance.SetValueWithoutNotify(cfg.seatSettings.exitDistance);
            _swapCooldown.SetValueWithoutNotify(cfg.seatSettings.seatSwapCooldown);
            _enableLocalSplitScreen.SetValueWithoutNotify(cfg.multiplayer.enableLocalSplitScreen);
            _localMaxPlayers.SetValueWithoutNotify(cfg.multiplayer.localMaxPlayers);
            _allowSharedVehicleOccupancy.SetValueWithoutNotify(cfg.multiplayer.allowSharedVehicleOccupancy);
            _enableOnline.SetValueWithoutNotify(cfg.multiplayer.enableOnline);
            _onlineProviderId.SetValueWithoutNotify(cfg.multiplayer.onlineProviderId);
            UpdateBackendStatus();

            BuildSeatList();
        }

        private void BuildSeatList()
        {
            _seatList.Clear();
            if (_context?.CurrentConfig == null) return;

            var seats = _context.CurrentConfig.seats;
            if (seats == null || seats.Count == 0)
            {
                _seatList.Add(new Label("No seats defined."));
                return;
            }

            for (int i = 0; i < seats.Count; i++)
            {
                int index = i;
                var seat = seats[index];
                if (seat == null) continue;

                var box = new VisualElement
                {
                    style =
                    {
                        paddingLeft = 8,
                        paddingRight = 8,
                        paddingTop = 6,
                        paddingBottom = 6,
                        marginBottom = 6,
                        borderBottomWidth = 1,
                        borderTopWidth = 1,
                        borderLeftWidth = 1,
                        borderRightWidth = 1,
                        borderBottomColor = new Color(0.2f, 0.2f, 0.2f),
                        borderTopColor = new Color(0.2f, 0.2f, 0.2f),
                        borderLeftColor = new Color(0.2f, 0.2f, 0.2f),
                        borderRightColor = new Color(0.2f, 0.2f, 0.2f)
                    }
                };

                var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                headerRow.Add(new Label($"Seat {index + 1}") { style = { unityFontStyleAndWeight = FontStyle.Bold } });

                var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var upBtn = new Button(() => MoveSeat(index, -1)) { text = "Up" };
                var downBtn = new Button(() => MoveSeat(index, 1)) { text = "Down" };
                var removeBtn = new Button(() => RemoveSeat(index)) { text = "Remove" };
                btnRow.Add(upBtn);
                btnRow.Add(downBtn);
                btnRow.Add(removeBtn);
                headerRow.Add(btnRow);

                box.Add(headerRow);

                var idField = new TextField("Id") { value = seat.id };
                idField.RegisterValueChangedCallback(evt =>
                {
                    UpdateConfig(() => seat.id = evt.newValue);
                });
                box.Add(idField);

                var roleField = new EnumField("Role", seat.role);
                roleField.RegisterValueChangedCallback(evt =>
                {
                    UpdateConfig(() => seat.role = (VehicleConfig.SeatRole)evt.newValue);
                });
                box.Add(roleField);

                var posField = new Vector3Field("Local Position") { value = seat.localPosition };
                posField.RegisterValueChangedCallback(evt =>
                {
                    UpdateConfig(() => seat.localPosition = evt.newValue);
                });
                box.Add(posField);

                var rotField = new Vector3Field("Local Euler") { value = seat.localEuler };
                rotField.RegisterValueChangedCallback(evt =>
                {
                    UpdateConfig(() => seat.localEuler = evt.newValue);
                });
                box.Add(rotField);

                var overrideField = new ObjectField("Override Transform")
                {
                    objectType = typeof(Transform),
                    value = seat.overrideTransform
                };
                overrideField.RegisterValueChangedCallback(evt =>
                {
                    UpdateConfig(() => seat.overrideTransform = evt.newValue as Transform);
                });
                box.Add(overrideField);

                var entryField = new TextField("Entry Socket Id") { value = seat.entrySocketId };
                entryField.RegisterValueChangedCallback(evt =>
                {
                    UpdateConfig(() => seat.entrySocketId = evt.newValue, false);
                });
                box.Add(entryField);

                var exitField = new TextField("Exit Socket Id") { value = seat.exitSocketId };
                exitField.RegisterValueChangedCallback(evt =>
                {
                    UpdateConfig(() => seat.exitSocketId = evt.newValue, false);
                });
                box.Add(exitField);

                var snapBtn = new Button(() => SnapSeatToTop(index)) { text = "Snap To Surface" };
                box.Add(snapBtn);

                _seatList.Add(box);
            }
        }

        private void AddSeat()
        {
            if (_context?.CurrentConfig == null) return;
            var cfg = _context.CurrentConfig;
            cfg.seats ??= new List<VehicleConfig.SeatAnchor>();

            var seat = new VehicleConfig.SeatAnchor
            {
                id = $"seat_{cfg.seats.Count + 1}",
                role = cfg.seats.Count == 0 ? VehicleConfig.SeatRole.Driver : VehicleConfig.SeatRole.Passenger,
                localPosition = Vector3.zero,
                localEuler = Vector3.zero
            };
            cfg.seats.Add(seat);
            EditorUtility.SetDirty(cfg);
            _context.NotifyConfigChanged(cfg);
            BuildSeatList();
        }

        private void RemoveSeat(int index)
        {
            if (_context?.CurrentConfig == null) return;
            var cfg = _context.CurrentConfig;
            if (cfg.seats == null || index < 0 || index >= cfg.seats.Count) return;

            cfg.seats.RemoveAt(index);
            EditorUtility.SetDirty(cfg);
            _context.NotifyConfigChanged(cfg);
            BuildSeatList();
        }

        private void MoveSeat(int index, int delta)
        {
            if (_context?.CurrentConfig == null) return;
            var cfg = _context.CurrentConfig;
            if (cfg.seats == null) return;

            int newIndex = index + delta;
            if (newIndex < 0 || newIndex >= cfg.seats.Count) return;

            var seat = cfg.seats[index];
            cfg.seats.RemoveAt(index);
            cfg.seats.Insert(newIndex, seat);
            EditorUtility.SetDirty(cfg);
            _context.NotifyConfigChanged(cfg);
            BuildSeatList();
        }

        private void ClearSeats()
        {
            if (_context?.CurrentConfig == null) return;
            _context.CurrentConfig.seats.Clear();
            EditorUtility.SetDirty(_context.CurrentConfig);
            _context.NotifyConfigChanged(_context.CurrentConfig);
            BuildSeatList();
        }

        private void AutoGenerateSeats()
        {
            if (_context?.CurrentConfig == null) return;

            var cfg = _context.CurrentConfig;
            cfg.seats ??= new List<VehicleConfig.SeatAnchor>();
            cfg.seats.Clear();

            Vector3 center = Vector3.zero;
            Vector3 extents = Vector3.one * 0.5f;

            if (_context.SelectedPrefab != null && TryGetPrefabBounds(_context.SelectedPrefab, out var bounds))
            {
                center = _context.SelectedPrefab.transform.InverseTransformPoint(bounds.center);
                extents = bounds.extents;
            }

            cfg.seatSettings.allowMultipleOccupants = true;
            cfg.seatSettings.maxOccupants = Mathf.Max(cfg.seatSettings.maxOccupants, 2);

            cfg.seats.Add(new VehicleConfig.SeatAnchor
            {
                id = "driver",
                role = VehicleConfig.SeatRole.Driver,
                localPosition = center + new Vector3(-extents.x * 0.25f, extents.y * 0.2f, extents.z * 0.1f),
                localEuler = Vector3.zero
            });
            cfg.seats.Add(new VehicleConfig.SeatAnchor
            {
                id = "passenger_1",
                role = VehicleConfig.SeatRole.Passenger,
                localPosition = center + new Vector3(extents.x * 0.25f, extents.y * 0.2f, extents.z * 0.1f),
                localEuler = Vector3.zero
            });

            EditorUtility.SetDirty(cfg);
            _context.NotifyConfigChanged(cfg);
            BuildSeatList();
        }

        private void SnapSeatToTop(int index)
        {
            if (_context?.CurrentConfig == null) return;
            var cfg = _context.CurrentConfig;
            if (cfg.seats == null || index < 0 || index >= cfg.seats.Count) return;

            if (_context.SelectedPrefab == null || !TryGetPrefabBounds(_context.SelectedPrefab, out var bounds))
                return;

            var seat = cfg.seats[index];
            var localPos = seat.localPosition;
            localPos.y = _context.SelectedPrefab.transform.InverseTransformPoint(bounds.max).y - 0.1f;
            seat.localPosition = localPos;

            EditorUtility.SetDirty(cfg);
            _context.NotifyConfigChanged(cfg);
            BuildSeatList();
        }

        private bool TryGetPrefabBounds(GameObject prefab, out Bounds bounds)
        {
            bounds = new Bounds(Vector3.zero, Vector3.one);
            if (prefab == null) return false;

            var path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path)) return false;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) return false;

                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private void UpdateConfig(System.Action apply, bool notifyPreview = true)
        {
            if (_context?.CurrentConfig == null) return;
            apply?.Invoke();
            EditorUtility.SetDirty(_context.CurrentConfig);
            if (notifyPreview)
                _context.NotifyConfigChanged(_context.CurrentConfig);
        }

        private void UpdateBackendStatus()
        {
            if (_context?.CurrentConfig == null || _backendStatus == null) return;

            if (!_context.CurrentConfig.multiplayer.enableOnline)
            {
                _backendStatus.text = "Backend: Local-only";
                _backendStatus.style.color = Color.green;
                return;
            }

            string provider = _context.CurrentConfig.multiplayer.onlineProviderId;
            if (string.Equals(provider, "ngo", StringComparison.OrdinalIgnoreCase))
            {
                bool available = IsNgoAvailable();
                _backendStatus.text = available
                    ? "Backend: NGO available"
                    : "Backend: NGO package missing";
                _backendStatus.style.color = available ? Color.green : Color.yellow;
                return;
            }

            _backendStatus.text = $"Backend: {provider} (custom)";
            _backendStatus.style.color = Color.yellow;
        }

        private static bool IsNgoAvailable()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Any(a => string.Equals(a.GetName().Name, "Unity.Netcode.Runtime", StringComparison.OrdinalIgnoreCase));
        }
    }
}
