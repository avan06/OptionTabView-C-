using System;
using System.ComponentModel;
using System.Configuration;

namespace OptionTreeView
{
    public class BaseOption
    {
        public string TreeName { get; private set; }
        public string GroupName { get; private set; }
        public string Description { get; private set; }
        public BaseOption(string treeName, string groupName, string description)
        {
            TreeName = treeName;
            GroupName = groupName;
            Description = description;
        }
    }

    /// <summary>
    /// Using Custom Classes with Application Settings
    /// http://www.blackwasp.co.uk/CustomAppSettings.aspx
    /// An alternative solution using TypeDescriptionProviderAttribute (TypeDescriptor.GetConverter(typeof(Foo)) will work as intended.)
    /// https://stackoverflow.com/a/53771936
    /// generic types for Settings.settings (it is possible to do this by manually editing the setting XML file.)
    /// https://stackoverflow.com/a/4046036
    /// </summary>
    [TypeConverter(typeof(OptionTypeConverter))]
    [SettingsSerializeAs(SettingsSerializeAs.String)]
    public class Option<T> : BaseOption
    {
        public T Value { get; set; }
        public Option(T value) : this(value, "Default", "Default", "") { }
        public Option(T value, string treeName, string groupName, string description) : base(treeName, groupName, description) => Value = value;
        public override string ToString() => Value == null ? "" : Value.ToString();
    }

    public class OptionTypeConverter : TypeConverter
    {
        public static char Separator { get; set; } = '|';
        public Type genericInstanceType { get; private set; }
        public Type innerType { get; private set; }
        public Type typeOfOption { get; private set; }
        public Type typeOfOptionWithGenericArgument { get; private set; }
        public TypeConverter innerTypeConverter { get; private set; }

        /// <summary>
        /// Creating an single Converter that could hanlde all of the types derrived
        /// https://stackoverflow.com/a/14980794
        /// </summary>
        public OptionTypeConverter(Type type)
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Option<>) || type.GetGenericArguments().Length != 1)
                throw new ArgumentException(String.Format("OptionTypeConverter: Incompatible type: {0}", type), "type");

            genericInstanceType = type;
            innerType = type.GetGenericArguments()[0];
            innerTypeConverter = TypeDescriptor.GetConverter(innerType);
            typeOfOption = typeof(Option<>);
            typeOfOptionWithGenericArgument = typeOfOption.MakeGenericType(innerType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

        /// <summary>
        /// With reflection and without the code knowing anything about 't', the closest you can get is an interface for MyType
        /// https://www.reddit.com/r/csharp/comments/f2t0hw/comment/fherb76/?utm_source=share&utm_medium=web2x&context=3
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (!(value is string str)) { return base.ConvertFrom(context, culture, value); }

            string[] parts = str.Split(new char[] { Separator });
            return (BaseOption)Activator.CreateInstance(typeOfOptionWithGenericArgument, innerTypeConverter.ConvertFrom(parts[0]),
                parts.Length > 1 ? parts[1] : "Default",
                parts.Length > 2 ? parts[2] : "Default",
                parts.Length > 3 ? parts[3] : "");
        }

        /// <summary>
        /// Cast generic type without knowing T
        /// https://social.msdn.microsoft.com/Forums/en-US/e1f9a9c0-ddb7-41b8-aad8-c2c4a8ef5e84/cast-generic-type-without-knowing-t?forum=aspcsharp
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != typeof(string) || !genericInstanceType.IsGenericType || genericInstanceType.GetGenericTypeDefinition() != typeof(Option<>))
                return base.ConvertTo(context, culture, value, destinationType);

            return string.Format("{0}" + Separator + "{1}" + Separator + "{2}" + Separator + "{3}",
                genericInstanceType.GetProperty("Value").GetValue(value, null),
                genericInstanceType.GetProperty("TreeName").GetValue(value, null),
                genericInstanceType.GetProperty("GroupName").GetValue(value, null),
                genericInstanceType.GetProperty("Description").GetValue(value, null));
        }
    }
}
