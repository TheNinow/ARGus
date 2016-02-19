using System;

namespace ARGus
{
    public class SwitchArgumentAttribute : ArgumentAttribute
    {
        public readonly char ShortName;

        public SwitchArgumentAttribute(string name, char shortName = char.MinValue, string description = "") : base(name, description)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 2) throw new ArgumentException(nameof(name) + " has to have a minimum length of 2!");
            ShortName = shortName;
        }
    }
}
