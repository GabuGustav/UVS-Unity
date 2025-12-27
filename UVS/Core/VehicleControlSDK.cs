using UnityEngine;
using System;
using System.Collections.Generic;

namespace UVS.Editor.Core
{
    /// Unified Vehicle Control SDK for specialized vehicle types
    /// Provides consistent interface for tanks, VTOLs, construction equipment
    public class VehicleControlSDK
    {
        #region Core Control Interfaces

        /// Base interface for all specialized vehicle controls
        public interface ISpecializedControl
        {
            string ControlName { get; }
            ControlType Type { get; }
            bool IsActive { get; }
            void Initialize(VehicleConfig config);
            void Update(float deltaTime);
            void Reset();
        }

        /// Tank-specific control interface
        public interface ITankControl : ISpecializedControl
        {
            // Turret controls
            void RotateTurret(float angle, float speed);
            void ElevateTurret(float angle, float speed);
            void FireWeapon();
            void Reload();

            // Track controls
            void SetTrackSpeed(float leftSpeed, float rightSpeed);
            void SetTrackBrake(float leftBrake, float rightBrake);

            // Weapon systems
            void SwitchAmmoType(AmmoType type);
            void SetFireMode(FireMode mode);

            // Status
            float GetTurretAngle();
            float GetTurretElevation();
            int GetAmmoCount();
            bool IsReloading();
        }

        /// VTOL-specific control interface
        public interface IVTOLControl : ISpecializedControl
        {
            // Flight mode controls
            void SetFlightMode(FlightMode mode);
            void TransitionToHover();
            void TransitionToForwardFlight();

            // Thrust vectoring
            void SetThrustVectoring(float horizontal, float vertical);
            void SetEnginePower(float power);

            // Hover controls
            void SetHoverHeight(float height);
            void SetHoverStability(float stability);

            // Status
            FlightMode GetCurrentFlightMode();
            float GetThrustVectorAngle();
            bool IsInTransition();
        }

        /// Construction equipment control interface
        public interface IConstructionControl : ISpecializedControl
        {
            // Arm controls
            void SetArmAngle(float angle);
            void SetArmExtension(float extension);
            void SetArmRotation(float rotation);

            // Attachment controls
            void SetBucketAngle(float angle);
            void SetBucketOpen(float openAmount);
            void SetAttachmentType(AttachmentType type);

            // Hydraulic systems
            void SetHydraulicPressure(float pressure);
            void SetHydraulicFlow(float flow);

            // Stability systems
            void DeployOutriggers();
            void RetractOutriggers();
            void SetStabilization(bool enabled);

            // Status
            float GetArmAngle();
            float GetArmExtension();
            bool IsStable();
            float GetLoadWeight();
        }

        /// Marine vessel control interface
        public interface IMarineControl : ISpecializedControl
        {
            // Propulsion
            void SetPropellerSpeed(float speed);
            void SetRudderAngle(float angle);
            void SetThrustVector(float vector);

            // Ballast systems
            void SetBallastLevel(float level);
            void SetTrimAngle(float angle);

            // Navigation
            void SetAutopilot(bool enabled);
            void SetWaypoint(Vector3 waypoint);

            // Status
            float GetSpeedKnots();
            float GetHeading();
            float GetDepth();
        }
        #endregion

        #region Control Implementation Classes

        /// Tank control implementation
        public class TankControl : ITankControl
        {
            public string ControlName => "Tank Control System";
            public ControlType Type => ControlType.Tank;
            public bool IsActive { get; private set; }

            private VehicleConfig _config;
            private float _turretAngle;
            private float _turretElevation;
            private int _ammoCount;
            private bool _isReloading;
            private FireMode _currentFireMode;
            private AmmoType _currentAmmoType;

            public void Initialize(VehicleConfig config)
            {
                _config = config;
                IsActive = true;

                // Load initial values from config
                _turretAngle = config.GetFloat("turret_angle", 0f);
                _turretElevation = config.GetFloat("turret_elevation", 0f);
                _ammoCount = config.GetInt("ammo_count", 40);
                _currentFireMode = config.GetEnum("fire_mode", FireMode.Single);
                _currentAmmoType = config.GetEnum("ammo_type", AmmoType.APFSDS);
            }

            public void Update(float deltaTime)
            {
                // Update tank systems
                if (_isReloading)
                {
                    // Handle reload timing
                }
            }

            public void Reset()
            {
                _turretAngle = 0f;
                _turretElevation = 0f;
                _isReloading = false;
            }

