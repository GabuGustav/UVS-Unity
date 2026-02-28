using System;
using System.Collections.Generic;
using UnityEngine;

namespace UVS.Editor.Core
{
    [CreateAssetMenu(menuName = "UVS/Editor/Module Compatibility Profile", fileName = "ModuleCompatibilityProfile")]
    public class ModuleCompatibilityProfile : ScriptableObject
    {
        [Serializable]
        public class ModuleRule
        {
            public string moduleId;
            public string typeId;
            public string categoryId;
            public string subcategoryId;
            public bool enabled = true;
        }

        public const string DefaultResourcePath = "ModuleCompatibilityProfile_Default";
        public List<ModuleRule> overrides = new();

        public static ModuleCompatibilityProfile GetDefault()
        {
            return Resources.Load<ModuleCompatibilityProfile>(DefaultResourcePath);
        }
    }
}
