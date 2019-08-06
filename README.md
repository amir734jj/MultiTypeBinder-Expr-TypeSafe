# MultiTypeBinder-Expr

This library is similar to my [MultiTypeBinder](https://github.com/amir734jj/MultiTypeBinder) but used LINQ Expression tress to build getter and setters.

[![pipeline status](https://gitlab.com/hesamian/MultiTypeBinder-Expr/badges/master/pipeline.svg)](https://gitlab.com/hesamian/MultiTypeBinder-Expr/commits/master)


[NuGet](https://www.nuget.org/packages/MultiTypeBinder-Expr/)


```csharp
public enum Key
{
    Name
}

public class EntityA
{
    public string Name { get; set; }
}

public class EntityB
{
    public string Name { get; set; }
}

public class MultiTypeBinderTest
{    
    [Fact]
    public void Test_Basic_Set()
    {
        // Arrange
        var a = new EntityA {Name = "A"};
        var b = new EntityB {Name = "B"};
        
        var multiTypeItems = new MultiTypeBinderBuilder<Key>()
            .WithType<EntityA>(opt1 => opt1
                .WithProperty(x => x.Name1, Key.Name)
                .FinalizeType())
            .WithType<EntityB>(opt1 => opt1
                .WithProperty(x => x.Name2, Key.Name)
                .FinalizeType())
            .Build()
            .Map(new List<object> {a, b});

        // Act
        multiTypeItems.FirstOrDefault()[Key.Name] = "updated A";
        multiTypeItems.LastOrDefault()[Key.Name] = "updated B";
        
        var v1 = multiTypeItems.FirstOrDefault()[Key.Name];
        var v2 = multiTypeItems.LastOrDefault()[Key.Name];

        // Assert
        Assert.Equal(2, multiTypeItems.Count());
        Assert.Equal("updated A", v1);
        Assert.Equal("updated B", v2);
    }
}
 ```
