using Xunit;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Infra.Repositories;
using Looplex.Samples.Application;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Foundation.Ports;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Notejam.Tests.Integration
{
    /// <summary>
    /// Testes de integração focados na interação Repository + Domain
    /// </summary>
    public class RepositoryTests
    {
        private readonly INoteRepository _noteRepository;
        private readonly IPadRepository _padRepository;

        public RepositoryTests()
        {
            // Para testes de integração, vamos usar mocks ou configurações de teste
            // Por enquanto, vamos pular estes testes até ter uma configuração adequada
            _noteRepository = null!;
            _padRepository = null!;
        }

        #region Note Repository Tests

        [Fact(Skip = "Requires proper test configuration")]
        public async Task NoteRepository_CreateAndRetrieve_ShouldWorkCorrectly()
        {
            // Arrange
            var note = Note.Create("Test Note", "Test content");

            // Act
            var createdNote = await _noteRepository.CreateAsync(note);
            var retrievedNote = await _noteRepository.GetByIdAsync(createdNote.Id);

            // Assert
            Assert.NotNull(createdNote);
            Assert.NotNull(retrievedNote);
            Assert.Equal(note.Name, createdNote.Name);
            Assert.Equal(note.Text, createdNote.Text);
            Assert.Equal(createdNote.Id, retrievedNote.Id);
        }

        [Fact]
        public async Task NoteRepository_Update_ShouldModifyExistingNote()
        {
            // Arrange
            var note = Note.Create("Original Name", "Original content");
            var createdNote = await _noteRepository.CreateAsync(note);
            
            // Act
            createdNote.Name = "Updated Name";
            createdNote.Text = "Updated content";
            var updatedNote = await _noteRepository.UpdateAsync(createdNote);

            // Assert
            Assert.Equal("Updated Name", updatedNote.Name);
            Assert.Equal("Updated content", updatedNote.Text);
        }

        [Fact]
        public async Task NoteRepository_Delete_ShouldRemoveNote()
        {
            // Arrange
            var note = Note.Create("To Delete", "Content to delete");
            var createdNote = await _noteRepository.CreateAsync(note);

            // Act
            await _noteRepository.DeleteAsync(createdNote.Id);
            var deletedNote = await _noteRepository.GetByIdAsync(createdNote.Id);

            // Assert
            Assert.Null(deletedNote);
        }

        [Fact]
        public async Task NoteRepository_QueryWithFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var note1 = Note.Create("Test Note 1", "Content 1");
            var note2 = Note.Create("Another Note", "Content 2");
            var note3 = Note.Create("Test Note 2", "Content 3");

            await _noteRepository.CreateAsync(note1);
            await _noteRepository.CreateAsync(note2);
            await _noteRepository.CreateAsync(note3);

            // Act
            var query = new QueryResource<Note>(1, 10, "name co \"Test\"", "name", "asc");
            var result = await _noteRepository.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Items.Count() >= 2);
            Assert.All(result.Items, item => Assert.Contains("Test", item.Name));
        }

        [Fact]
        public async Task NoteRepository_Pagination_ShouldReturnCorrectPage()
        {
            // Arrange
            for (int i = 1; i <= 15; i++)
            {
                var note = Note.Create($"Note {i}", $"Content {i}");
                await _noteRepository.CreateAsync(note);
            }

            // Act
            var query = new QueryResource<Note>(2, 5, "", "name", "asc");
            var result = await _noteRepository.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Items.Count());
            Assert.Equal(2, result.Page);
            Assert.Equal(5, result.PageSize);
        }

        #endregion

        #region Pad Repository Tests

        [Fact]
        public async Task PadRepository_CreateAndRetrieve_ShouldWorkCorrectly()
        {
            // Arrange
            var pad = Pad.Create("Test Pad", true);

            // Act
            var createdPad = await _padRepository.CreateAsync(pad);
            var retrievedPad = await _padRepository.GetByIdAsync(createdPad.Id);

            // Assert
            Assert.NotNull(createdPad);
            Assert.NotNull(retrievedPad);
            Assert.Equal(pad.Name, createdPad.Name);
            Assert.Equal(pad.Active, createdPad.Active);
            Assert.Equal(createdPad.Id, retrievedPad.Id);
        }

        [Fact]
        public async Task PadRepository_Update_ShouldModifyExistingPad()
        {
            // Arrange
            var pad = Pad.Create("Original Pad", true);
            var createdPad = await _padRepository.CreateAsync(pad);
            
            // Act
            createdPad.Name = "Updated Pad";
            createdPad.Active = false;
            var updatedPad = await _padRepository.UpdateAsync(createdPad);

            // Assert
            Assert.Equal("Updated Pad", updatedPad.Name);
            Assert.False(updatedPad.Active);
        }

        [Fact]
        public async Task PadRepository_QueryWithActiveFilter_ShouldReturnOnlyActivePads()
        {
            // Arrange
            var pad1 = Pad.Create("Active Pad 1", true);
            var pad2 = Pad.Create("Inactive Pad", false);
            var pad3 = Pad.Create("Active Pad 2", true);

            await _padRepository.CreateAsync(pad1);
            await _padRepository.CreateAsync(pad2);
            await _padRepository.CreateAsync(pad3);

            // Act
            var query = new QueryResource<Pad>(1, 10, "active eq true", "name", "asc");
            var result = await _padRepository.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Items.Count() >= 2);
            Assert.All(result.Items, item => Assert.True(item.Active));
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Repository_GetNonExistentId_ShouldReturnNull()
        {
            // Act
            var note = await _noteRepository.GetByIdAsync(Guid.NewGuid());
            var pad = await _padRepository.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(note);
            Assert.Null(pad);
        }

        [Fact]
        public async Task Repository_UpdateNonExistentEntity_ShouldHandleGracefully()
        {
            // Arrange
            var nonExistentNote = Note.Create("Non Existent", "Content");
            nonExistentNote.Id = Guid.NewGuid();

            // Act & Assert
            var updatedNote = await _noteRepository.UpdateAsync(nonExistentNote);
            Assert.Null(updatedNote);
        }

        [Fact]
        public async Task Repository_DeleteNonExistentEntity_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw exception
            await _noteRepository.DeleteAsync(Guid.NewGuid());
            await _padRepository.DeleteAsync(Guid.NewGuid());
        }

        #endregion
    }
}
