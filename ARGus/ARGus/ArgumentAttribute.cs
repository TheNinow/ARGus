using System;

namespace ARGus
{
    public abstract class ArgumentAttribute : Attribute
    {
        public readonly string Name, Description;

        protected ArgumentAttribute(string name = "", string description = "")
        {
            Name = name;
            Description = description;
        }
    }
}
