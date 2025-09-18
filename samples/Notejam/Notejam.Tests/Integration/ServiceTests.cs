using Xunit;
using Looplex.Samples.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Notejam.Tests.Integration
{
    /// <summary>
    /// Testes de integração concisos e robustos focados em validação de domínio e cenários críticos
    /// </summary>
    public class ServiceTests
    {
        #region Domain Validation Tests

        [Fact]
        public async Task DomainValidation_ValidEntities_ShouldCreateSuccessfully()
        {
            // Arrange & Act
            var note = Note.Create("Test Note", "Test content");
            var pad = Pad.Create("Test Pad", true);

            // Assert
            Assert.NotNull(note);
            Assert.Equal("Test Note", note.Name);
            Assert.Equal("Test content", note.Text);
            Assert.True(note.Active);

            Assert.NotNull(pad);
            Assert.Equal("Test Pad", pad.Name);
            Assert.True(pad.Active);
        }

        [Theory]
        [InlineData("   ", "Valid content")]
        [InlineData(null, "Valid content")]
        public async Task DomainValidation_InvalidNoteNames_ShouldThrowArgumentException(string name, string content)
        {
            // Act & Assert - Setters throw ArgumentException for null/whitespace
            Assert.Throws<ArgumentException>(() => Note.Create(name, content));
        }

        [Theory]
        [InlineData("", "Valid content")]
        public async Task DomainValidation_EmptyNoteNames_ShouldThrowInvalidOperationException(string name, string content)
        {
            // Act & Assert - Validate() throws InvalidOperationException for empty
            Assert.Throws<InvalidOperationException>(() => Note.Create(name, content));
        }

        [Theory]
        [InlineData("Valid name", null)]
        public async Task DomainValidation_NullNoteContent_ShouldThrowArgumentException(string name, string content)
        {
            // Act & Assert - Text setter throws ArgumentException for null
            Assert.Throws<ArgumentException>(() => Note.Create(name, content));
        }

        [Theory]
        [InlineData("Valid name", "")]
        [InlineData("Valid name", "   ")]
        public async Task DomainValidation_EmptyNoteContent_ShouldThrowInvalidOperationException(string name, string content)
        {
            // Act & Assert - Validate() throws InvalidOperationException for empty/whitespace
            Assert.Throws<InvalidOperationException>(() => Note.Create(name, content));
        }

        [Theory]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task DomainValidation_InvalidPadNames_ShouldThrowArgumentException(string name)
        {
            // Act & Assert - Setters throw ArgumentException for null/whitespace
            Assert.Throws<ArgumentException>(() => Pad.Create(name, true));
        }

        [Theory]
        [InlineData("")]
        public async Task DomainValidation_EmptyPadNames_ShouldThrowInvalidOperationException(string name)
        {
            // Act & Assert - Validate() throws InvalidOperationException for empty
            Assert.Throws<InvalidOperationException>(() => Pad.Create(name, true));
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task DomainValidation_LongNames_ShouldHandleCorrectly()
        {
            // Arrange
            var longName = new string('A', 255); // Maximum allowed length
            var longContent = new string('B', 1000); // Reasonable content length

            // Act & Assert
            var note = Note.Create(longName, longContent);
            var pad = Pad.Create(longName, true);

            Assert.NotNull(note);
            Assert.NotNull(pad);
            Assert.Equal(longName, note.Name);
            Assert.Equal(longContent, note.Text);
        }

        [Fact]
        public async Task DomainValidation_SpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var specialName = "Test Note with @#$%^&*()_+-=[]{}|;':\",./<>?";
            var specialContent = "Content with émojis 🎉 and special chars: áéíóú ñ ç";

            // Act & Assert
            var note = Note.Create(specialName, specialContent);
            var pad = Pad.Create(specialName, true);

            Assert.NotNull(note);
            Assert.NotNull(pad);
            Assert.Equal(specialName, note.Name);
            Assert.Equal(specialContent, note.Text);
        }

        [Fact]
        public async Task DomainValidation_ExceedMaxLength_ShouldThrowException()
        {
            // Arrange
            var tooLongName = new string('A', 256); // Exceeds maximum

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Note.Create(tooLongName, "Valid content"));
            Assert.Throws<ArgumentException>(() => Pad.Create(tooLongName, true));
        }

        #endregion
    }
}
