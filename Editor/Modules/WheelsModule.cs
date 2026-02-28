using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UVS.Editor.Core;
using UVS.Modules;

namespace UVS.Editor.Modules
{
    public class WheelsModule : VehicleEditorModuleBase
    {
        private VisualElement _wheelsContent;
        private Button _scanWheelsButton;
        private Button _swapFrontRearButton;
        private Button _applyCollidersButton;
        private Button _measureButton;
        
        public override string ModuleId => "wheels";
        public override string DisplayName => "Wheels";
        public override int Priority => 30;

        protected bool HasValidVehicle()
        {
            return _context?.CurrentConfig != null;
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

        private string GetLastPathPart(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            int lastSlash = path.LastIndexOf('/');
            return lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
        }
        
        private Transform FindChildByPath(Transform root, string path)
        {
            return root.Find(path);
        }

        protected override VisualElement CreateModuleUI()
        {
            var container = new VisualElement();
            container.style.paddingLeft = 20;
            container.style.paddingRight = 20;
            container.style.paddingTop = 20;
            container.style.paddingBottom = 20;
            
            var headerLabel = new Label("Wheel Configuration");
            headerLabel.style.fontSize = 16;
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.marginBottom = 15;
            container.Add(headerLabel);
            
            CreateControlButtons(container);
            
            _wheelsContent = new ScrollView();
            _wheelsContent.style.height = 400;
            _wheelsContent.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            _wheelsContent.style.paddingLeft = 10;
            _wheelsContent.style.paddingRight = 10;
            _wheelsContent.style.paddingTop = 10;
            _wheelsContent.style.paddingBottom = 10;
            container.Add(_wheelsContent);
            
            return container;
        }
        
        private void CreateControlButtons(VisualElement parent)
        {
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.flexWrap = Wrap.Wrap;
            buttonContainer.style.marginBottom = 15;
            
            _scanWheelsButton = new Button(ScanWheels)
            {
                text = "Scan Wheels"
            };
            _scanWheelsButton.style.marginRight = 10;
            _scanWheelsButton.style.marginBottom = 5;
            buttonContainer.Add(_scanWheelsButton);

            _swapFrontRearButton = new Button(SwapFrontRear)
            {
                text = "Swap Front/Rear"
            };
            _swapFrontRearButton.style.marginRight = 10;
            _swapFrontRearButton.style.marginBottom = 5;
            buttonContainer.Add(_swapFrontRearButton);
            
            _applyCollidersButton = new Button(ApplyWheelColliders)
            {
                text = "Apply Colliders"
            };
            _applyCollidersButton.style.marginRight = 10;
            _applyCollidersButton.style.marginBottom = 5;
            buttonContainer.Add(_applyCollidersButton);
            
            _measureButton = new Button(MeasureVehicle)
            {
                text = "Measure Vehicle"
            };
            _measureButton.style.marginRight = 10;
            _measureButton.style.marginBottom = 5;
            buttonContainer.Add(_measureButton);
            
            parent.Add(buttonContainer);
        }
        
        private void ScanWheels()
        {
            if (!HasValidVehicle())
            {
                LogError("No vehicle loaded. Please select a vehicle first.");
                return;
            }
            
            if (_context.LastScan == null || !_context.LastScan.ContainsKey(VehicleConfig.VehiclePartType.Wheel))
            {
                LogError("No wheels found in last scan. Please scan parts first.");
                return;
            }
            
            var wheelTransforms = _context.LastScan[VehicleConfig.VehiclePartType.Wheel];
            var wheelSettings = new List<VehicleConfig.WheelSettings>();
            
            foreach (var wheel in wheelTransforms)
            {
                if (wheel == null) continue;
                
                var wheelSetting = new VehicleConfig.WheelSettings
                {
                    partPath = GetTransformPath(wheel),
                    radius = 0.35f,
                    width = 0.2f,
                    SuspensionDistance = 0.3f,
                    localPosition = wheel.localPosition
                };
                
                wheelSetting.isSteering = wheelSetting.localPosition.z > 0;
                wheelSetting.isPowered = wheelSetting.localPosition.z < 0;
                wheelSetting.role = wheelSetting.isSteering
                    ? VehicleConfig.WheelRole.FrontSteer
                    : wheelSetting.isPowered
                        ? VehicleConfig.WheelRole.RearDrive
                        : VehicleConfig.WheelRole.Free;
                
                wheelSettings.Add(wheelSetting);
            }
            
            _context.CurrentConfig.wheels = wheelSettings;
            EditorUtility.SetDirty(_context.CurrentConfig);
            AssetDatabase.SaveAssets();
            
            DisplayWheels(wheelSettings);
            LogMessage($"Scanned {wheelSettings.Count} wheels successfully.");
        }
        
        private void DisplayWheels(List<VehicleConfig.WheelSettings> wheelSettings)
        {
            _wheelsContent.Clear();
            
            if (wheelSettings == null || wheelSettings.Count == 0)
            {
                var noWheelsLabel = new Label("No wheels configured. Click 'Scan Wheels' to detect wheels.");
                noWheelsLabel.style.color = Color.yellow;
                _wheelsContent.Add(noWheelsLabel);
                return;
            }
            
            var allGroup = new Foldout { text = $"All Wheels ({wheelSettings.Count})", value = true };
            allGroup.AddToClassList("wheels-foldout");

            foreach (var wheel in wheelSettings.OrderByDescending(w => w.localPosition.z))
            {
                allGroup.Add(CreateWheelRow(wheel));
            }

            _wheelsContent.Add(allGroup);
        }
        
        private VisualElement CreateWheelRow(VehicleConfig.WheelSettings wheelSetting)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 5;
            row.style.paddingLeft = 5;
            row.style.paddingRight = 5;
            row.style.paddingTop = 5;
            row.style.paddingBottom = 5;
            row.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            var nameLabel = new Label(GetLastPathPart(wheelSetting.partPath));
            nameLabel.style.width = 120;
            nameLabel.style.color = Color.white;
            row.Add(nameLabel);

            var radiusField = new FloatField("Radius")
            {
                value = wheelSetting.radius
            };
            radiusField.style.width = 80;
            radiusField.RegisterValueChangedCallback(evt => 
            {
                wheelSetting.radius = evt.newValue;
                EditorUtility.SetDirty(_context.CurrentConfig);
            });
            row.Add(radiusField);

            var widthField = new FloatField("Width")
            {
                value = wheelSetting.width
            };
            widthField.style.width = 80;
            widthField.RegisterValueChangedCallback(evt => 
            {
                wheelSetting.width = evt.newValue;
                EditorUtility.SetDirty(_context.CurrentConfig);
            });
            row.Add(widthField);

            var suspensionField = new FloatField("Suspension")
            {
                value = wheelSetting.SuspensionDistance
            };
            suspensionField.style.width = 100;
            suspensionField.RegisterValueChangedCallback(evt => 
            {
                wheelSetting.SuspensionDistance = evt.newValue;
                EditorUtility.SetDirty(_context.CurrentConfig);
            });
            row.Add(suspensionField);

            var steeringToggle = new Toggle("Steering")
            {
                value = wheelSetting.isSteering
            };
            steeringToggle.RegisterValueChangedCallback(evt => 
            {
                wheelSetting.isSteering = evt.newValue;
                wheelSetting.role = ResolveRoleFromFlags(wheelSetting);
                EditorUtility.SetDirty(_context.CurrentConfig);
            });
            row.Add(steeringToggle);

            var poweredToggle = new Toggle("Powered")
            {
                value = wheelSetting.isPowered
            };
            poweredToggle.RegisterValueChangedCallback(evt => 
            {
                wheelSetting.isPowered = evt.newValue;
                wheelSetting.role = ResolveRoleFromFlags(wheelSetting);
                EditorUtility.SetDirty(_context.CurrentConfig);
            });
            row.Add(poweredToggle);

            var roleField = new EnumField("Role", wheelSetting.role)
            {
                value = wheelSetting.role
            };
            roleField.RegisterValueChangedCallback(evt =>
            {
                wheelSetting.role = (VehicleConfig.WheelRole)evt.newValue;
                ApplyRolePreset(wheelSetting);
                steeringToggle.SetValueWithoutNotify(wheelSetting.isSteering);
                poweredToggle.SetValueWithoutNotify(wheelSetting.isPowered);
                EditorUtility.SetDirty(_context.CurrentConfig);
            });
            row.Add(roleField);
            
            return row;
        }

