using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ARGus
{
    public static class Argus
    {
        public static readonly List<string> OptionalParameterMarker = new List<string> { "/", "--", "-" };
        public static BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;

        /// <summary>
        /// Parses the given arguments and fills the data in a instance of the given generic type. The Properties or Fields of the generic type need to be marked with ExplicitArgument / ImplicitArgument or SwitchArgument Attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        /// <param name="success">If the given generic type worked for the given args</param>
        /// <returns></returns>
        public static T Parse<T>(string[] args, out bool success)
        {
            return Parse(args, Activator.CreateInstance<T>(), out success);
        }

        /// <summary>
        /// Parses the given arguments and fills the data in a instance of the given generic type. The Properties or Fields of the generic type need to be marked with ExplicitArgument / ImplicitArgument or SwitchArgument Attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T Parse<T>(string[] args)
        {
            bool success;
            return Parse<T>(args, out success);
        }

        /// <summary>
        /// Parses the given arguments and fills the data in a instance of the given generic type. The Properties or Fields of the generic type need to be marked with ExplicitArgument / ImplicitArgument or SwitchArgument Attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        /// <param name="success">If the given generic type worked for the given args</param>
        /// <returns></returns>
        public static T Parse<T>(string[] args, T options, out bool success)
        {
            success = true;

            var items = options.GetType().GetProperties(Flags).Select(item => new PropertyContainer(item)).Concat<MemberContainer>(options.GetType().GetFields(Flags).Select(item => new FieldContainer(item))).ToArray();

            var implicitAttribs = GetAttributeMembers<ImplicitArgumentAttribute>(items).OrderBy(item => item.Item1.Order).ToArray();
            var explicitAttribs = GetAttributeMembers<ExplicitArgumentAttribute>(items).ToArray();
            var switchAttribs = GetAttributeMembers<SwitchArgumentAttribute>(items).ToArray();

            if (implicitAttribs.Any(item => item.Item1.IsParams && item.Item2.DataType != typeof(IEnumerable<string>)))
                throw new InvalidOperationException("All " + typeof(ImplicitArgumentAttribute).Name + " items with params set have to be of type IEnumerable<string>!");

            var implicitAttribQueue = new Queue<Tuple<ImplicitArgumentAttribute, MemberContainer>>(implicitAttribs);

            var paramsAttrib = implicitAttribs.FirstOrDefault(item => item.Item1.IsParams);

            if (paramsAttrib != null && !(paramsAttrib.Item2.DataType == typeof(IEnumerable<string>))) throw new InvalidOperationException("A params item must be of type IEnumerable<string>");
           
            var awaitParameter = false;
            MemberContainer awaitContainer = null;
            foreach (var arg in args)
            {
                var foundCandidate = false;
                foreach (var s in OptionalParameterMarker)
                {
                    if (arg.StartsWith(s))
                    {
                        foundCandidate = true;
                        var name = arg.Substring(s.Length);
                        var explItem = explicitAttribs.FirstOrDefault(item => item.Item1.Name.ToLower() == name.ToLower() || item.Item1.ShortName.ToString().ToLower() == name.ToLower());
                        if (explItem != null)
                        {
                            awaitContainer = explItem.Item2;
                            if (awaitParameter)
                                success = false;
                            awaitParameter = true;
                        }
                        else
                        {
                            switchAttribs.FirstOrDefault(item => item.Item1.Name == name || item.Item1.ShortName.ToString() == name)?.Item2.SetValue(true, options);
                        }
                    }
                }
                if (foundCandidate)
                    continue;

                if (awaitParameter)
                {
                    awaitContainer.SetValue(arg, options);
                    continue;
                }

                if (implicitAttribQueue.Count == 1 && paramsAttrib != null)
                {
                    AddItemToParamsAttribute(arg, paramsAttrib.Item2, options);
                    continue;
                }
                if (implicitAttribQueue.Count == 0)
                {
                    success = false;
                    continue;
                }

                var nextImplicitItem = implicitAttribQueue.Dequeue();
                nextImplicitItem.Item2.SetValue(arg, options);
            }

            if (implicitAttribQueue.Count > 0 || awaitParameter) success = false;

            return options;
        }

        /// <summary>
        /// Writes the usage (Lists every parameter, with description) to the given TextWriter (e.g: Console.Out)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="writer"></param>
        public static void PrintUsage<T>(TextWriter writer)
        {
            var items = typeof(T).GetProperties(Flags).Select(item => new PropertyContainer(item)).Concat<MemberContainer>(typeof(T).GetFields(Flags).Select(item => new FieldContainer(item))).ToArray();

            var implicitAttribs = GetAttributeMembers<ImplicitArgumentAttribute>(items).OrderBy(item => item.Item1.Order).ToArray();
            var explicitAttribs = GetAttributeMembers<ExplicitArgumentAttribute>(items).ToArray();
            var switchAttribs = GetAttributeMembers<SwitchArgumentAttribute>(items).ToArray();

            var fileName = Path.GetFileName(Assembly.GetEntryAssembly().Location);

            var usage = "Usage: " + fileName + " ";
            if (implicitAttribs.Any())
                usage += "{" + string.Join("} {", implicitAttribs.Select(item => item.Item1.Name)) + "} ";
            if (explicitAttribs.Any())
                usage += "[" + string.Join(" value] [", explicitAttribs.Select(item => item.Item1.Name + "|" + item.Item1.ShortName)) + " value] ";
            if (switchAttribs.Any())
                usage += "[" + string.Join("] [", switchAttribs.Select(item => item.Item1.Name + "|" + item.Item1.ShortName)) + "] ";

            var maxCharSpace = Math.Max(Math.Max(implicitAttribs.Any() ? implicitAttribs.Max(item => item.Item1.Name.Length) : 0, explicitAttribs.Any() ? explicitAttribs.Max(item => ShortAsString(item.Item1).Length + item.Item1.Name.Length + 4) : 0), switchAttribs.Any() ? switchAttribs.Max(item => ShortAsString(item.Item1).Length + item.Item1.Name.Length + 4) : 0);

            writer.WriteLine(usage + "\n");
            if (implicitAttribs.Any())
            {
                //writer.WriteLine("* Mandatory items:");
                foreach (var implicitAttrib in implicitAttribs.Where(item => !string.IsNullOrEmpty(item.Item1.Description) && !string.IsNullOrWhiteSpace(item.Item1.Description) || item.Item1.IsParams))
                {
                    var title = implicitAttrib.Item1.Name;
                    writer.WriteLine(title + ": " + GetEmptySpaces(maxCharSpace - title.Length) + (implicitAttrib.Item1.IsParams ? "Is a List. All non-optional items after this will get added to this list! " : string.Empty) + implicitAttrib.Item1.Description);
                }
                writer.WriteLine();
            }
            if (explicitAttribs.Any())
            {
                //writer.WriteLine("* Optional items with values:");
                foreach (var explicitAttrib in explicitAttribs.Where(item => !string.IsNullOrEmpty(item.Item1.Description) && !string.IsNullOrWhiteSpace(item.Item1.Description)))
                {
                    var title = "-" + explicitAttrib.Item1.ShortName + " --" + explicitAttrib.Item1.Name.ToLower();
                    writer.WriteLine(title + ": " + GetEmptySpaces(maxCharSpace - title.Length) + explicitAttrib.Item1.Description);
                }
                writer.WriteLine();
            }
            if (switchAttribs.Any())
            {
                //writer.WriteLine("* Optional items (switches):");
                foreach (var switchAttrib in switchAttribs.Where(item => !string.IsNullOrEmpty(item.Item1.Description) && !string.IsNullOrWhiteSpace(item.Item1.Description)))
                {
                    var title = "-" + switchAttrib.Item1.ShortName + " --" + switchAttrib.Item1.Name.ToLower();
                    writer.WriteLine(title + ": " + GetEmptySpaces(maxCharSpace - title.Length) + switchAttrib.Item1.Description);
                }
                writer.WriteLine();
            }

        }

        private static string ShortAsString(SwitchArgumentAttribute attrib)
        {
            return attrib.ShortName == char.MinValue ? string.Empty : attrib.ShortName.ToString();
        }

        private static string GetEmptySpaces(int count)
        {
            return new string(' ', count);
        }

        /// <summary>
        /// Writes the usage (Lists every parameter, with description) to the Console
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void PrintUsage<T>()
        {
            PrintUsage<T>(Console.Out);
        }

        private static void AddItemToParamsAttribute<T>(string value, MemberContainer container, T options)
        {
            var enumerable = (IEnumerable<string>)container.GetValue(options) ?? new List<string>();
            enumerable = enumerable.Concat(new[] { value });
            container.SetValue(enumerable, options);
        }

        private static IEnumerable<Tuple<T, MemberContainer>> GetAttributeMembers<T>(IEnumerable<MemberContainer> container) where T : Attribute
        {
            return from memberContainer in container let attrib = Attribute.GetCustomAttribute(memberContainer.MemberInfo, typeof(T)) where attrib != null && attrib.GetType() == typeof(T) select new Tuple<T, MemberContainer>((T)attrib, memberContainer);
        }

        private abstract class MemberContainer
        {
            public abstract Type DataType { get; }
            public abstract MemberInfo MemberInfo { get; }
            public abstract object GetValue<T>(T obj);
            public abstract void SetValue<T, TU>(T value, TU obj);
        }

        private class PropertyContainer : MemberContainer
        {
            private readonly PropertyInfo _propInfo;
            public override Type DataType => _propInfo.PropertyType;
            public override MemberInfo MemberInfo => _propInfo;

            public PropertyContainer(PropertyInfo propInfo)
            {
                _propInfo = propInfo;
            }

            public override object GetValue<T>(T obj)
            {
                return _propInfo.GetValue(obj);
            }

            public override void SetValue<T, TU>(T value, TU obj)
            {
                object data;
                try
                {
                    data = Convert.ChangeType(value, DataType);
                }
                catch
                {
                    data = DataType.IsValueType ? Activator.CreateInstance(DataType) : null;
                }
                _propInfo.SetValue(obj, data);
            }
        }

        private class FieldContainer : MemberContainer
        {
            private readonly FieldInfo _fieldInfo;
            public override Type DataType => _fieldInfo.FieldType;
            public override MemberInfo MemberInfo => _fieldInfo;

            public FieldContainer(FieldInfo fieldInfo)
            {
                _fieldInfo = fieldInfo;
            }

            public override object GetValue<T>(T obj)
            {
                return _fieldInfo.GetValue(obj);
            }

            public override void SetValue<T, TU>(T value, TU obj)
            {
                object data;
                try
                {
                    data = Convert.ChangeType(value, DataType);
                }
                catch
                {
                    data = DataType.IsValueType ? Activator.CreateInstance(DataType) : null;
                }
                _fieldInfo.SetValue(obj, data);
            }
        }
    }
}
