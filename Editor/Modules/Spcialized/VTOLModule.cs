using UnityEngine;
using UnityEngine.UIElements;
using System;
using UVS.Editor.Core;

namespace UVS.Editor.Modules.Specialized
{
    /// <summary>
    /// VTOL (Vertical Take-Off and Landing) vehicle configuration module
    /// Handles thrust vectoring, flight mode transitions, and vertical flight systems
    /// </summary>
    public class VTOLModule : VehicleEditorModuleBase
    {
        #region Module Properties
        public override string ModuleId => "VTOL";
        public override string DisplayName => "VTOL Systems";
        public override int Priority => 60;
        public override bool RequiresVehicle => true;
        public override bool RequiresSpecializedCategory => true;
        public override bool IsConstructionModule => false;
        public override bool IsTankModule => false;
        public override bool IsVTOLModule => true;

        public override bool CanActivateWithConfig(VehicleConfig config)
        {
            if (config == null) return false;

            return config.vehicleType == VehicleConfig.VehicleType.Air &&
                   config.airCategory == VehicleConfig.AirVehicleCategory.Specialized &&
                   config.specializedAir == VehicleConfig.SpecializedAirVehicleType.VTOL;
        }

        #endregion

        #region UI Fields
        // Thrust Vectoring
        private FloatField _thrustVectoringRange;
        private FloatField _vectoringSpeed;
        private Toggle _autoVectoring;
        private FloatField _vectoringResponseTime;

        // Flight Modes
        private EnumField _defaultFlightMode;
        private Toggle _autoTransition;
        private FloatField _transitionSpeed;
        private FloatField _transitionAltitude;

        // Hover Systems
        private FloatField _hoverHeight;
        private FloatField _hoverStability;
        private Toggle _autoHover;
        private FloatField _hoverPowerRequirement;

        // Vertical Flight
        private FloatField _verticalClimbRate;
        private FloatField _verticalDescentRate;
        private FloatField _maxVerticalSpeed;
        private Toggle _verticalStabilization;

        // Engine Configuration
        private IntegerField _engineCount;
        private FloatField _enginePower;
        private FloatField _engineResponseTime;
        private Toggle _engineSync;

        // Safety Systems
        private Toggle _emergencyAutoLand;
        private FloatField _emergencyAltitude;
        private Toggle _stallProtection;
        private FloatField _stallSpeed;

        // Control Systems
        private FloatField _controlSensitivity;
        private FloatField _controlDeadzone;
        private Toggle _assistedFlying;
        private EnumField _controlScheme;

        public VTOLModule()
        {
        }
        #endregion

