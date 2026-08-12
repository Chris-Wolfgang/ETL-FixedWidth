using System;
using System.Diagnostics.CodeAnalysis;
using Wolfgang.Etl.FixedWidth.Generated;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Direct unit tests for <see cref="GeneratedAccessorRegistry"/> — the runtime store the
/// source generator registers factory/getter/setter delegates into. These run on every
/// target framework (they do not depend on generated code) and use probe keys that never
/// collide with the generator's own registrations.
/// </summary>
public sealed class GeneratedAccessorRegistryTests
{
    [ExcludeFromCodeCoverage]
    private sealed class RegistryProbe
    {
        public string Value { get; set; } = string.Empty;
    }



    [Fact]
    public void RegisterFactory_then_TryGetFactory_returns_the_delegate()
    {
        Func<object> factory = () => new RegistryProbe { Value = "made" };
        GeneratedAccessorRegistry.RegisterFactory(typeof(RegistryProbe), factory);

        var found = GeneratedAccessorRegistry.TryGetFactory(typeof(RegistryProbe), out var resolved);

        Assert.True(found);
        Assert.Equal("made", Assert.IsType<RegistryProbe>(resolved()).Value);
    }



    [Fact]
    public void RegisterGetter_then_TryGetGetter_returns_the_delegate()
    {
        Func<object, object?> getter = instance => ((RegistryProbe)instance).Value;
        GeneratedAccessorRegistry.RegisterGetter(typeof(RegistryProbe), "GetProbe", getter);

        var found = GeneratedAccessorRegistry.TryGetGetter(typeof(RegistryProbe), "GetProbe", out var resolved);

        Assert.True(found);
        Assert.Equal("hi", resolved(new RegistryProbe { Value = "hi" }));
    }



    [Fact]
    public void RegisterSetter_then_TryGetSetter_returns_the_delegate()
    {
        Action<object, object?> setter = (instance, value) => ((RegistryProbe)instance).Value = (string)value!;
        GeneratedAccessorRegistry.RegisterSetter(typeof(RegistryProbe), "SetProbe", setter);

        var found = GeneratedAccessorRegistry.TryGetSetter(typeof(RegistryProbe), "SetProbe", out var resolved);
        var target = new RegistryProbe();
        resolved(target, "written");

        Assert.True(found);
        Assert.Equal("written", target.Value);
    }



    [Fact]
    public void TryGetFactory_when_unregistered_returns_false()
    {
        var found = GeneratedAccessorRegistry.TryGetFactory(typeof(Uri), out _);

        Assert.False(found);
    }



    [Fact]
    public void RegisterFactory_when_type_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>
        (
            () => GeneratedAccessorRegistry.RegisterFactory(null!, () => new RegistryProbe())
        );

        Assert.Equal("type", ex.ParamName);
    }



    [Fact]
    public void RegisterFactory_when_factory_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>
        (
            () => GeneratedAccessorRegistry.RegisterFactory(typeof(RegistryProbe), null!)
        );

        Assert.Equal("factory", ex.ParamName);
    }



    [Fact]
    public void RegisterGetter_when_declaringType_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>
        (
            () => GeneratedAccessorRegistry.RegisterGetter(null!, "P", _ => null)
        );

        Assert.Equal("declaringType", ex.ParamName);
    }



    [Fact]
    public void RegisterGetter_when_propertyName_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>
        (
            () => GeneratedAccessorRegistry.RegisterGetter(typeof(RegistryProbe), null!, _ => null)
        );

        Assert.Equal("propertyName", ex.ParamName);
    }



    [Fact]
    public void RegisterGetter_when_getter_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>
        (
            () => GeneratedAccessorRegistry.RegisterGetter(typeof(RegistryProbe), "P", null!)
        );

        Assert.Equal("getter", ex.ParamName);
    }



    [Fact]
    public void RegisterSetter_when_declaringType_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>
        (
            () => GeneratedAccessorRegistry.RegisterSetter(null!, "P", (_, _) => { })
        );

        Assert.Equal("declaringType", ex.ParamName);
    }



    [Fact]
    public void RegisterSetter_when_propertyName_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>
        (
            () => GeneratedAccessorRegistry.RegisterSetter(typeof(RegistryProbe), null!, (_, _) => { })
        );

        Assert.Equal("propertyName", ex.ParamName);
    }



    [Fact]
    public void RegisterSetter_when_setter_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>
        (
            () => GeneratedAccessorRegistry.RegisterSetter(typeof(RegistryProbe), "P", null!)
        );

        Assert.Equal("setter", ex.ParamName);
    }
}
