﻿using System;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OptionTreeView
{
    [DataContract]
    public class OptionJson
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public Object Value { get; set; }
        public OptionJson(string name, Object value)
        {
            Name = name;
            Value = value;
        }
    }

    public class BaseOption
    {
        public dynamic BaseObject { get; set; }
        public string TreeName { get; private set; }
        public string GroupName { get; private set; }
        public string Description { get; private set; }
        public dynamic MinObject { get; private set; }
        public dynamic MaxObject { get; private set; }
        public BaseOption(dynamic baseObject, string treeName, string groupName, string description, dynamic minObject, dynamic maxObject)
        {
            BaseObject = baseObject;
            TreeName = treeName;
            GroupName = groupName;
            Description = description;
            MinObject = minObject;
            MaxObject = maxObject;
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
    public class Option<Dynamic> : BaseOption
    {
        public Dynamic Value
        {
            get => (Dynamic)BaseObject;
            set
            {
                if (MinObject != null && MinObject.GetType().Name != "String" && value < MinObject) value = MinObject;
                if (MaxObject != null && MaxObject.GetType().Name != "String" && value > MaxObject) value = MaxObject;
                BaseObject = value;
            }
        }
        public Option(Dynamic value, string treeName = "Default", string groupName = "Default", string description = "", object minObject = null, object maxObject = null) : base(value, treeName, groupName, description, minObject, maxObject) { }
        public override string ToString()
        {
            string result = "";
            if (Value == null) result = "";
            else if (Value is Color color) result = color.ToArgb().ToString("X");
            else result = Value.ToString();
            return result;
        }
    }

    public class OptionTypeConverter : TypeConverter
    {
        public static char Separator { get; set; } = '|';
        public Type GenericInstanceType { get; private set; }
        public Type InnerType { get; private set; }
        public Type TypeOfOption { get; private set; }
        public Type TypeOfOptionWithGenericArgument { get; private set; }
        public TypeConverter InnerTypeConverter { get; private set; }

        /// <summary>
        /// Creating an single Converter that could hanlde all of the types derrived
        /// https://stackoverflow.com/a/14980794
        /// </summary>
        public OptionTypeConverter(Type type)
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Option<>) || type.GetGenericArguments().Length != 1)
                throw new ArgumentException(String.Format("OptionTypeConverter: Incompatible type: {0}", type), "type");

            GenericInstanceType = type;
            InnerType = type.GetGenericArguments()[0];
            InnerTypeConverter = TypeDescriptor.GetConverter(InnerType);
            TypeOfOption = typeof(Option<>);
            TypeOfOptionWithGenericArgument = TypeOfOption.MakeGenericType(InnerType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

        /// <summary>
        /// With reflection and without the code knowing anything about 't', the closest you can get is an interface for MyType
        /// https://www.reddit.com/r/csharp/comments/f2t0hw/comment/fherb76/?utm_source=share&utm_medium=web2x&context=3
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null) return (BaseOption)Activator.CreateInstance(TypeOfOptionWithGenericArgument, null, "Default", "Default", "");
            if (!(value is string str)) return base.ConvertFrom(context, culture, value);

            string[] parts = str.Split(new char[] { Separator });

            object obj, minObj = null, maxObj = null;
            if (InnerType.Name == "FontFamily")
                obj = Regex.Match(parts[0], @"\[FontFamily: *Name=([^]]+)\]") is Match m1 && m1.Success ? new FontFamily(m1.Groups[1].Value) : SystemFonts.DefaultFont.FontFamily;
            else if (InnerType.Name == "Color")
            {
                bool isARGB = uint.TryParse(parts[0], NumberStyles.HexNumber, null, out uint argb);
                if (argb < 0x01000000) argb += 0xFF000000;
                obj = isARGB ? Color.FromArgb((int)argb) : Color.FromName(parts[0]);
            }
            else if (InnerType.Name == "Keys")
            {
                string keyCombination = parts[0];
                // Initialize default states for Keys
                bool control = false;
                bool shift = false;
                bool alt = false;
                Keys keyCode = Keys.None;

                // Split the input string by '+' and trim spaces
                var keyParts = keyCombination.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in keyParts)
                {
                    string trimmedPart = part.Trim();

                    // Check for modifier keys
                    if (string.Equals(trimmedPart, "Ctrl", StringComparison.OrdinalIgnoreCase))
                        control = true;
                    else if (string.Equals(trimmedPart, "Shift", StringComparison.OrdinalIgnoreCase))
                        shift = true;
                    else if (string.Equals(trimmedPart, "Alt", StringComparison.OrdinalIgnoreCase))
                        alt = true;
                    else
                    { // Assume the remaining part is the main key
                        if (Enum.TryParse(trimmedPart, true, out Keys parsedKey)) keyCode = parsedKey;
                        else throw new ArgumentException($"Invalid key: {trimmedPart}");
                    }
                }
                obj = keyCode = (control ? Keys.Control : Keys.None) | (shift ? Keys.Shift : Keys.None) | (alt ? Keys.Alt : Keys.None) | keyCode;
            }
            else
            {
                obj = InnerTypeConverter.ConvertFrom(parts[0]);
                minObj = (parts.Length > 4 && parts[4] != "") ? InnerTypeConverter.ConvertFrom(parts[4]) : null;
                maxObj = (parts.Length > 5 && parts[5] != "") ? InnerTypeConverter.ConvertFrom(parts[5]) : null;
            }

            return (BaseOption)Activator.CreateInstance(TypeOfOptionWithGenericArgument, obj,
                parts.Length > 1 ? parts[1] : "Default",
                parts.Length > 2 ? parts[2] : "Default",
                parts.Length > 3 ? parts[3] : "", minObj, maxObj);
        }

        /// <summary>
        /// Cast generic type without knowing T
        /// https://social.msdn.microsoft.com/Forums/en-US/e1f9a9c0-ddb7-41b8-aad8-c2c4a8ef5e84/cast-generic-type-without-knowing-t?forum=aspcsharp
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != typeof(string) || !GenericInstanceType.IsGenericType || GenericInstanceType.GetGenericTypeDefinition() != typeof(Option<>))
                return base.ConvertTo(context, culture, value, destinationType);

            var newVal = GenericInstanceType.GetProperty("Value").GetValue(value, null);
            if (InnerType.Name == "Color") newVal = ((Color)newVal).ToArgb().ToString("X");
            else if (InnerType.Name == "Keys") newVal = KeysToString((Keys)newVal);

            return string.Format("{0}" + Separator + "{1}" + Separator + "{2}" + Separator + "{3}" + Separator + "{4}" + Separator + "{5}",
                newVal,
                GenericInstanceType.GetProperty("TreeName").GetValue(value, null),
                GenericInstanceType.GetProperty("GroupName").GetValue(value, null),
                GenericInstanceType.GetProperty("Description").GetValue(value, null),
                GenericInstanceType.GetProperty("MinObject").GetValue(value, null),
                GenericInstanceType.GetProperty("MaxObject").GetValue(value, null));
        }

        public string KeysToString(Keys keys)
        {
            string keyCombination = "";

            if ((keys & Keys.Control) == Keys.Control)
                keyCombination += "Ctrl + ";
            if ((keys & Keys.Shift) == Keys.Shift)
                keyCombination += "Shift + ";
            if ((keys & Keys.Alt) == Keys.Alt)
                keyCombination += "Alt + ";

            keyCombination += (keys & ~Keys.Control & ~Keys.Shift & ~Keys.Alt).ToString();

            return keyCombination;
        }
    }
}
