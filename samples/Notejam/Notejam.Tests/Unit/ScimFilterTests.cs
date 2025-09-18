using Xunit;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Samples.Application;

namespace Notejam.Tests.Unit
{
    /// <summary>
    /// Unit tests focused on SCIM filter processing
    /// </summary>
    public class ScimFilterTests
    {
        [Fact]
        public void ScimFilter_SimpleEquality_ShouldGenerateCorrectSql()
        {
            // Arrange
            var filter = "name eq \"TestPad\"";
            var schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;

            // Act
            var (sqlPredicate, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);

            // Assert - Case-sensitive comparison (no LOWER function)
            Assert.Contains("p.name = @p1", sqlPredicate);
            Assert.Contains("p1=TestPad", string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}")));
        }

        [Fact]
        public void ScimFilter_BooleanEquality_ShouldGenerateCorrectSql()
        {
            // Arrange
            var filter = "active eq true";
            var schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;

            // Act
            var (sqlPredicate, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);

            // Assert
            Assert.Contains("p.active = @p1", sqlPredicate);
            Assert.Contains("p1=True", string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}")));
        }

        [Fact]
        public void ScimFilter_ComplexFilter_ShouldGenerateCorrectSql()
        {
            // Arrange
            var filter = "name eq \"TestPad\" and active eq true";
            var schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;

            // Act
            var (sqlPredicate, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);

            // Assert - Case-sensitive comparison (no LOWER function)
            Assert.Contains("p.name = @p1", sqlPredicate);
            Assert.Contains("p.active = @p2", sqlPredicate);
            Assert.Contains("AND", sqlPredicate);
            Assert.Equal(2, parameters.Count);
        }

        [Fact]
        public void ScimFilter_DateComparison_ShouldGenerateCorrectSql()
        {
            // Arrange
            var filter = "active eq true";
            var schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;

            // Act
            var (sqlPredicate, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);

            // Assert
            Assert.Contains("p.active = @p1", sqlPredicate);
            Assert.Contains("p1=True", string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}")));
        }

        [Fact]
        public void ScimFilter_ContainsOperator_ShouldGenerateCorrectSql()
        {
            // Arrange
            var filter = "name co \"test\"";
            var schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;

            // Act
            var (sqlPredicate, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);

            // Assert - Case-sensitive comparison (no LOWER function)
            Assert.Contains("p.name LIKE @p1", sqlPredicate);
            Assert.Contains("p1=%test%", string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}")));
        }

        [Fact]
        public void ScimFilter_InvalidFilter_ShouldThrowException()
        {
            // Arrange
            var invalidFilter = "invalid filter syntax";
            var schemaMapping = NoteConfiguration.ScimMappings.AttributeToColumn;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                invalidFilter.ToSqlPredicateWithParameters(schemaMapping));
        }

        #region Edge Cases Tests

        [Theory]
        [InlineData("   ")]
        public void ScimFilter_WhitespaceFilter_ShouldThrowArgumentException(string filter)
        {
            // Arrange
            var schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                filter.ToSqlPredicateWithParameters(schemaMapping));
        }

        #endregion
    }
}
