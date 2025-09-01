using Xunit;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application.Services;
using Looplex.Samples.Application.Commands;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Foundation.SCIMv2.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Notejam.Tests.Integration
{
    /// <summary>
    /// Testes de integração focados na interação Service + Repository
    /// </summary>
    public class ServiceTests
    {
        private readonly Notes _notesService;
        private readonly Pads _padsService;

        public ServiceTests()
        {
            // Para testes de integração, vamos usar mocks ou configurações de teste
            // Por enquanto, vamos pular estes testes até ter uma configuração adequada
            _notesService = null!;
            _padsService = null!;
        }

        #region Notes Service Tests

        [Fact(Skip = "Requires proper test configuration")]
        public async Task NotesService_CreateNote_ShouldWorkCorrectly()
        {
            // Arrange
            var note = Note.Create("Test Note", "Test content");
            var createCommand = new CreateNoteCommand(note);

            // Act
            var result = await _notesService.CreateAsync(createCommand);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(note.Name, result.Name);
            Assert.Equal(note.Text, result.Text);
            Assert.Equal(note.Active, result.Active);
            Assert.Equal(note.Status, result.Status);
        }

        [Fact(Skip = "Requires proper test configuration")]
        public async Task NotesService_GetNoteById_ShouldReturnCorrectNote()
        {
            // Arrange
            var createCommand = new CreateNoteCommand
            {
                Name = "Test Note for Get",
                Text = "Test content for get",
                Active = true
            };
            var createdNote = await _notesService.CreateAsync(createCommand);

            // Act
            var retrievedNote = await _notesService.GetByIdAsync(createdNote.Id);

            // Assert
            Assert.NotNull(retrievedNote);
            Assert.Equal(createdNote.Id, retrievedNote.Id);
            Assert.Equal(createdNote.Name, retrievedNote.Name);
            Assert.Equal(createdNote.Text, retrievedNote.Text);
        }

        [Fact(Skip = "Requires proper test configuration")]
        public async Task NotesService_UpdateNote_ShouldModifyExistingNote()
        {
            // Arrange
            var createCommand = new CreateNoteCommand
            {
                Name = "Original Note",
                Text = "Original content",
                Active = true
            };
            var createdNote = await _notesService.CreateAsync(createCommand);

            var updateCommand = new UpdateNoteCommand
            {
                Id = createdNote.Id,
                Name = "Updated Note",
                Text = "Updated content",
                Active = false
            };

            // Act
            var updatedNote = await _notesService.UpdateAsync(updateCommand);

            // Assert
            Assert.NotNull(updatedNote);
            Assert.Equal("Updated Note", updatedNote.Name);
            Assert.Equal("Updated content", updatedNote.Text);
            Assert.False(updatedNote.Active);
        }

        [Fact(Skip = "Requires proper test configuration")]
        public async Task NotesService_DeleteNote_ShouldRemoveNote()
        {
            // Arrange
            var note = Note.Create("Note to Delete", "Content to delete");
            var createCommand = new CreateNoteCommand(note);
            var createdNote = await _notesService.CreateAsync(createCommand);

            // Act
            await _notesService.DeleteAsync(createdNote.Id);
            var deletedNote = await _notesService.GetByIdAsync(createdNote.Id);

            // Assert
            Assert.Null(deletedNote);
        }

        [Fact]
        public async Task NotesService_QueryWithSCIMFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var note1 = await _notesService.CreateAsync(new CreateNoteCommand
            {
                Name = "Test Note 1",
                Text = "Content 1",
                Active = true
            });

            var note2 = await _notesService.CreateAsync(new CreateNoteCommand
            {
                Name = "Another Note",
                Text = "Content 2",
                Active = true
            });

            var note3 = await _notesService.CreateAsync(new CreateNoteCommand
            {
                Name = "Test Note 2",
                Text = "Content 3",
                Active = false
            });

            // Act
            var query = new QueryResource<Note>(1, 10, "name co \"Test\" and active eq true", "name", "asc");
            var result = await _notesService.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Items.Count() >= 1);
            Assert.All(result.Items, item => 
            {
                Assert.Contains("Test", item.Name);
                Assert.True(item.Active);
            });
        }

        [Fact]
        public async Task NotesService_QueryWithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            for (int i = 1; i <= 12; i++)
            {
                await _notesService.CreateAsync(new CreateNoteCommand
                {
                    Name = $"Note {i:D2}",
                    Text = $"Content {i}",
                    Active = true
                });
            }

            // Act
            var query = new QueryResource<Note>(2, 5, "", "name", "asc");
            var result = await _notesService.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Items.Count());
            Assert.Equal(2, result.Page);
            Assert.Equal(5, result.PageSize);
        }

        #endregion

        #region Pads Service Tests

        [Fact]
        public async Task PadsService_CreatePad_ShouldWorkCorrectly()
        {
            // Arrange
            var createCommand = new CreatePadCommand
            {
                Name = "Test Pad",
                Active = true,
                Status = 1
            };

            // Act
            var result = await _padsService.CreateAsync(createCommand);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createCommand.Name, result.Name);
            Assert.Equal(createCommand.Active, result.Active);
            Assert.Equal(createCommand.Status, result.Status);
        }

        [Fact]
        public async Task PadsService_GetPadById_ShouldReturnCorrectPad()
        {
            // Arrange
            var createCommand = new CreatePadCommand
            {
                Name = "Test Pad for Get",
                Active = true
            };
            var createdPad = await _padsService.CreateAsync(createCommand);

            // Act
            var retrievedPad = await _padsService.GetByIdAsync(createdPad.Id);

            // Assert
            Assert.NotNull(retrievedPad);
            Assert.Equal(createdPad.Id, retrievedPad.Id);
            Assert.Equal(createdPad.Name, retrievedPad.Name);
            Assert.Equal(createdPad.Active, retrievedPad.Active);
        }

        [Fact]
        public async Task PadsService_UpdatePad_ShouldModifyExistingPad()
        {
            // Arrange
            var createCommand = new CreatePadCommand
            {
                Name = "Original Pad",
                Active = true
            };
            var createdPad = await _padsService.CreateAsync(createCommand);

            var updateCommand = new UpdatePadCommand
            {
                Id = createdPad.Id,
                Name = "Updated Pad",
                Active = false
            };

            // Act
            var updatedPad = await _padsService.UpdateAsync(updateCommand);

            // Assert
            Assert.NotNull(updatedPad);
            Assert.Equal("Updated Pad", updatedPad.Name);
            Assert.False(updatedPad.Active);
        }

        [Fact]
        public async Task PadsService_QueryWithActiveFilter_ShouldReturnOnlyActivePads()
        {
            // Arrange
            await _padsService.CreateAsync(new CreatePadCommand
            {
                Name = "Active Pad 1",
                Active = true
            });

            await _padsService.CreateAsync(new CreatePadCommand
            {
                Name = "Inactive Pad",
                Active = false
            });

            await _padsService.CreateAsync(new CreatePadCommand
            {
                Name = "Active Pad 2",
                Active = true
            });

            // Act
            var query = new QueryResource<Pad>(1, 10, "active eq true", "name", "asc");
            var result = await _padsService.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Items.Count() >= 2);
            Assert.All(result.Items, item => Assert.True(item.Active));
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Service_GetNonExistentId_ShouldReturnNull()
        {
            // Act
            var note = await _notesService.GetByIdAsync(Guid.NewGuid());
            var pad = await _padsService.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(note);
            Assert.Null(pad);
        }

        [Fact]
        public async Task Service_UpdateNonExistentEntity_ShouldHandleGracefully()
        {
            // Arrange
            var updateCommand = new UpdateNoteCommand
            {
                Id = Guid.NewGuid(),
                Name = "Non Existent",
                Text = "Content"
            };

            // Act & Assert
            var updatedNote = await _notesService.UpdateAsync(updateCommand);
            Assert.Null(updatedNote);
        }

        [Fact]
        public async Task Service_DeleteNonExistentEntity_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw exception
            await _notesService.DeleteAsync(Guid.NewGuid());
            await _padsService.DeleteAsync(Guid.NewGuid());
        }

        #endregion

        #region SCIM Compliance Tests

        [Fact]
        public async Task Service_SCIMFilterCompliance_ShouldHandleComplexFilters()
        {
            // Arrange
            await _notesService.CreateAsync(new CreateNoteCommand
            {
                Name = "Test Note A",
                Text = "Content A",
                Active = true,
                Status = 1
            });

            await _notesService.CreateAsync(new CreateNoteCommand
            {
                Name = "Test Note B",
                Text = "Content B",
                Active = true,
                Status = 2
            });

            // Act - Test complex SCIM filter
            var query = new QueryResource<Note>(1, 10, 
                "(name co \"Test\" and active eq true) or (status eq 2)", 
                "name", "asc");
            var result = await _notesService.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Items.Count() >= 2);
        }

        [Fact]
        public async Task Service_SCIMSorting_ShouldOrderResultsCorrectly()
        {
            // Arrange
            await _notesService.CreateAsync(new CreateNoteCommand
            {
                Name = "Zebra Note",
                Text = "Content",
                Active = true
            });

            await _notesService.CreateAsync(new CreateNoteCommand
            {
                Name = "Alpha Note",
                Text = "Content",
                Active = true
            });

            // Act
            var query = new QueryResource<Note>(1, 10, "", "name", "asc");
            var result = await _notesService.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            var items = result.Items.ToList();
            if (items.Count >= 2)
            {
                Assert.True(string.Compare(items[0].Name, items[1].Name) <= 0);
            }
        }

        #endregion
    }
}
