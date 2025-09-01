using Xunit;
using Looplex.Samples.Domain.Entities;

namespace Notejam.Tests.Unit
{
    /// <summary>
    /// Unit tests focused on domain logic
    /// </summary>
    public class DomainTests
    {
        #region Note Entity Tests

        [Fact]
        public void Note_Create_WithValidData_ShouldSucceed()
        {
            // Arrange & Act
            var note = Note.Create("Test Note", "Test content");

            // Assert
            Assert.Equal("Test Note", note.Name);
            Assert.Equal("Test content", note.Text);
            Assert.True(note.Active);
            Assert.Equal(1, note.Status);
            Assert.NotNull(note);
        }

        [Theory]
        [InlineData("   ", "content")]
        [InlineData(null, "content")]
        public void Note_Create_WithInvalidName_ShouldThrowArgumentException(string name, string content)
        {
            // Act & Assert - Setters throw ArgumentException for null/whitespace
            Assert.Throws<ArgumentException>(() => Note.Create(name, content));
        }

        [Theory]
        [InlineData("", "content")]
        public void Note_Create_WithEmptyName_ShouldThrowInvalidOperationException(string name, string content)
        {
            // Act & Assert - Create() calls Validate() which throws InvalidOperationException for empty
            Assert.Throws<InvalidOperationException>(() => Note.Create(name, content));
        }

        [Theory]
        [InlineData("name", null)]
        public void Note_Create_WithNullContent_ShouldThrowArgumentException(string name, string content)
        {
            // Act & Assert - Text setter throws ArgumentException for null
            Assert.Throws<ArgumentException>(() => Note.Create(name, content));
        }

        [Theory]
        [InlineData("name", "")]
        [InlineData("name", "   ")]
        public void Note_Create_WithEmptyContent_ShouldThrowInvalidOperationException(string name, string content)
        {
            // Act & Assert - Validate() throws InvalidOperationException for empty/whitespace
            Assert.Throws<InvalidOperationException>(() => Note.Create(name, content));
        }

        [Fact]
        public void Note_Status_WithValidValues_ShouldSucceed()
        {
            // Arrange
            var note = Note.Create("Test", "Content");

            // Act & Assert
            note.Status = 0;
            Assert.Equal(0, note.Status);

            note.Status = 255;
            Assert.Equal(255, note.Status);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(256)]
        public void Note_Status_WithInvalidValues_ShouldThrowException(int invalidStatus)
        {
            // Arrange
            var note = Note.Create("Test", "Content");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => note.Status = invalidStatus);
        }

        #endregion

        #region Pad Entity Tests

        [Fact]
        public void Pad_Create_WithValidData_ShouldSucceed()
        {
            // Arrange & Act
            var pad = Pad.Create("Test Pad", true);

            // Assert
            Assert.Equal("Test Pad", pad.Name);
            Assert.True(pad.Active);
            Assert.Equal(1, pad.Status);
            Assert.NotNull(pad);
        }

        [Theory]
        [InlineData("   ")]
        [InlineData(null)]
        public void Pad_Create_WithInvalidName_ShouldThrowArgumentException(string name)
        {
            // Act & Assert - Setters throw ArgumentException for null/whitespace
            Assert.Throws<ArgumentException>(() => Pad.Create(name, true));
        }

        [Theory]
        [InlineData("")]
        public void Pad_Create_WithEmptyName_ShouldThrowInvalidOperationException(string name)
        {
            // Act & Assert - Create() calls Validate() which throws InvalidOperationException for empty
            Assert.Throws<InvalidOperationException>(() => Pad.Create(name, true));
        }

        #endregion
    }
}
