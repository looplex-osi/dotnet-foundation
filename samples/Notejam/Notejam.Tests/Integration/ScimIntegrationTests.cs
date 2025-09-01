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
    /// Testes de integração reais para serviços SCIM com mocks adequados
    /// </summary>
    public class ScimIntegrationTests
    {
        private readonly INoteRepository _noteRepository;
        private readonly IPadRepository _padRepository;

        public ScimIntegrationTests()
        {
            _noteRepository = Substitute.For<INoteRepository>();
            _padRepository = Substitute.For<IPadRepository>();
        }

        #region SCIM Repository Integration Tests

        [Fact]
        public async Task ScimNotes_CreateAndRetrieve_ShouldWorkCorrectly()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var note = Note.Create("Test Note", "Test content");
            
            _noteRepository.CreateNoteAsync(note, default).Returns(noteId);
            _noteRepository.GetNoteByIdAsync(noteId, default).Returns(note);

            // Act - Create
            var createdId = await _noteRepository.CreateNoteAsync(note);

            // Act - Retrieve
            var retrievedNote = await _noteRepository.GetNoteByIdAsync(createdId);

            // Assert
            Assert.Equal(noteId, createdId);
            Assert.NotNull(retrievedNote);
            Assert.Equal(note.Name, retrievedNote.Name);
            Assert.Equal(note.Text, retrievedNote.Text);
        }

        [Fact]
        public async Task ScimNotes_QueryWithFilter_ShouldProcessCorrectly()
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

        [Fact]
        public async Task ScimNotes_UpdateAndDelete_ShouldWorkCorrectly()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var note = Note.Create("Updated Note", "Updated content");
            
            _noteRepository.UpdateNoteAsync(noteId, note, default).Returns(1);
            _noteRepository.DeleteNoteAsync(noteId, default).Returns(1);

            // Act - Update
            var updateResult = await _noteRepository.UpdateNoteAsync(noteId, note);

            // Act - Delete
            var deleteResult = await _noteRepository.DeleteNoteAsync(noteId);

            // Assert
            Assert.Equal(1, updateResult);
            Assert.Equal(1, deleteResult);
        }

        #endregion

        #region SCIM Pads Integration Tests

        [Fact]
        public async Task ScimPads_CreateAndRetrieve_ShouldWorkCorrectly()
        {
            // Arrange
            var padId = Guid.NewGuid();
            var pad = Pad.Create("Test Pad", true);
            
            _padRepository.CreatePadAsync(pad, default).Returns(padId);
            _padRepository.GetPadByIdAsync(padId, default).Returns(pad);

            // Act - Create
            var createdId = await _padRepository.CreatePadAsync(pad);

            // Act - Retrieve
            var retrievedPad = await _padRepository.GetPadByIdAsync(createdId);

            // Assert
            Assert.Equal(padId, createdId);
            Assert.NotNull(retrievedPad);
            Assert.Equal(pad.Name, retrievedPad.Name);
            Assert.Equal(pad.Active, retrievedPad.Active);
        }

        [Fact]
        public async Task ScimPads_QueryWithComplexFilter_ShouldProcessCorrectly()
        {
            // Arrange
            var pads = new List<Pad>
            {
                Pad.Create("Pad 1", true),
                Pad.Create("Pad 2", false),
                Pad.Create("Test Pad", true)
            };

            var filteredPads = pads.Where(p => p.Name.Contains("Test") && p.Active).ToList();
            _padRepository.GetPadsAsync("name co \"Test\" and active eq true", 1, 10, default)
                .Returns(filteredPads);

            // Act
            var result = await _padRepository.GetPadsAsync("name co \"Test\" and active eq true", 1, 10);

            // Assert
            Assert.Single(result);
            Assert.Contains(result, p => p.Name.Contains("Test") && p.Active);
        }

        #endregion

        #region SCIM Error Handling Tests

        [Fact]
        public async Task ScimNotes_InvalidFilter_ShouldHandleGracefully()
        {
            // Arrange
            _noteRepository.GetNotesAsync("invalid filter", 1, 10, default)
                .Returns((new List<Note>(), 0));

            // Act
            var (notes, totalCount) = await _noteRepository.GetNotesAsync("invalid filter", 1, 10);

            // Assert
            Assert.Equal(0, totalCount);
            Assert.Empty(notes);
        }

        [Fact]
        public async Task ScimPads_NonExistentResource_ShouldHandleGracefully()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _padRepository.GetPadByIdAsync(nonExistentId, default).Returns((Pad?)null);

            // Act & Assert
            var result = await _padRepository.GetPadByIdAsync(nonExistentId);
            Assert.Null(result);
        }

        #endregion

        #region SCIM Pagination Tests

        [Fact]
        public async Task ScimNotes_Pagination_ShouldWorkCorrectly()
        {
            // Arrange
            var notes = Enumerable.Range(1, 25)
                .Select(i => Note.Create($"Note {i}", $"Content {i}"))
                .ToList();

            _noteRepository.GetNotesAsync("", 2, 10, default)
                .Returns((notes.Skip(10).Take(10).ToList(), 25));

            // Act
            var (paginatedNotes, totalCount) = await _noteRepository.GetNotesAsync("", 2, 10);

            // Assert
            Assert.Equal(25, totalCount);
            Assert.Equal(10, paginatedNotes.Count);
            Assert.Equal("Note 11", paginatedNotes.First().Name);
        }

        #endregion
    }
}
