# MultiTypeBinder-Expr-TypeSafe

This library is similar to my [object-binder](https://github.com/amir734jj/object-binder) but uses `System.Reflection.Emit` to emit common proxy type.

[![pipeline status](https://gitlab.com/hesamian/MultiTypeBinder-Expr-TypeSafe/badges/master/pipeline.svg)](https://gitlab.com/hesamian/MultiTypeBinder-Expr-TypeSafe/commits/master)


[NuGet](https://www.nuget.org/packages/multi-type-binder-expr-typesafe/)


```csharp
public interface ICommon
{
    string Name { get; set; }
}

public class EntityA
{
    public string Name { get; set; }
}

public class EntityB
{
    public string Name { get; set; }
}

public class MultiTypeBinderExprTypeSafeTest
{    
    [Fact]
    public void Test_Basic_Set()
    {
        // Arrange
        var a = new EntityA {Name1 = "A"};
        var b = new EntityB {Name2 = "B"};
        
        var multiTypeItems = new MultiTypeBinderBuilder<ICommon>()
            .WithType<EntityA> (x =>
                x.WithProperty(y => y.Name, y => y.Name1)
                .FinalizeType())
            .WithType<EntityB> (x =>
                x.WithProperty(y => y.Name, y => y.Name2)
                .FinalizeType())
            .Build();

        // Act
        var commons = _utility.Map(new object[] { entityA, entityB });
        
        // Assert
        Assert.Equal(commons.First().Name, entityA.Name1);
        Assert.Equal(commons.Last().Name, entityB.Name2);
    }
}
 ```
