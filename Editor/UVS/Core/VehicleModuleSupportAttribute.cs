using System;

namespace UVS.Editor.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class VehicleModuleSupportAttribute : Attribute
    {
        public string typeId;
        public string categoryId;
        public string subcategoryId;

        public VehicleModuleSupportAttribute(string typeId = null, string categoryId = null, string subcategoryId = null)
        {
            this.typeId = typeId;
            this.categoryId = categoryId;
            this.subcategoryId = subcategoryId;
        }
    }
}
