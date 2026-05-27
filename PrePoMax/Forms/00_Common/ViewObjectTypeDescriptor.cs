using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Collections;

public sealed class ViewObjectTypeDescriptor : ICustomTypeDescriptor
{
    private readonly object _target;

    public ViewObjectTypeDescriptor(object target)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public PropertyDescriptorCollection GetProperties()
    {
        return GetProperties(Array.Empty<Attribute>());
    }

    public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
    {
        var flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        var propertyDescriptors = _target.GetType()
            .GetProperties(flags)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Select(p => (PropertyDescriptor)new ReflectionPropertyDescriptor(_target, p));

        var fieldDescriptors = _target.GetType()
            .GetFields(flags)
            .Select(f => (PropertyDescriptor)new ReflectionFieldDescriptor(_target, f));

        return new PropertyDescriptorCollection(
            propertyDescriptors
                .Concat(fieldDescriptors)
                .ToArray()
        );
    }
    public static object WrapValue(object value)
    {
        if (value == null)
            return null;

        Type type = value.GetType();

        if (IsSimpleType(type))
            return value;

        if (value is System.Collections.IList list)
            return new ViewListTypeDescriptor(list);

        return new ViewObjectTypeDescriptor(value);
    }
    public static bool IsSimpleType(Type type)
    {
        if (type.IsPrimitive)
            return true;

        return type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(double)
            || type == typeof(float)
            || type == typeof(int)
            || type == typeof(long)
            || type == typeof(short)
            || type == typeof(byte)
            || type == typeof(bool)
            || type.IsEnum;
    }
    public TypeConverter GetConverter()
    {
        return new ViewObjectExpandableConverter();
    }
    public override string ToString()
    {
        return _target?.ToString() ?? "";
    }



    public object GetPropertyOwner(PropertyDescriptor pd) => _target;

    public AttributeCollection GetAttributes() => AttributeCollection.Empty;
    public string GetClassName() => _target.GetType().Name;
    public string GetComponentName() => null;
    public EventDescriptor GetDefaultEvent() => null;
    public PropertyDescriptor GetDefaultProperty() => null;
    public object GetEditor(Type editorBaseType) => null;
    public EventDescriptorCollection GetEvents() => EventDescriptorCollection.Empty;
    public EventDescriptorCollection GetEvents(Attribute[] attributes) => EventDescriptorCollection.Empty;
}


public sealed class ReflectionFieldDescriptor : PropertyDescriptor
{
    private readonly object _target;
    private readonly FieldInfo _field;

    public ReflectionFieldDescriptor(object target, FieldInfo field)
        : base(field.Name, null)
    {
        _target = target;
        _field = field;
    }
    public override TypeConverter Converter
    {
        get
        {
            if (ViewObjectTypeDescriptor.IsSimpleType(PropertyType))
                return TypeDescriptor.GetConverter(PropertyType);

            return new ViewObjectExpandableConverter();
        }
    }
    public override Type ComponentType => _target.GetType();
    public override Type PropertyType => _field.FieldType;
    public override bool IsReadOnly => _field.IsInitOnly || _field.IsLiteral;
    public override object GetValue(object component)
    {
        try
        {
            object value = _field.GetValue(_target);
            return ViewObjectTypeDescriptor.WrapValue(value);
        }
        catch (Exception ex)
        {
            return $"<error: {ex.Message}>";
        }
    }
    public override void SetValue(object component, object value)
    {
        if (!IsReadOnly)
            _field.SetValue(_target, value);
    }
    public override bool CanResetValue(object component) => false;
    public override void ResetValue(object component) { }
    public override bool ShouldSerializeValue(object component) => false;
}
public sealed class ReflectionPropertyDescriptor : PropertyDescriptor
{
    private readonly object _target;
    private readonly PropertyInfo _property;