        #region Module Implementation
        protected override VisualElement CreateModuleUI()
        {
            var root = new VisualElement();
            root.style.paddingTop = 10;

            // Thrust Vectoring Section
            var vectoringSection = CreateSection("Thrust Vectoring", true);

            _thrustVectoringRange = new FloatField("Vectoring Range (°)") { value = 45f };
            _thrustVectoringRange.RegisterValueChangedCallback(OnVectoringFloatConfigChanged);
            vectoringSection.contentContainer.Add(_thrustVectoringRange);

            _vectoringSpeed = new FloatField("Vectoring Speed (°/s)") { value = 30f };
            _vectoringSpeed.RegisterValueChangedCallback(OnVectoringFloatConfigChanged);
            vectoringSection.contentContainer.Add(_vectoringSpeed);

            _autoVectoring = new Toggle("Auto Vectoring") { value = true };
            _autoVectoring.RegisterValueChangedCallback(OnVectoringBoolConfigChanged);
            vectoringSection.contentContainer.Add(_autoVectoring);

            _vectoringResponseTime = new FloatField("Response Time (s)") { value = 0.2f };
            _vectoringResponseTime.RegisterValueChangedCallback(OnVectoringFloatConfigChanged);
            vectoringSection.contentContainer.Add(_vectoringResponseTime);

            root.Add(vectoringSection);

            // Flight Modes Section
            var flightModeSection = CreateSection("Flight Modes", false);

            _defaultFlightMode = new EnumField("Default Mode", FlightMode.Hover) { value = FlightMode.Hover };
            _defaultFlightMode.RegisterValueChangedCallback(OnFlightModeEnumConfigChanged);
            flightModeSection.contentContainer.Add(_defaultFlightMode);

            _autoTransition = new Toggle("Auto Transition") { value = true };
            _autoTransition.RegisterValueChangedCallback(OnFlightModeBoolConfigChanged);
            flightModeSection.contentContainer.Add(_autoTransition);

            _transitionSpeed = new FloatField("Transition Speed (km/h)") { value = 80f };
            _transitionSpeed.RegisterValueChangedCallback(OnFlightModeFloatConfigChanged);
            flightModeSection.contentContainer.Add(_transitionSpeed);

            _transitionAltitude = new FloatField("Transition Altitude (m)") { value = 100f };
            _transitionAltitude.RegisterValueChangedCallback(OnFlightModeFloatConfigChanged);
            flightModeSection.contentContainer.Add(_transitionAltitude);

            root.Add(flightModeSection);

            // Hover Systems Section
            var hoverSection = CreateSection("Hover Systems", false);

            _hoverHeight = new FloatField("Hover Height (m)") { value = 15f };
            _hoverHeight.RegisterValueChangedCallback(OnHoverFloatConfigChanged);
            hoverSection.contentContainer.Add(_hoverHeight);

            _hoverStability = new FloatField("Stability Factor") { value = 0.8f };
            _hoverStability.RegisterValueChangedCallback(OnHoverFloatConfigChanged);
            hoverSection.contentContainer.Add(_hoverStability);

            _autoHover = new Toggle("Auto Hover") { value = true };
            _autoHover.RegisterValueChangedCallback(OnHoverBoolConfigChanged);
            hoverSection.contentContainer.Add(_autoHover);

            _hoverPowerRequirement = new FloatField("Power Requirement (%)") { value = 75f };
            _hoverPowerRequirement.RegisterValueChangedCallback(OnHoverFloatConfigChanged);
            hoverSection.contentContainer.Add(_hoverPowerRequirement);

            root.Add(hoverSection);

            // Vertical Flight Section
            var verticalSection = CreateSection("Vertical Flight", false);

            _verticalClimbRate = new FloatField("Climb Rate (m/s)") { value = 8f };
            _verticalClimbRate.RegisterValueChangedCallback(OnVerticalFloatConfigChanged);
            verticalSection.contentContainer.Add(_verticalClimbRate);

            _verticalDescentRate = new FloatField("Descent Rate (m/s)") { value = 5f };
            _verticalDescentRate.RegisterValueChangedCallback(OnVerticalFloatConfigChanged);
            verticalSection.contentContainer.Add(_verticalDescentRate);

            _maxVerticalSpeed = new FloatField("Max Vertical Speed (m/s)") { value = 15f };
            _maxVerticalSpeed.RegisterValueChangedCallback(OnVerticalFloatConfigChanged);
            verticalSection.contentContainer.Add(_maxVerticalSpeed);

            _verticalStabilization = new Toggle("Vertical Stabilization") { value = true };
            _verticalStabilization.RegisterValueChangedCallback(OnVerticalBoolConfigChanged);
            verticalSection.contentContainer.Add(_verticalStabilization);

            root.Add(verticalSection);

            // Engine Configuration Section
            var engineSection = CreateSection("Engine Configuration", false);

            _engineCount = new IntegerField("Engine Count") { value = 4 };
            _engineCount.RegisterValueChangedCallback(OnEngineIntConfigChanged);
            engineSection.contentContainer.Add(_engineCount);

            _enginePower = new FloatField("Power per Engine (kW)") { value = 1500f };
            _enginePower.RegisterValueChangedCallback(OnEngineFloatConfigChanged);
            engineSection.contentContainer.Add(_enginePower);

            _engineResponseTime = new FloatField("Response Time (s)") { value = 0.5f };
            _engineResponseTime.RegisterValueChangedCallback(OnEngineFloatConfigChanged);
            engineSection.contentContainer.Add(_engineResponseTime);

            _engineSync = new Toggle("Engine Synchronization") { value = true };
            _engineSync.RegisterValueChangedCallback(OnEngineBoolConfigChanged);
            engineSection.contentContainer.Add(_engineSync);

            root.Add(engineSection);

            // Safety Systems Section
            var safetySection = CreateSection("Safety Systems", false);

            _emergencyAutoLand = new Toggle("Emergency Auto-Land") { value = true };
            _emergencyAutoLand.RegisterValueChangedCallback(OnSafetyBoolConfigChanged);
            safetySection.contentContainer.Add(_emergencyAutoLand);

            _emergencyAltitude = new FloatField("Emergency Altitude (m)") { value = 50f };
            _emergencyAltitude.RegisterValueChangedCallback(OnSafetyFloatConfigChanged);
            safetySection.contentContainer.Add(_emergencyAltitude);

            _stallProtection = new Toggle("Stall Protection") { value = true };
            _stallProtection.RegisterValueChangedCallback(OnSafetyBoolConfigChanged);
            safetySection.contentContainer.Add(_stallProtection);

            _stallSpeed = new FloatField("Stall Speed (km/h)") { value = 40f };
            _stallSpeed.RegisterValueChangedCallback(OnSafetyFloatConfigChanged);
            safetySection.contentContainer.Add(_stallSpeed);

            root.Add(safetySection);

            // Control Systems Section
            var controlSection = CreateSection("Control Systems", false);

            _controlSensitivity = new FloatField("Control Sensitivity") { value = 1.0f };
            _controlSensitivity.RegisterValueChangedCallback(OnControlFloatConfigChanged);
            controlSection.contentContainer.Add(_controlSensitivity);

            _controlDeadzone = new FloatField("Control Deadzone") { value = 0.1f };
            _controlDeadzone.RegisterValueChangedCallback(OnControlFloatConfigChanged);
            controlSection.contentContainer.Add(_controlDeadzone);

            _assistedFlying = new Toggle("Assisted Flying") { value = true };
            _assistedFlying.RegisterValueChangedCallback(OnControlBoolConfigChanged);
            controlSection.contentContainer.Add(_assistedFlying);

            _controlScheme = new EnumField("Control Scheme", ControlScheme.Standard);
            _controlScheme.RegisterValueChangedCallback(OnControlEnumConfigChanged);
            controlSection.contentContainer.Add(_controlScheme);

            root.Add(controlSection);

            return root;
        }