            #region ITankControl Implementation
            public void RotateTurret(float angle, float speed)
            {
                float maxSpeed = _config.GetFloat("turret_rotation_speed", 45f);
                speed = Mathf.Clamp(speed, 0f, maxSpeed);

                _turretAngle = Mathf.MoveTowards(_turretAngle, angle, speed * Time.deltaTime);
                _config.SetFloat("turret_angle", _turretAngle);
            }

            public void ElevateTurret(float angle, float speed)
            {
                float maxSpeed = _config.GetFloat("turret_elevation_speed", 25f);
                speed = Mathf.Clamp(speed, 0f, maxSpeed);

                float minElevation = _config.GetFloat("turret_min_elevation", -10f);
                float maxElevation = _config.GetFloat("turret_max_elevation", 20f);

                angle = Mathf.Clamp(angle, minElevation, maxElevation);
                _turretElevation = Mathf.MoveTowards(_turretElevation, angle, speed * Time.deltaTime);
                _config.SetFloat("turret_elevation", _turretElevation);
            }

            public void FireWeapon()
            {
                if (_ammoCount > 0 && !_isReloading)
                {
                    _ammoCount--;
                    _config.SetInt("ammo_count", _ammoCount);

                    // Trigger fire event
                    OnWeaponFired?.Invoke(_currentAmmoType);
                }
            }

            public void Reload()
            {
                if (!_isReloading)
                {
                    _isReloading = true;
                    float reloadTime = _config.GetFloat("reload_time", 8.5f);
                    // Start reload coroutine or timer
                }
            }

            public void SetTrackSpeed(float leftSpeed, float rightSpeed)
            {
                _config.SetFloat("left_track_speed", leftSpeed);
                _config.SetFloat("right_track_speed", rightSpeed);
            }

            public void SetTrackBrake(float leftBrake, float rightBrake)
            {
                _config.SetFloat("left_track_brake", leftBrake);
                _config.SetFloat("right_track_brake", rightBrake);
            }

            public void SwitchAmmoType(AmmoType type)
            {
                _currentAmmoType = type;
                _config.SetEnum("ammo_type", type);
            }

            public void SetFireMode(FireMode mode)
            {
                _currentFireMode = mode;
                _config.SetEnum("fire_mode", mode);
            }

            public float GetTurretAngle() => _turretAngle;
            public float GetTurretElevation() => _turretElevation;
            public int GetAmmoCount() => _ammoCount;
            public bool IsReloading() => _isReloading;
            #endregion

            #region Events
            public event Action<AmmoType> OnWeaponFired;
            public event Action OnReloadComplete;
            #endregion
        }

        /// VTOL control implementation
        public class VTOLControl : IVTOLControl
        {
            public string ControlName => "VTOL Control System";
            public ControlType Type => ControlType.VTOL;
            public bool IsActive { get; private set; }

            private VehicleConfig _config;
            private FlightMode _currentFlightMode;
            private float _thrustVectorHorizontal;
            private float _thrustVectorVertical;
            private float _enginePower;
            private bool _isTransitioning;

            public void Initialize(VehicleConfig config)
            {
                _config = config;
                IsActive = true;

                _currentFlightMode = config.GetEnum("default_flight_mode", FlightMode.Hover);
                _enginePower = config.GetFloat("engine_power", 0.75f);
            }

            public void Update(float deltaTime)
            {
                // Handle flight mode transitions
                if (_isTransitioning)
                {
                    // Process transition logic
                }
            }

            public void Reset()
            {
                _currentFlightMode = FlightMode.Hover;
                _isTransitioning = false;
                _enginePower = 0f;
            }

            #region IVTOLControl Implementation
            public void SetFlightMode(FlightMode mode)
            {
                if (_currentFlightMode != mode)
                {
                    _currentFlightMode = mode;
                    _config.SetEnum("current_flight_mode", mode);
                }
            }

            public void TransitionToHover()
            {
                if (_currentFlightMode != FlightMode.Hover)
                {
                    _isTransitioning = true;
                    _currentFlightMode = FlightMode.Transition;
                    // Start transition coroutine
                }
            }

            public void TransitionToForwardFlight()
            {
                if (_currentFlightMode != FlightMode.ForwardFlight)
                {
                    _isTransitioning = true;
                    _currentFlightMode = FlightMode.Transition;
                    // Start transition coroutine
                }
            }

            public void SetThrustVectoring(float horizontal, float vertical)
            {
                float maxRange = _config.GetFloat("thrust_vectoring_range", 45f);
                _thrustVectorHorizontal = Mathf.Clamp(horizontal, -maxRange, maxRange);
                _thrustVectorVertical = Mathf.Clamp(vertical, -maxRange, maxRange);

                _config.SetFloat("thrust_vector_horizontal", _thrustVectorHorizontal);
                _config.SetFloat("thrust_vector_vertical", _thrustVectorVertical);
            }