        private void SwapFrontRear()
        {
            if (!HasValidVehicle() || _context.CurrentConfig.wheels == null || _context.CurrentConfig.wheels.Count == 0)
            {
                LogError("No wheels configured.");
                return;
            }

            foreach (var wheel in _context.CurrentConfig.wheels)
            {
                bool oldSteer = wheel.isSteering;
                wheel.isSteering = wheel.isPowered;
                wheel.isPowered = oldSteer;

                wheel.role = wheel.role switch
                {
                    VehicleConfig.WheelRole.FrontSteer => VehicleConfig.WheelRole.RearDrive,
                    VehicleConfig.WheelRole.RearDrive => VehicleConfig.WheelRole.FrontSteer,
                    _ => ResolveRoleFromFlags(wheel)
                };
            }

            EditorUtility.SetDirty(_context.CurrentConfig);
            AssetDatabase.SaveAssets();
            DisplayWheels(_context.CurrentConfig.wheels);
            LogMessage("Swapped front/rear wheel treatment. Re-test steering and drive.");
        }

        private static VehicleConfig.WheelRole ResolveRoleFromFlags(VehicleConfig.WheelSettings wheelSetting)
        {
            if (wheelSetting.isSteering && !wheelSetting.isPowered) return VehicleConfig.WheelRole.FrontSteer;
            if (!wheelSetting.isSteering && wheelSetting.isPowered) return VehicleConfig.WheelRole.RearDrive;
            return VehicleConfig.WheelRole.Free;
        }

