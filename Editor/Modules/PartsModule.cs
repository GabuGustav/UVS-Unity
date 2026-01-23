using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    public class PartsModule : VehicleEditorModuleBase
    {
        private VisualElement _partsContent;
        private Button _rescanButton;
        private Button _autoTagButton;
        private Button _saveButton;
        private Button _loadButton;

        public override string ModuleId => "parts";
        public override string DisplayName => "Parts";
        public override int Priority => 20;
        public override bool RequiresVehicle => true;
        public override bool RequiresSpecializedCategory => false;
        public override bool IsConstructionModule => false;
        public override bool IsTankModule => false;
        public override bool IsVTOLModule => false;

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

            var headerLabel = new Label("Vehicle Parts Classification")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 15
                }
            };
            container.Add(headerLabel);

            CreateControlButtons(container);

            _partsContent = new ScrollView
            {
                style =
                {
                    height = 400,
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                    paddingLeft = 20,
                    paddingTop = 20,
                    paddingBottom = 20
                }
            };
            container.Add(_partsContent);

            return container;
        }

        protected override void OnModuleActivated()
        {
            // Load or refresh module state
            if (_context?.CurrentConfig != null)
            {
                OnConfigChanged(_context.CurrentConfig);
            }
        }

        private void CreateControlButtons(VisualElement parent)
        {
            var buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    marginBottom = 15
                }
            };

            _rescanButton = new Button(RescanParts)
            {
                text = "Rescan Parts",
                style = { marginRight = 10, marginBottom = 5 }
            };
            buttonContainer.Add(_rescanButton);

            _autoTagButton = new Button(AutoTagParts)
            {
                text = "Auto-Tag Parts",
                style = { marginRight = 10, marginBottom = 5 }
            };
            buttonContainer.Add(_autoTagButton);

            _saveButton = new Button(SavePartClassifications)
            {
                text = "Save Classifications",
                style = { marginRight = 10, marginBottom = 5 }
            };
            buttonContainer.Add(_saveButton);

            _loadButton = new Button(LoadPartClassifications)
            {
                text = "Load Classifications",
                style = { marginRight = 10, marginBottom = 5 }
            };
            buttonContainer.Add(_loadButton);

            parent.Add(buttonContainer);
        }

        private void RescanParts()
        {
            if (_context?.SelectedPrefab == null)
            {
                LogError("No vehicle loaded. Please select a vehicle first.");
                return;
            }

            _partsContent.Clear();
            _context.LastScan.Clear();

            foreach (VehicleConfig.VehiclePartType partType in System.Enum.GetValues(typeof(VehicleConfig.VehiclePartType)))
            {
                _context.LastScan[partType] = new List<Transform>();
            }

            var lods = new List<Transform>();
            var colliders = new List<Transform>();

            foreach (Transform child in _context.SelectedPrefab.transform.GetComponentsInChildren<Transform>(true))
            {
                if (child == _context.SelectedPrefab.transform) continue;

                string lowerName = child.name.ToLowerInvariant();
                bool isLOD = lowerName.Contains("lod") || lowerName.Contains("transform");
                bool isCollider = lowerName.Contains("collider");

                if (isLOD) { lods.Add(child); continue; }
                if (isCollider) { colliders.Add(child); continue; }

                VehicleConfig.VehiclePartType foundType = ClassifyPart(child);
                _context.LastScan[foundType].Add(child);
            }

            DisplayScanResults(lods, colliders);

            int totalParts = _context.LastScan.Values.Sum(list => list.Count) + lods.Count + colliders.Count;
            LogMessage($"Parts scanned: {totalParts} parts found and classified.");
        }

        private VehicleConfig.VehiclePartType ClassifyPart(Transform part)
        {
            string lowerName = part.name.ToLowerInvariant();

            if (lowerName.Contains("steering") || lowerName.Contains("steer"))
                return VehicleConfig.VehiclePartType.SteeringWheel;
            else if (lowerName.Contains("wheel") || lowerName.Contains("tire") || lowerName.Contains("tyre"))
                return VehicleConfig.VehiclePartType.Wheel;
            else if (lowerName.Contains("spring") || lowerName.Contains("shock") || lowerName.Contains("damper"))
                return VehicleConfig.VehiclePartType.Suspension;
            else if (lowerName.Contains("engine"))
                return VehicleConfig.VehiclePartType.Engine;
            else if (lowerName.Contains("brake") || lowerName.Contains("caliper") || lowerName.Contains("calliper"))
                return VehicleConfig.VehiclePartType.Brake;
            else if (lowerName.Contains("light") || lowerName.Contains("lamp"))
                return VehicleConfig.VehiclePartType.Light;
            else if (lowerName.Contains("glass") || lowerName.Contains("window"))
                return VehicleConfig.VehiclePartType.Glass;
            else if (lowerName.Contains("exhaust") || lowerName.Contains("pipe"))
                return VehicleConfig.VehiclePartType.Exhaust;
            else if (lowerName.Contains("fuel") || lowerName.Contains("gas"))
                return VehicleConfig.VehiclePartType.FuelSystem;
            else if (lowerName.Contains("mirror"))
                return VehicleConfig.VehiclePartType.Mirror;
            else if (lowerName.Contains("interior") || lowerName.Contains("seat") || lowerName.Contains("passenger") || lowerName.Contains("driver"))
                return VehicleConfig.VehiclePartType.Interior;
            else if (lowerName.Contains("drivetrain") || lowerName.Contains("axle"))
                return VehicleConfig.VehiclePartType.Drivetrain;
            else if (lowerName.Contains("electrical") || lowerName.Contains("wire"))
                return VehicleConfig.VehiclePartType.Electrical;
            else if (lowerName.Contains("shifter") || lowerName.Contains("gear"))
                return VehicleConfig.VehiclePartType.Transmission;
            else if (lowerName.Contains("door"))
                return VehicleConfig.VehiclePartType.Door;
            else if (lowerName.Contains("turbo"))
                return VehicleConfig.VehiclePartType.Turbo;
            else
                return VehicleConfig.VehiclePartType.Body;
        }

        private void DisplayScanResults(List<Transform> lods, List<Transform> colliders)
        {
            if (lods.Count > 0)
            {
                var lodFoldout = new Foldout { text = $"LODs ({lods.Count})" };
                lodFoldout.AddToClassList("parts-foldout");
                foreach (var lod in lods)
                {
                    lodFoldout.Add(new Label(lod.name));
                }
                _partsContent.Add(lodFoldout);
                _context.LastScan[VehicleConfig.VehiclePartType.Miscellaneous].AddRange(lods);
            }

            if (colliders.Count > 0)
            {
                var colliderFoldout = new Foldout { text = $"Colliders ({colliders.Count})" };
                colliderFoldout.AddToClassList("parts-foldout");
                foreach (var collider in colliders)
                {
                    colliderFoldout.Add(new Label(collider.name));
                }
                _partsContent.Add(colliderFoldout);
                _context.LastScan[VehicleConfig.VehiclePartType.Miscellaneous].AddRange(colliders);
            }

            foreach (var kvp in _context.LastScan)
            {
                if (kvp.Value.Count == 0) continue;

                var foldout = new Foldout { text = $"{kvp.Key} ({kvp.Value.Count})" };
                foldout.AddToClassList("parts-foldout");

                foreach (var part in kvp.Value)
                {
                    var partRow = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center
                        }
                    };

                    var partLabel = new Label(part.name) { style = { flexGrow = 1 } };
                    partRow.Add(partLabel);

                    var reassignButton = new Button(() => ReassignPart(part, kvp.Key))
                    {
                        text = "Reassign",
                        style = { width = 80 }
                    };
                    partRow.Add(reassignButton);

                    foldout.Add(partRow);
                }

                _partsContent.Add(foldout);
            }
        }

        private void ReassignPart(Transform part, VehicleConfig.VehiclePartType currentType)
        {
            LogMessage($"Reassign functionality for {part.name} - would show dropdown in full implementation");
        }

        private void AutoTagParts()
        {
            LogMessage("Auto-tagging parts based on naming conventions...");
            RescanParts();
            LogMessage("Auto-tagging completed.");
        }

        private void SavePartClassifications()
        {
            if (_context?.CurrentConfig == null)
            {
                LogError("No VehicleConfig to save into.");
                return;
            }

            _context.CurrentConfig.partClassifications.Clear();

            foreach (var kvp in _context.LastScan)
            {
                foreach (var transform in kvp.Value)
                {
                    _context.CurrentConfig.partClassifications.Add(new VehicleConfig.VehiclePartClassification
                    {
                        partPath = GetTransformPath(transform),
                        partType = kvp.Key
                    });
                }
            }

            EditorUtility.SetDirty(_context.CurrentConfig);
            AssetDatabase.SaveAssets();

            LogMessage($"Saved {_context.CurrentConfig.partClassifications.Count} part classifications.");
        }

        private void LoadPartClassifications()
        {
            if (_context?.CurrentConfig == null || _context.SelectedPrefab == null)
            {
                LogError("No config or prefab loaded.");
                return;
            }

            _context.LastScan.Clear();
            foreach (VehicleConfig.VehiclePartType partType in System.Enum.GetValues(typeof(VehicleConfig.VehiclePartType)))
            {
                _context.LastScan[partType] = new List<Transform>();
            }

            foreach (var classification in _context.CurrentConfig.partClassifications)
            {
                Transform found = FindChildByPath(_context.SelectedPrefab.transform, classification.partPath);
                if (found != null)
                {
                    _context.LastScan[classification.partType].Add(found);
                }
                else
                {
                    LogError($"Could not find part at path '{classification.partPath}'");
                }
            }

            DisplayScanResults(new List<Transform>(), new List<Transform>());
            LogMessage($"Loaded {_context.LastScan.Sum(g => g.Value.Count)} part classifications.");
        }

        private string GetTransformPath(Transform transform)
        {
            var names = new List<string>();
            Transform current = transform;

            while (current != null && current != _context.SelectedPrefab.transform)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private Transform FindChildByPath(Transform root, string path)
        {
            return root.Find(path);
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null)
                return ValidationResult.Warning("No vehicle loaded");

            if (_context.LastScan == null || _context.LastScan.Count == 0)
                return ValidationResult.Warning("No parts scanned yet");

            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null && _context.SelectedPrefab != null)
            {
                LoadPartClassifications();
            }
        }

        public override void OnModuleGUI() { }
    }
}