            public void SetEnginePower(float power)
            {
                _enginePower = Mathf.Clamp01(power);
                _config.SetFloat("engine_power", _enginePower);
            }

            public void SetHoverHeight(float height)
            {
                _config.SetFloat("target_hover_height", height);
            }

            public void SetHoverStability(float stability)
            {
                _config.SetFloat("hover_stability", Mathf.Clamp01(stability));
            }

            public FlightMode GetCurrentFlightMode() => _currentFlightMode;
            public float GetThrustVectorAngle() => Mathf.Sqrt(_thrustVectorHorizontal * _thrustVectorHorizontal + _thrustVectorVertical * _thrustVectorVertical);
            public bool IsInTransition() => _isTransitioning;
            #endregion
        }


        /// Construction equipment control implementation
        public class ConstructionControl : IConstructionControl
        {
            public string ControlName => "Construction Control System";
            public ControlType Type => ControlType.Construction;
            public bool IsActive { get; private set; }

            private VehicleConfig _config;
            private float _armAngle;
            private float _armExtension;
            private float _armRotation;
            private float _bucketAngle;
            private float _bucketOpen;
            private bool _outriggersDeployed;
            private bool _stabilizationActive;

            public void Initialize(VehicleConfig config)
            {
                _config = config;
                IsActive = true;

                _stabilizationActive = config.GetBool("auto_stabilization", true);
                _outriggersDeployed = false;
            }

            public void Update(float deltaTime)
            {
                // Update hydraulic systems
                // Monitor stability
                // Check load limits
            }

            public void Reset()
            {
                _armAngle = 0f;
                _armExtension = 0f;
                _armRotation = 0f;
                _bucketAngle = 0f;
                _bucketOpen = 0f;
                _outriggersDeployed = false;
            }

            #region IConstructionControl Implementation
            public void SetArmAngle(float angle)
            {
                float maxAngle = _config.GetFloat("arm_max_angle", 160f);
                float minAngle = _config.GetFloat("arm_min_angle", -30f);

                _armAngle = Mathf.Clamp(angle, minAngle, maxAngle);
                _config.SetFloat("arm_angle", _armAngle);
            }

            public void SetArmExtension(float extension)
            {
                float maxExtension = _config.GetFloat("arm_max_extension", 6.2f);
                _armExtension = Mathf.Clamp01(extension) * maxExtension;
                _config.SetFloat("arm_extension", _armExtension);
            }

            public void SetArmRotation(float rotation)
            {
                _armRotation = Mathf.Clamp(rotation, -180f, 180f);
                _config.SetFloat("arm_rotation", _armRotation);
            }

            public void SetBucketAngle(float angle)
            {
                _bucketAngle = Mathf.Clamp(angle, -90f, 90f);
                _config.SetFloat("bucket_angle", _bucketAngle);
            }

            public void SetBucketOpen(float openAmount)
            {
                _bucketOpen = Mathf.Clamp01(openAmount);
                _config.SetFloat("bucket_open", _bucketOpen);
            }

            public void SetAttachmentType(AttachmentType type)
            {
                _config.SetEnum("attachment_type", type);
            }

            public void SetHydraulicPressure(float pressure)
            {
                float maxPressure = _config.GetFloat("hydraulic_pressure", 280f);
                pressure = Mathf.Clamp(pressure, 0f, maxPressure);
                _config.SetFloat("hydraulic_pressure_current", pressure);
            }

            public void SetHydraulicFlow(float flow)
            {
                float maxFlow = _config.GetFloat("hydraulic_flow_rate", 205f);
                flow = Mathf.Clamp(flow, 0f, maxFlow);
                _config.SetFloat("hydraulic_flow_current", flow);
            }

            public void DeployOutriggers()
            {
                _outriggersDeployed = true;
                _config.SetBool("outriggers_deployed", true);
            }

            public void RetractOutriggers()
            {
                _outriggersDeployed = false;
                _config.SetBool("outriggers_deployed", false);
            }

            public void SetStabilization(bool enabled)
            {
                _stabilizationActive = enabled;
                _config.SetBool("stabilization_active", enabled);
            }

            public float GetArmAngle() => _armAngle;
            public float GetArmExtension() => _armExtension;
            public bool IsStable() => _stabilizationActive && _outriggersDeployed;
            public float GetLoadWeight() => _config.GetFloat("current_load_weight", 0f);
            #endregion
        }


        /// Marine vessel control implementation
        public class MarineControl : IMarineControl
        {
            public string ControlName => "Marine Control System";
            public ControlType Type => ControlType.Marine;
            public bool IsActive { get; private set; }