        protected override ValidationResult ValidateModule()
        {
            var result = new ValidationResult();

            // Thrust vectoring validation
            if (_thrustVectoringRange.value > 90)
                result.AddError("Thrust vectoring range cannot exceed 90°");
            if (_thrustVectoringRange.value < 15)
                result.AddWarning("Limited vectoring range may reduce maneuverability");

            if (_vectoringSpeed.value > 60)
                result.AddError("Vectoring speed too high - risk of mechanical failure");
            if (_vectoringSpeed.value < 10)
                result.AddWarning("Slow vectoring may cause control lag");

            // Flight mode validation
            if (_transitionSpeed.value > 200)
                result.AddError("Transition speed exceeds safe limits for VTOL operations");
            if (_transitionSpeed.value < 40)
                result.AddWarning("Low transition speed may cause instability");

            if (_transitionAltitude.value < 20)
                result.AddError("Transition altitude too low - safety risk");
            if (_transitionAltitude.value > 500)
                result.AddWarning("High transition altitude may reduce efficiency");

            // Hover validation
            if (_hoverHeight.value < 5)
                result.AddWarning("Very low hover height increases ground effect risks");
            if (_hoverHeight.value > 100)
                result.AddWarning("High hover height reduces stability");

            if (_hoverPowerRequirement.value > 90)
                result.AddError("Excessive power requirement - engine overload risk");
            if (_hoverPowerRequirement.value < 50)
                result.AddWarning("Low power requirement may indicate insufficient thrust");

            // Vertical flight validation
            if (_verticalClimbRate.value > 20)
                result.AddError("Excessive climb rate - structural stress risk");
            if (_verticalDescentRate.value > 10)
                result.AddError("Excessive descent rate - hard landing risk");

            // Engine validation
            if (_engineCount.value < 2)
                result.AddError("VTOL requires minimum 2 engines for safety");
            if (_engineCount.value > 8)
                result.AddWarning("Many engines increase complexity and maintenance");

            if (_enginePower.value > 3000)
                result.AddWarning("High engine power may cause excessive fuel consumption");

            // Safety validation
            if (_emergencyAltitude.value < 30)
                result.AddWarning("Low emergency altitude reduces auto-land effectiveness");
            if (_stallSpeed.value > 80)
                result.AddError("High stall speed reduces safety margins");

            // Control validation
            if (_controlSensitivity.value > 2.0f)
                result.AddWarning("High sensitivity may cause over-control");
            if (_controlSensitivity.value < 0.3f)
                result.AddWarning("Low sensitivity may cause sluggish response");

            if (_controlDeadzone.value > 0.5f)
                result.AddError("Large deadzone reduces control precision");

            return result;
        }