        private static void ApplyRolePreset(VehicleConfig.WheelSettings wheelSetting)
        {
            switch (wheelSetting.role)
            {
                case VehicleConfig.WheelRole.FrontSteer:
                    wheelSetting.isSteering = true;
                    wheelSetting.isPowered = false;
                    break;
                case VehicleConfig.WheelRole.RearDrive:
                    wheelSetting.isSteering = false;
                    wheelSetting.isPowered = true;
                    break;
                case VehicleConfig.WheelRole.TrackLeft:
                case VehicleConfig.WheelRole.TrackRight:
                    wheelSetting.isSteering = false;
                    wheelSetting.isPowered = true;
                    break;
                case VehicleConfig.WheelRole.Free:
                    break;
            }
        }
        
        private void ApplyWheelColliders()
        {
            if (!HasValidVehicle())
            {
                LogError("No vehicle loaded.");
                return;
            }
            
            if (_context.CurrentConfig.wheels == null || _context.CurrentConfig.wheels.Count == 0)
            {
                LogError("No wheel settings found. Please scan wheels first.");
                return;
            }
            
            string path = AssetDatabase.GetAssetPath(_context.SelectedPrefab);
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            
            try
            {
                if (!prefabRoot.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb = prefabRoot.AddComponent<Rigidbody>();
                    LogMessage("Added Rigidbody to vehicle");
                }
                
                rb.mass = _context.CurrentConfig.body.mass > 0 ? _context.CurrentConfig.body.mass : 1200f;
                rb.linearDamping = _context.CurrentConfig.body.dragCoefficient;
                rb.angularDamping = 0.05f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.centerOfMass = _context.CurrentConfig.measurements.centerOfMassEstimate;

                Transform existingWCParent = prefabRoot.transform.Find("WheelColliders");
                if (existingWCParent != null)
                {
                    Object.DestroyImmediate(existingWCParent.gameObject);
                }

                Transform wheelCollidersParent = new GameObject("WheelColliders").transform;
                wheelCollidersParent.SetParent(prefabRoot.transform);
                wheelCollidersParent.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (var wheelSetting in _context.CurrentConfig.wheels)
                {
                    Transform wheelTransform = FindChildByPath(prefabRoot.transform, wheelSetting.partPath);
                    if (wheelTransform == null) continue;
                    
                    GameObject colliderObj = new($"{wheelTransform.name}_collider");
                    colliderObj.transform.SetParent(wheelCollidersParent, false);
                    colliderObj.transform.SetLocalPositionAndRotation(wheelTransform.localPosition, wheelTransform.localRotation);
                    var wheelCollider = colliderObj.AddComponent<WheelCollider>();
                    wheelCollider.radius = wheelSetting.radius;
                    wheelCollider.suspensionDistance = wheelSetting.SuspensionDistance;
                    wheelCollider.mass = 20f;
                    wheelCollider.center = Vector3.zero;
                    
                    var spring = wheelCollider.suspensionSpring;
                    spring.spring = _context.CurrentConfig.suspension.springStiffness > 0 ? 
                        _context.CurrentConfig.suspension.springStiffness : 35000f;
                    spring.damper = _context.CurrentConfig.suspension.damperStiffness > 0 ? 
                        _context.CurrentConfig.suspension.damperStiffness : 4500f;
                    spring.targetPosition = 0.5f;
                    wheelCollider.suspensionSpring = spring;
                    
                    ConfigureWheelFriction(wheelCollider);
                }
                
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                LogMessage("Wheel colliders applied successfully");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
        
        private void ConfigureWheelFriction(WheelCollider wheelCollider)
        {
            var forwardFriction = wheelCollider.forwardFriction;
            forwardFriction.extremumSlip = 0.4f;
            forwardFriction.extremumValue = 1f;
            forwardFriction.asymptoteSlip = 0.8f;
            forwardFriction.asymptoteValue = 0.5f;
            forwardFriction.stiffness = 1f;
            wheelCollider.forwardFriction = forwardFriction;
            
            var sidewaysFriction = wheelCollider.sidewaysFriction;
            sidewaysFriction.extremumSlip = 0.2f;
            sidewaysFriction.extremumValue = 1f;
            sidewaysFriction.asymptoteSlip = 0.5f;
            sidewaysFriction.asymptoteValue = 0.75f;
            sidewaysFriction.stiffness = 1f;
            wheelCollider.sidewaysFriction = sidewaysFriction;
        }
        
        private void MeasureVehicle()
        {
            if (!HasValidVehicle())
            {
                LogError("No vehicle loaded.");
                return;
            }
            
            if (_context.LastScan == null || !_context.LastScan.ContainsKey(VehicleConfig.VehiclePartType.Wheel))
            {
                LogError("No wheels found. Please scan parts first.");
                return;
            }
            
            try
            {
                VehicleMeasurementModule.Measure(_context.CurrentConfig, _context.LastScan[VehicleConfig.VehiclePartType.Wheel]);
                LogMessage("Vehicle measurements updated successfully");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to measure vehicle: {ex.Message}");
            }
        }
        
        protected override ValidationResult ValidateModule()
        {
            if (!HasValidVehicle())
            {
                return ValidationResult.Warning("No vehicle loaded");
            }
            
            if (_context.CurrentConfig.wheels == null || _context.CurrentConfig.wheels.Count == 0)
            {
                return ValidationResult.Warning("No wheels configured");
            }
            
            foreach (var wheel in _context.CurrentConfig.wheels)
            {
                if (wheel.radius <= 0)
                {
                    return ValidationResult.Error($"Invalid wheel radius: {wheel.radius}");
                }
                
                if (wheel.width <= 0)
                {
                    return ValidationResult.Error($"Invalid wheel width: {wheel.width}");
                }
                
                if (wheel.SuspensionDistance <= 0)
                {
                    return ValidationResult.Error($"Invalid suspension distance: {wheel.SuspensionDistance}");
                }
            }
            
            return ValidationResult.Success();
        }
        
        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null && config.wheels != null)
            {
                DisplayWheels(config.wheels);
            }
        }
        
        protected override void OnModuleActivated()
        {
            if (_context.CurrentConfig != null && _context.CurrentConfig.wheels != null)
            {
                DisplayWheels(_context.CurrentConfig.wheels);
            }
        }
    }
}
