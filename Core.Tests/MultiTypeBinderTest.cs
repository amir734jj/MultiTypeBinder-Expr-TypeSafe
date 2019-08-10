using System;
using System.Linq;
using MultiTypeBinderExprTypeSafe.Interfaces;
using Xunit;

namespace MultiTypeBinderExprTypeSafe.Tests
{
    public interface ICommon
    {
        string Name { get; set; }

        string Missing { get; set; }
    }

    public class EntityA
    {
        public string Name1 { get; set; }
    }

    public class EntityB
    {
        public string Name2 { get; set; }
    }

    public class MultiTypeBinderTest
    {
        private readonly IMultiTypeBinder<ICommon> _utility;

        public MultiTypeBinderTest()
        {
            _utility = new MultiTypeBinderBuilder<ICommon>()
                .WithType<EntityA>(x =>
                    x.WithProperty(y => y.Name, y => y.Name1)
                        .FinalizeType())
                .WithType<EntityB>(x =>
                    x.WithProperty(y => y.Name, y => y.Name2)
                        .FinalizeType())
                .Build();
        }

        [Fact]
        public void Test__Getter()
        {
            // Arrange
            var entityA = new EntityA {Name1 = "Test1"};
            var entityB = new EntityB {Name2 = "Test2"};

            // Act
            var commons = _utility.Map(new object[] {entityA, entityB});

            // Assert
            Assert.Equal(commons.First().Name, entityA.Name1);
            Assert.Equal(commons.Last().Name, entityB.Name2);
        }

        [Fact]
        public void Test__Setter()
        {
            // Arrange
            var entityA = new EntityA {Name1 = "Test1"};
            var entityB = new EntityB {Name2 = "Test2"};

            // Act
            var commons = _utility.Map(new object[] {entityA, entityB});
            commons.First().Name = "Updated Test1";
            commons.Last().Name = "Updated Test2";

            // Assert
            Assert.Equal(commons.First().Name, entityA.Name1);
            Assert.Equal(commons.Last().Name, entityB.Name2);
        }

        [Fact]
        public void Test__Getter_MissingProp()
        {
            // Arrange
            var entityA = new EntityA {Name1 = "Test1"};
            var entityB = new EntityB {Name2 = "Test2"};

            // Act
            var commons = _utility.Map(new object[] {entityA, entityB});

            // Assert
            Assert.Throws<NotImplementedException>(() => commons.First().Missing);
            Assert.Throws<NotImplementedException>(() => commons.First().Missing);
        }

        [Fact]
        public void Test__Setter_MissingProp()
        {
            // Arrange
            var entityA = new EntityA {Name1 = "Test1"};
            var entityB = new EntityB {Name2 = "Test2"};

            // Act
            var commons = _utility.Map(new object[] {entityA, entityB});

            // Assert
            Assert.Throws<NotImplementedException>(() => commons.First().Missing = "Updated Test1");
            Assert.Throws<NotImplementedException>(() => commons.First().Missing = "Updated Test2");
        }

        [Fact]
        public void Test__Missing_Mapper()
        {
            // Arrange
            var entityA = new EntityA {Name1 = "Test1"};
            var entityB = new EntityB {Name2 = "Test2"};
            var random = new string("Random Str!");

            // Act, Assert
            Assert.Throws<Exception>(() => _utility.Map(new object[] {entityA, entityB, random}));
        }
    }
}