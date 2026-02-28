using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using UVS.Editor.Core;
using System.Collections.Generic;

namespace UVS.Editor.Modules
{
    [VehicleModuleSupport(typeId: "water")]
    public class WaterModule : VehicleEditorModuleBase
    {
        private FloatField _waterDensity;
        private FloatField _buoyancyForce;
        private FloatField _linearDrag;
        private FloatField _angularDrag;
        private FloatField _propulsionForce;
        private FloatField _turnTorque;
        private EnumField _qualityTier;
        private Toggle _foamEnabled;
        private Toggle _depthColorEnabled;
        private Toggle _causticsEnabled;

        private VisualElement _pointsContainer;
        private Button _addPointButton;
        private Button _clearPointsButton;
        private Button _autoGenerateButton;

        public override string ModuleId => "water";
        public override string DisplayName => "Water Physics";
        public override int Priority => 56;
        public override bool RequiresVehicle => true;
        public override bool RequiresSpecializedCategory => false;
        public override bool IsConstructionModule => false;
        public override bool IsTankModule => false;
        public override bool IsVTOLModule => false;

        public override bool CanActivateWithConfig(VehicleConfig config)
        {
            return config != null && config.vehicleType == VehicleConfig.VehicleType.Water;
        }

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

            var header = new Label("Water Physics Settings")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 15
                }
            };
            container.Add(header);

            _waterDensity = new FloatField("Water Density (kg/m3)");
            _waterDensity.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_waterDensity);

            _buoyancyForce = new FloatField("Buoyancy Force Multiplier");
            _buoyancyForce.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_buoyancyForce);

            _linearDrag = new FloatField("Linear Drag");
            _linearDrag.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_linearDrag);

            _angularDrag = new FloatField("Angular Drag");
            _angularDrag.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_angularDrag);

            _propulsionForce = new FloatField("Propulsion Force (N)");
            _propulsionForce.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_propulsionForce);

            _turnTorque = new FloatField("Turn Torque");
            _turnTorque.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_turnTorque);

            container.Add(new Label("Water Rendering")
            {
                style =
                {
                    marginTop = 10,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            });

            _qualityTier = new EnumField("Quality Tier", VehicleConfig.WaterQualityTier.Medium);
            _qualityTier.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_qualityTier);

            _foamEnabled = new Toggle("Enable Foam");
            _foamEnabled.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_foamEnabled);

            _depthColorEnabled = new Toggle("Enable Depth Color");
            _depthColorEnabled.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_depthColorEnabled);

            _causticsEnabled = new Toggle("Enable Caustics");
            _causticsEnabled.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_causticsEnabled);

            var pointsHeader = new Label("Buoyancy Points")
            {
                style =
                {
                    marginTop = 10,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            container.Add(pointsHeader);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 6 } };
            _addPointButton = new Button(AddPoint) { text = "Add Point" };
            _clearPointsButton = new Button(ClearPoints) { text = "Clear Points" };
            _autoGenerateButton = new Button(AutoGeneratePoints) { text = "Auto-Generate" };
            buttonRow.Add(_addPointButton);
            buttonRow.Add(_clearPointsButton);
            buttonRow.Add(_autoGenerateButton);
            container.Add(buttonRow);

            _pointsContainer = new VisualElement();
            container.Add(_pointsContainer);

            return container;
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null)
                return ValidationResult.Warning("No vehicle loaded");

            var w = _context.CurrentConfig.water;
            if (w.waterDensity <= 0) return ValidationResult.Error("Water density must be > 0");
            if (w.buoyancyPoints == null || w.buoyancyPoints.Count == 0)
                return ValidationResult.Warning("No buoyancy points configured");

            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
            {
                LoadFromConfig(config);
            }
        }

        protected override void OnModuleActivated()
        {
            if (_context?.CurrentConfig != null)
            {
                LoadFromConfig(_context.CurrentConfig);
            }
        }

        private void LoadFromConfig(VehicleConfig config)
        {
            var w = config.water;
            _waterDensity.value = w.waterDensity;
            _buoyancyForce.value = w.buoyancyForce;
            _linearDrag.value = w.linearDrag;
            _angularDrag.value = w.angularDrag;
            _propulsionForce.value = w.propulsionForce;
            _turnTorque.value = w.turnTorque;
            _qualityTier.value = config.waterRender.qualityTier;
            _foamEnabled.value = config.waterRender.foamEnabled;
            _depthColorEnabled.value = config.waterRender.depthColorEnabled;
            _causticsEnabled.value = config.waterRender.causticsEnabled;
            RebuildPointsUI();
        }

        private void SaveConfig()
        {
            if (_context?.CurrentConfig == null) return;
            var w = _context.CurrentConfig.water;
            w.waterDensity = _waterDensity.value;
            w.buoyancyForce = _buoyancyForce.value;
            w.linearDrag = _linearDrag.value;
            w.angularDrag = _angularDrag.value;
            w.propulsionForce = _propulsionForce.value;
            w.turnTorque = _turnTorque.value;
            _context.CurrentConfig.waterRender.qualityTier = (VehicleConfig.WaterQualityTier)_qualityTier.value;
            _context.CurrentConfig.waterRender.foamEnabled = _foamEnabled.value;
            _context.CurrentConfig.waterRender.depthColorEnabled = _depthColorEnabled.value;
            _context.CurrentConfig.waterRender.causticsEnabled = _causticsEnabled.value;

            EditorUtility.SetDirty(_context.CurrentConfig);
            _context.NotifyConfigChanged(_context.CurrentConfig);
        }

        private void RebuildPointsUI()
        {
            _pointsContainer.Clear();
            if (_context?.CurrentConfig == null) return;

            List<VehicleConfig.BuoyancyPoint> points = _context.CurrentConfig.water.buoyancyPoints;
            if (points == null)
            {
                points = new List<VehicleConfig.BuoyancyPoint>();
                _context.CurrentConfig.water.buoyancyPoints = points;
            }

            for (int i = 0; i < points.Count; i++)
            {
                int index = i;
                var point = points[index];

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 4 } };

                var posField = new Vector3Field($"Point {index} Pos") { value = point.localPosition };
                posField.style.minWidth = 200;
                posField.RegisterValueChangedCallback(evt =>
                {
                    points[index].localPosition = evt.newValue;
                    SaveConfig();
                });
                row.Add(posField);

                var volumeField = new FloatField("Volume") { value = point.volume };
                volumeField.style.minWidth = 80;
                volumeField.RegisterValueChangedCallback(evt =>
                {
                    points[index].volume = evt.newValue;
                    SaveConfig();
                });
                row.Add(volumeField);

                var subField = new FloatField("Max Submersion") { value = point.maxSubmersion };
                subField.style.minWidth = 120;
                subField.RegisterValueChangedCallback(evt =>
                {
                    points[index].maxSubmersion = evt.newValue;
                    SaveConfig();
                });
                row.Add(subField);

                var removeBtn = new Button(() =>
                {
                    points.RemoveAt(index);
                    SaveConfig();
                    RebuildPointsUI();
                }) { text = "Remove" };
                row.Add(removeBtn);

                _pointsContainer.Add(row);
            }
        }

        private void AddPoint()
        {
            if (_context?.CurrentConfig == null) return;
            _context.CurrentConfig.water.buoyancyPoints.Add(new VehicleConfig.BuoyancyPoint
            {
                localPosition = Vector3.zero,
                volume = 1f,
                maxSubmersion = 1f
            });
            SaveConfig();
            RebuildPointsUI();
        }

        private void ClearPoints()
        {
            if (_context?.CurrentConfig == null) return;
            _context.CurrentConfig.water.buoyancyPoints.Clear();
            SaveConfig();
            RebuildPointsUI();
        }

        private void AutoGeneratePoints()
        {
            if (_context?.SelectedPrefab == null)
            {
                LogError("No prefab selected.");
                return;
            }

            var renderers = _context.SelectedPrefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                LogError("No renderers found to generate buoyancy points.");
                return;
            }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            Vector3 min = b.min;
            Vector3 max = b.max;
            float y = min.y;

            var worldPoints = new Vector3[]
            {
                new Vector3(min.x, y, min.z),
                new Vector3(min.x, y, max.z),
                new Vector3(max.x, y, min.z),
                new Vector3(max.x, y, max.z),
                new Vector3((min.x + max.x) * 0.5f, y, (min.z + max.z) * 0.5f)
            };

            var points = _context.CurrentConfig.water.buoyancyPoints;
            points.Clear();
            float maxSub = Mathf.Max(0.1f, b.size.y * 0.5f);

            foreach (var wp in worldPoints)
            {
                var local = _context.SelectedPrefab.transform.InverseTransformPoint(wp);
                points.Add(new VehicleConfig.BuoyancyPoint
                {
                    localPosition = local,
                    volume = 1f,
                    maxSubmersion = maxSub
                });
            }

            SaveConfig();
            RebuildPointsUI();
            LogMessage("Generated buoyancy points from bounds.");
        }

        public override void OnModuleGUI() { }
    }
}
