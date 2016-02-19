namespace ARGus
{
    public class ExplicitArgumentAttribute : SwitchArgumentAttribute
    {
        public ExplicitArgumentAttribute(string name, char shortName = char.MinValue, string description = "") : base(name, shortName, description)
        {
        }
    }
}