            private VehicleConfig _config;
            private float _propellerSpeed;
            private float _rudderAngle;
            private float _thrustVector;
            private float _ballastLevel;
            private float _trimAngle;
            private bool _autopilotActive;
            private Vector3 _currentWaypoint;

            public void Initialize(VehicleConfig config)
            {
                _config = config;
                IsActive = true;
            }

            public void Update(float deltaTime)
            {
                // Update marine systems
                // Handle autopilot navigation
                // Monitor vessel stability
            }

            public void Reset()
            {
                _propellerSpeed = 0f;
                _rudderAngle = 0f;
                _thrustVector = 0f;
                _ballastLevel = 0.5f;
                _trimAngle = 0f;
                _autopilotActive = false;
            }

            #region IMarineControl Implementation
            public void SetPropellerSpeed(float speed)
            {
                _propellerSpeed = Mathf.Clamp(speed, -1f, 1f);
                _config.SetFloat("propeller_speed", _propellerSpeed);
            }

            public void SetRudderAngle(float angle)
            {
                _rudderAngle = Mathf.Clamp(angle, -45f, 45f);
                _config.SetFloat("rudder_angle", _rudderAngle);
            }

            public void SetThrustVector(float vector)
            {
                _thrustVector = Mathf.Clamp(vector, -1f, 1f);
                _config.SetFloat("thrust_vector", _thrustVector);
            }

            public void SetBallastLevel(float level)
            {
                _ballastLevel = Mathf.Clamp01(level);
                _config.SetFloat("ballast_level", _ballastLevel);
            }

            public void SetTrimAngle(float angle)
            {
                _trimAngle = Mathf.Clamp(angle, -15f, 15f);
                _config.SetFloat("trim_angle", _trimAngle);
            }

            public void SetAutopilot(bool enabled)
            {
                _autopilotActive = enabled;
                _config.SetBool("autopilot_active", enabled);
            }

            public void SetWaypoint(Vector3 waypoint)
            {
                _currentWaypoint = waypoint;
                _config.SetVector3("current_waypoint", waypoint);
            }

            public float GetSpeedKnots()
            {
                return Mathf.Abs(_propellerSpeed) * _config.GetFloat("max_speed_knots", 30f);
            }

            public float GetHeading()
            {
                return _config.GetFloat("current_heading", 0f);
            }

            public float GetDepth()
            {
                return _config.GetFloat("current_depth", 0f);
            }
            #endregion
        }
        #endregion

        #region Control Manager
        private readonly Dictionary<ControlType, ISpecializedControl> _controls = new();
        private VehicleEditorContext _context;

        public void Initialize(VehicleEditorContext context)
        {
            _context = context;
        }

        public void RegisterControl(ISpecializedControl control)
        {
            _controls[control.Type] = control;
            control.Initialize(_context.Config);
        }

        public void UnregisterControl(ControlType type)
        {
            if (_controls.ContainsKey(type))
            {
                _controls[type].Reset();
                _controls.Remove(type);
            }
        }

        public T GetControl<T>(ControlType type) where T : ISpecializedControl
        {
            if (_controls.ContainsKey(type))
                return (T)_controls[type];
            return default;
        }

        public void UpdateControls(float deltaTime)
        {
            foreach (var control in _controls.Values)
            {
                if (control.IsActive)
                    control.Update(deltaTime);
            }
        }

        public void ResetAllControls()
        {
            foreach (var control in _controls.Values)
            {
                control.Reset();
            }
        }
        #endregion
    }

    #region Supporting Enums and Types
    public enum ControlType
    {
        Tank,
        VTOL,
        Construction,
        Marine
    }

    public enum FireMode
    {
        Single,
        Burst,
        Auto
    }

    public enum FlightMode
    {
        Hover,
        ForwardFlight,
        Transition,
        Auto
    }

    public enum AttachmentType
    {
        StandardBucket,
        HeavyDutyBucket,
        RockBucket,
        HydraulicHammer,
        Grapple,
        Auger,
        Ripper,
        Blade,
        Hook,
        Forks
    }

    public enum AmmoType
    {
        APFSDS,      // Armor-Piercing Fin-Stabilized Discarding Sabot
        HEAT,        // High-Explosive Anti-Tank
        HE,          // High-Explosive
        APHE,        // Armor-Piercing High-Explosive
        Canister,    // Anti-personnel
        Smoke        // Smoke rounds
    }

    [Serializable]
    public class ControlBinding
    {
        public string ActionName { get; set; }
        public KeyCode PrimaryKey { get; set; }
        public KeyCode SecondaryKey { get; set; }
        public string GamepadButton { get; set; }
        public float Sensitivity { get; set; }
        public bool IsInverted { get; set; }
    }
    #endregion
}