        protected override void OnModuleActivated()
        {
            _context.Console.LogInfo($"VTOL module activated for vehicle: {_context.Config?.VehicleID ?? "Unknown"}");
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
            {
                _context.Console.LogInfo($"VTOL configuration updated for {config.VehicleID}");
            }
        }
        #endregion

        #region Event Handlers
        private void OnVectoringFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("thrust_vectoring_range", _thrustVectoringRange.value);
                _context.Config.SetFloat("vectoring_speed", _vectoringSpeed.value);
                _context.Config.SetFloat("vectoring_response_time", _vectoringResponseTime.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnVectoringBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("auto_vectoring", _autoVectoring.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnFlightModeEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("default_flight_mode", (FlightMode)_defaultFlightMode.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnFlightModeBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("auto_transition", _autoTransition.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnFlightModeFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("transition_speed", _transitionSpeed.value);
                _context.Config.SetFloat("transition_altitude", _transitionAltitude.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnHoverFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("hover_height", _hoverHeight.value);
                _context.Config.SetFloat("hover_stability", _hoverStability.value);
                _context.Config.SetFloat("hover_power_requirement", _hoverPowerRequirement.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnHoverBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("auto_hover", _autoHover.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnVerticalFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("vertical_climb_rate", _verticalClimbRate.value);
                _context.Config.SetFloat("vertical_descent_rate", _verticalDescentRate.value);
                _context.Config.SetFloat("max_vertical_speed", _maxVerticalSpeed.value);
                _context.Config.SetBool("vertical_stabilization", _verticalStabilization.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnVerticalBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("vertical_stabilization", _verticalStabilization.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnEngineIntConfigChanged(ChangeEvent<int> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetInt("engine_count", _engineCount.value);
                _context.Config.SetBool("engine_sync", _engineSync.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnEngineFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("engine_power", _enginePower.value);
                _context.Config.SetFloat("engine_response_time", _engineResponseTime.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnEngineBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("engine_sync", _engineSync.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnSafetyBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("emergency_auto_land", _emergencyAutoLand.value);
                _context.Config.SetBool("stall_protection", _stallProtection.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnSafetyFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("emergency_altitude", _emergencyAltitude.value);
                _context.Config.SetFloat("stall_speed", _stallSpeed.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnControlFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("control_sensitivity", _controlSensitivity.value);
                _context.Config.SetFloat("control_deadzone", _controlDeadzone.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnControlBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("assisted_flying", _assistedFlying.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnControlEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("control_scheme", (ControlScheme)_controlScheme.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }
        #endregion

        #region Helper Methods
        private Foldout CreateSection(string title, bool expanded)
        {
            return new Foldout()
            {
                text = title,
                value = expanded,
                style =
                {
                    marginBottom = 5,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f))
                }
            };
        }

        public override void OnModuleGUI() { }
        #endregion
    }

    #region Supporting Enums
    public enum FlightMode
    {
        Hover,
        ForwardFlight,
        Transition,
        Auto
    }

    public enum ControlScheme
    {
        Standard,
        Advanced,
        Expert,
        Custom
    }
    #endregion
}