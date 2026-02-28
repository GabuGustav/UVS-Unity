using UnityEngine;
using UnityEngine.UIElements;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    public class WelcomeModule : VehicleEditorModuleBase
    {
        public override string ModuleId => "welcome";
        public override string DisplayName => "Welcome";
        public override int Priority => 0;
        public override bool RequiresVehicle => false;

        protected override VisualElement CreateModuleUI()
        {
            var container = new VisualElement
            {
                style =
                {
                    paddingLeft = 20,
                    paddingRight = 20,
                    paddingTop = 20,
                    paddingBottom = 20,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
                }
            };

            var titleLabel = new Label("Welcome to the Vehicle Editor System")
            {
                style =
                {
                    fontSize = 24,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.white,
                    marginBottom = 20
                }
            };
            container.Add(titleLabel);

            var descriptionLabel = new Label(
                "This editor allows you to configure and customize vehicles.\n\n" +
                "Features:\n" +
                "- Drag and drop vehicle prefabs\n" +
                "- Automatic part classification\n" +
                "- Real-time 3D preview\n" +
                "- Comprehensive configuration options\n" +
                "- Export/import capabilities\n\n" +
                "Use New Vehicle, Load Vehicle, or Help to get started."
            )
            {
                style =
                {
                    fontSize = 14,
                    color = new Color(0.9f, 0.9f, 0.9f, 1f),
                    whiteSpace = WhiteSpace.Normal
                }
            };
            container.Add(descriptionLabel);

            var quickActionsContainer = new VisualElement
            {
                style =
                {
                    marginTop = 20,
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap
                }
            };

            var newVehicleButton = new Button(() => TriggerContextCommand(_context?.RequestNewVehicle, "New Vehicle"))
            {
                text = "New Vehicle",
                style = { marginRight = 10, marginBottom = 10 }
            };
            quickActionsContainer.Add(newVehicleButton);

            var loadVehicleButton = new Button(() => TriggerContextCommand(_context?.RequestLoadVehicle, "Load Vehicle"))
            {
                text = "Load Vehicle",
                style = { marginRight = 10, marginBottom = 10 }
            };
            quickActionsContainer.Add(loadVehicleButton);

            var helpButton = new Button(() => TriggerContextCommand(_context?.RequestHelp, "Help"))
            {
                text = "Help",
                style = { marginRight = 10, marginBottom = 10 }
            };
            quickActionsContainer.Add(helpButton);

            container.Add(quickActionsContainer);

            return container;
        }

        protected override ValidationResult ValidateModule()
        {
            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            // Welcome module is not config dependent.
        }

        protected override void OnModuleActivated()
        {
            LogMessage("Welcome module activated");
        }

        private void TriggerContextCommand(System.Action command, string actionName)
        {
            if (command == null)
            {
                LogWarning($"{actionName} action is unavailable.");
                return;
            }

            command.Invoke();
        }
    }
}