    public ReflectionPropertyDescriptor(object target, PropertyInfo property)
        : base(property.Name, null)
    {
        _target = target;
        _property = property;
    }
    public override TypeConverter Converter
    {
        get
        {
            if (ViewObjectTypeDescriptor.IsSimpleType(PropertyType))
                return TypeDescriptor.GetConverter(PropertyType);

            return new ViewObjectExpandableConverter();
        }
    }
    public override Type ComponentType => _target.GetType();
    public override Type PropertyType => _property.PropertyType;
    public override bool IsReadOnly => _property.SetMethod == null;
    public override object GetValue(object component)
    {
        try
        {
            object value = _property.GetValue(_target, null);
            return ViewObjectTypeDescriptor.WrapValue(value);
        }
        catch (Exception ex)
        {
            return $"<error: {ex.Message}>";
        }
    }
    public override void SetValue(object component, object value)
    {
        if (IsReadOnly)
            return;

        _property.SetValue(_target, value, null);
    }
    public override bool CanResetValue(object component) => false;
    public override void ResetValue(object component)
    {
    }
    public override bool ShouldSerializeValue(object component) => false;
}

public sealed class ViewObjectExpandableConverter : ExpandableObjectConverter
{
    public override bool GetPropertiesSupported(ITypeDescriptorContext context)
    {
        return true;
    }

    public override PropertyDescriptorCollection GetProperties(
        ITypeDescriptorContext context,
        object value,
        Attribute[] attributes)
    {
        if (value is ViewObjectTypeDescriptor viewObject)
            return viewObject.GetProperties(attributes);

        return base.GetProperties(context, value, attributes);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object ConvertTo(
        ITypeDescriptorContext context,
        System.Globalization.CultureInfo culture,
        object value,
        Type destinationType)
    {
        if (destinationType == typeof(string))
            return value?.ToString() ?? "";

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
public sealed class ListItemPropertyDescriptor : PropertyDescriptor
{
    private readonly IList _list;
    private readonly int _index;

    public ListItemPropertyDescriptor(IList list, int index)
        : base($"[{index}]", null)
    {
        _list = list;
        _index = index;
    }

    public override Type ComponentType => _list.GetType();

    public override Type PropertyType
    {
        get
        {
            object value = _list[_index];
            return value?.GetType() ?? typeof(object);
        }
    }

    public override bool IsReadOnly => _list.IsReadOnly || _list.IsFixedSize;

    public override object GetValue(object component)
    {
        object value = _list[_index];
        return ViewObjectTypeDescriptor.WrapValue(value);
    }

    public override void SetValue(object component, object value)
    {
        if (!IsReadOnly)
            _list[_index] = value;
    }

    public override TypeConverter Converter
    {
        get
        {
            if (ViewObjectTypeDescriptor.IsSimpleType(PropertyType))
                return TypeDescriptor.GetConverter(PropertyType);

            return new ViewObjectExpandableConverter();
        }
    }

    public override bool CanResetValue(object component) => false;
    public override void ResetValue(object component) { }
    public override bool ShouldSerializeValue(object component) => false;
}
public sealed class ViewListTypeDescriptor : ICustomTypeDescriptor
{
    private readonly IList _list;

    public ViewListTypeDescriptor(IList list)
    {
        _list = list;
    }

    public PropertyDescriptorCollection GetProperties()
    {
        return GetProperties(Array.Empty<Attribute>());
    }

    public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
    {
        var props = Enumerable.Range(0, _list.Count)
            .Select(i => (PropertyDescriptor)new ListItemPropertyDescriptor(_list, i))
            .ToArray();

        return new PropertyDescriptorCollection(props);
    }

    public object GetPropertyOwner(PropertyDescriptor pd) => _list;

    public AttributeCollection GetAttributes() => AttributeCollection.Empty;
    public string GetClassName() => _list.GetType().Name;
    public string GetComponentName() => null;
    public TypeConverter GetConverter() => new ViewObjectExpandableConverter();
    public EventDescriptor GetDefaultEvent() => null;
    public PropertyDescriptor GetDefaultProperty() => null;
    public object GetEditor(Type editorBaseType) => null;
    public EventDescriptorCollection GetEvents() => EventDescriptorCollection.Empty;
    public EventDescriptorCollection GetEvents(Attribute[] attributes) => EventDescriptorCollection.Empty;

    public override string ToString()
    {
        return $"[{_list.Count}]";
    }
}