using System;
using UnityEngine;

namespace PVZ3D.Interaction
{
    public class XRControllerTriggerGrabConfigurator : MonoBehaviour
    {
        [Header("Trigger Grab Mapping")]
        [SerializeField] private bool enforceTriggerForSelect = true;
        [SerializeField] private bool logConfigResult;
        [SerializeField] private float retryDurationSeconds = 3f;

        private float retryEndTime;
        private bool configuredAtLeastOnce;

        private void Start()
        {
            if (!enforceTriggerForSelect)
            {
                return;
            }

            retryEndTime = Time.unscaledTime + Mathf.Max(0.2f, retryDurationSeconds);
            configuredAtLeastOnce = ConfigureControllers() > 0;
        }

        private void Update()
        {
            if (!enforceTriggerForSelect || configuredAtLeastOnce)
            {
                return;
            }

            if (Time.unscaledTime > retryEndTime)
            {
                enabled = false;
                return;
            }

            configuredAtLeastOnce = ConfigureControllers() > 0;
        }

        private int ConfigureControllers()
        {
            Type actionBasedControllerType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.ActionBasedController, Unity.XR.Interaction.Toolkit");
            if (actionBasedControllerType == null)
            {
                return 0;
            }

            Component[] allComponents = FindObjectsByType<Component>(FindObjectsSortMode.None);
            int changed = 0;
            for (int i = 0; i < allComponents.Length; i++)
            {
                Component component = allComponents[i];
                if (component == null || !actionBasedControllerType.IsAssignableFrom(component.GetType()))
                {
                    continue;
                }

                object activateAction = GetProperty(component, "activateAction");
                if (activateAction == null)
                {
                    continue;
                }

                bool changedSelect = SetProperty(component, "selectAction", activateAction);
                object activateActionValue = GetProperty(component, "activateActionValue");
                bool changedSelectValue = activateActionValue != null && SetProperty(component, "selectActionValue", activateActionValue);

                if (changedSelect || changedSelectValue)
                {
                    changed++;
                }
            }

            if (logConfigResult)
            {
                Debug.Log($"PVZ3D: Trigger-grab configurator updated {changed} XR controller(s).");
            }

            return changed;
        }

        private static object GetProperty(Component target, string name)
        {
            var prop = target.GetType().GetProperty(name);
            if (prop == null || !prop.CanRead)
            {
                return null;
            }

            return prop.GetValue(target);
        }

        private static bool SetProperty(Component target, string name, object value)
        {
            var prop = target.GetType().GetProperty(name);
            if (prop == null || !prop.CanWrite || value == null)
            {
                return false;
            }

            if (!prop.PropertyType.IsInstanceOfType(value))
            {
                return false;
            }

            prop.SetValue(target, value);
            return true;
        }
    }
}
