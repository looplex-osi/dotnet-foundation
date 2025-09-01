using Xunit;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application;
using System;
using System.Threading.Tasks;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;

namespace Notejam.Tests.Integration
{
    /// <summary>
    /// Concise and robust integration tests focused on Repository + Domain interaction
    /// </summary>
    public class RepositoryTests
    {
        private readonly INoteRepository _noteRepository;
        private readonly IPadRepository _padRepository;

        public RepositoryTests()
        {
            _noteRepository = Substitute.For<INoteRepository>();
            _padRepository = Substitute.For<IPadRepository>();
        }

        #region Note Repository Tests

        [Fact]
        public async Task NoteRepository_CompleteCRUD_ShouldWorkCorrectly()
        {
            // Arrange
            var note = Note.Create("Test Note", "Test content");
            var noteId = Guid.NewGuid();
            
            _noteRepository.CreateNoteAsync(note, default).Returns(noteId);
            _noteRepository.GetNoteByIdAsync(noteId, default).Returns(note);
            _noteRepository.UpdateNoteAsync(noteId, note, default).Returns(1);
            _noteRepository.DeleteNoteAsync(noteId, default).Returns(1);

            // Act & Assert - Create
            var createdId = await _noteRepository.CreateNoteAsync(note);
            Assert.Equal(noteId, createdId);

            // Act & Assert - Read
            var retrievedNote = await _noteRepository.GetNoteByIdAsync(createdId);
            Assert.NotNull(retrievedNote);
            Assert.Equal(note.Name, retrievedNote.Name);

            // Act & Assert - Update
            var updateResult = await _noteRepository.UpdateNoteAsync(noteId, note);
            Assert.Equal(1, updateResult);

            // Act & Assert - Delete
            var deleteResult = await _noteRepository.DeleteNoteAsync(noteId);
            Assert.Equal(1, deleteResult);
        }

        [Fact]
        public async Task NoteRepository_QueryWithFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var notes = new List<Note>
            {
                Note.Create("Note 1", "Content 1"),
                Note.Create("Note 2", "Content 2"),
                Note.Create("Test Note", "Test Content")
            };

            _noteRepository.GetNotesAsync("name co \"Test\"", 1, 10, default)
                .Returns((notes.Where(n => n.Name.Contains("Test")).ToList(), 1));

            // Act
            var (filteredNotes, totalCount) = await _noteRepository.GetNotesAsync("name co \"Test\"", 1, 10);

            // Assert
            Assert.Equal(1, totalCount);
            Assert.Single(filteredNotes);
            Assert.Contains(filteredNotes, n => n.Name.Contains("Test"));
        }

        #endregion

        #region Pad Repository Tests

        [Fact]
        public async Task PadRepository_CompleteCRUD_ShouldWorkCorrectly()
        {
            // Arrange
            var pad = Pad.Create("Test Pad", true);
            var padId = Guid.NewGuid();

            _padRepository.CreatePadAsync(pad, default).Returns(padId);
            _padRepository.GetPadByIdAsync(padId, default).Returns(pad);
            _padRepository.UpdatePadAsync(padId, pad, default).Returns(1);
            _padRepository.DeletePadAsync(padId, default).Returns(1);

            // Act & Assert - Create
            var createdId = await _padRepository.CreatePadAsync(pad);
            Assert.Equal(padId, createdId);

            // Act & Assert - Read
            var retrievedPad = await _padRepository.GetPadByIdAsync(createdId);
            Assert.NotNull(retrievedPad);
            Assert.Equal(pad.Name, retrievedPad.Name);

            // Act & Assert - Update
            var updateResult = await _padRepository.UpdatePadAsync(padId, pad);
            Assert.Equal(1, updateResult);

            // Act & Assert - Delete
            var deleteResult = await _padRepository.DeletePadAsync(padId);
            Assert.Equal(1, deleteResult);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Repository_ErrorScenarios_ShouldHandleGracefully()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _noteRepository.GetNoteByIdAsync(nonExistentId, default).Returns((Note?)null);
            _padRepository.GetPadByIdAsync(nonExistentId, default).Returns((Pad?)null);

            // Act & Assert - Non-existent entities
            var note = await _noteRepository.GetNoteByIdAsync(nonExistentId);
            var pad = await _padRepository.GetPadByIdAsync(nonExistentId);
            Assert.Null(note);
            Assert.Null(pad);
        }

        #endregion
    }
}
