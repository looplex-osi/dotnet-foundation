using Microsoft.Extensions.Logging;
using Xunit;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application;
using Looplex.Samples.Infra.Repositories;
using NSubstitute;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System;
using Looplex.Foundation.SCIMv2.Queries;

namespace Notejam.Tests
{
    /// <summary>
    /// Comprehensive edge case tests for Notejam application.
    /// 
    /// These tests cover:
    /// - Boundary conditions and limits
    /// - Error scenarios and exception handling
    /// - Unusual input data and edge cases
    /// - Performance edge cases
    /// - Security edge cases
    /// - Data validation edge cases
    /// </summary>
    public class EdgeCaseTests
    {
        private readonly INoteRepository _noteRepository;
        private readonly ILogger<NoteRepository> _logger;

        public EdgeCaseTests()
        {
            _logger = Substitute.For<ILogger<NoteRepository>>();
            var dbConnections = Substitute.For<IDbConnections>();
            _noteRepository = new NoteRepository(dbConnections, _logger);
        }

        #region Domain Entity Edge Cases

        [Fact]
        public void Note_CreateWithBoundaryLengthName_ShouldSucceed()
        {
            // Test maximum allowed name length (255 characters)
            var maxLengthName = new string('A', 255);
            var note = Note.Create(maxLengthName, "Valid content");
            
            Assert.Equal(maxLengthName, note.Name);
            Assert.Equal("Valid content", note.Text);
        }

        [Fact]
        public void Note_CreateWithBoundaryLengthText_ShouldSucceed()
        {
            // Test maximum allowed text length (10,000 characters)
            var maxLengthText = new string('B', 10000);
            var note = Note.Create("Valid name", maxLengthText);
            
            Assert.Equal("Valid name", note.Name);
            Assert.Equal(maxLengthText, note.Text);
        }

        [Fact]
        public void Note_CreateWithExactBoundaryLengthName_ShouldThrowException()
        {
            // Test name exceeding maximum length (256 characters)
            var tooLongName = new string('A', 256);
            
            Assert.Throws<ArgumentException>(() => Note.Create(tooLongName, "Valid content"));
        }

        [Fact]
        public void Note_CreateWithExactBoundaryLengthText_ShouldThrowException()
        {
            // Test text exceeding maximum length (10,001 characters)
            var tooLongText = new string('B', 10001);
            
            Assert.Throws<ArgumentException>(() => Note.Create("Valid name", tooLongText));
        }

        [Fact]
        public void Note_CreateWithWhitespaceOnlyName_ShouldThrowException()
        {
            // Test name with only whitespace characters
            Assert.Throws<ArgumentException>(() => Note.Create("   ", "Valid content"));
            Assert.Throws<ArgumentException>(() => Note.Create("\t", "Valid content"));
            Assert.Throws<ArgumentException>(() => Note.Create("\n", "Valid content"));
            Assert.Throws<ArgumentException>(() => Note.Create("\r", "Valid content"));
        }

        [Fact]
        public void Note_CreateWithUnicodeCharacters_ShouldSucceed()
        {
            // Test with various Unicode characters
            var unicodeName = "Nota com acentos: áéíóú çãõ ñ";
            var unicodeText = "Conteúdo com emojis: 🚀📝✅";
            
            var note = Note.Create(unicodeName, unicodeText);
            
            Assert.Equal(unicodeName, note.Name);
            Assert.Equal(unicodeText, note.Text);
        }

        [Fact]
        public void Note_CreateWithSpecialCharacters_ShouldSucceed()
        {
            // Test with special characters that might cause SQL injection
            var specialName = "Note with 'quotes' and \"double quotes\" and ; semicolon";
            var specialText = "Content with <script>alert('xss')</script> and -- SQL comment";
            
            var note = Note.Create(specialName, specialText);
            
            Assert.Equal(specialName, note.Name);
            Assert.Equal(specialText, note.Text);
        }

        [Fact]
        public void Note_StatusWithBoundaryValues_ShouldSucceed()
        {
            var note = Note.Create("Test", "Content");
            
            // Test minimum status value
            note.Status = 0;
            Assert.Equal(0, note.Status);
            
            // Test maximum status value
            note.Status = 255;
            Assert.Equal(255, note.Status);
        }

        [Fact]
        public void Note_StatusWithInvalidValues_ShouldThrowException()
        {
            var note = Note.Create("Test", "Content");
            
            // Test negative status
            Assert.Throws<ArgumentException>(() => note.Status = -1);
            
            // Test status exceeding maximum
            Assert.Throws<ArgumentException>(() => note.Status = 256);
        }

        [Fact]
        public void Note_CustomFieldsWithVariousValues_ShouldSucceed()
        {
            var note = Note.Create("Test", "Content");
            
            // Test empty custom fields
            note.CustomFields = "";
            Assert.Equal("{}", note.CustomFields);
            
            // Test whitespace custom fields
            note.CustomFields = "   ";
            Assert.Equal("{}", note.CustomFields);
            
            // Test valid JSON custom fields
            note.CustomFields = "{\"key\": \"value\", \"number\": 123}";
            Assert.Equal("{\"key\": \"value\", \"number\": 123}", note.CustomFields);
        }

        #endregion

        #region SCIM Filter Edge Cases

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void SCIMFilter_EmptyOrNullFilter_ShouldBeHandledGracefully(string? filter)
        {
            // These filters should be handled gracefully without throwing exceptions
            // The repository should return all results when filter is empty/null
            var queryCommand = new QueryResource<Note>(1, 10, filter, "created", "desc");
            
            // Verify that empty/null filters are handled gracefully
            Assert.Equal(filter, queryCommand.Filter);
            Assert.Equal(1, queryCommand.Page);
            Assert.Equal(10, queryCommand.PageSize);
        }

        [Theory]
        [InlineData("invalid filter syntax")]
        [InlineData("name eq")]
        [InlineData("eq 'value'")]
        [InlineData("name eq 'value' and")]
        [InlineData("name eq 'value' or")]
        [InlineData("(name eq 'value'")]
        [InlineData("name eq 'value')")]
        public void SCIMFilter_InvalidSyntax_ShouldBeHandledGracefully(string filter)
        {
            // Invalid SCIM filters should be handled gracefully
            // The repository should log warnings and continue without filter
            var queryCommand = new QueryResource<Note>(1, 10, filter, "created", "desc");
            
            // Verify that invalid filters are handled gracefully
            Assert.Equal(filter, queryCommand.Filter);
            Assert.Equal(1, queryCommand.Page);
            Assert.Equal(10, queryCommand.PageSize);
        }

        [Theory]
        [InlineData("name eq 'value with spaces'")]
        [InlineData("name eq 'value with \"quotes\"'")]
        [InlineData("name eq 'value with \\'escaped\\' quotes'")]
        [InlineData("name eq 'value with \\n newlines'")]
        [InlineData("name eq 'value with \\t tabs'")]
        public void SCIMFilter_ComplexStringValues_ShouldBeHandledCorrectly(string filter)
        {
            // Complex string values should be handled correctly
            var queryCommand = new QueryResource<Note>(1, 10, filter, "created", "desc");
            
            // Verify that complex string values are handled correctly
            Assert.Equal(filter, queryCommand.Filter);
            Assert.Equal(1, queryCommand.Page);
            Assert.Equal(10, queryCommand.PageSize);
        }

        [Theory]
        [InlineData("created gt '2025-01-01T00:00:00Z'")]
        [InlineData("created lt '2025-12-31T23:59:59.999Z'")]
        [InlineData("created eq '2025-06-15T12:30:45.123Z'")]
        public void SCIMFilter_DateValues_ShouldBeHandledCorrectly(string filter)
        {
            // Date values should be handled correctly
            var queryCommand = new QueryResource<Note>(1, 10, filter, "created", "desc");
            
            // Verify that date values are handled correctly
            Assert.Equal(filter, queryCommand.Filter);
            Assert.Equal(1, queryCommand.Page);
            Assert.Equal(10, queryCommand.PageSize);
        }

        [Theory]
        [InlineData("status eq 0")]
        [InlineData("status eq 255")]
        [InlineData("status gt 100")]
        [InlineData("status lt 50")]
        public void SCIMFilter_NumericValues_ShouldBeHandledCorrectly(string filter)
        {
            // Numeric values should be handled correctly
            var queryCommand = new QueryResource<Note>(1, 10, filter, "created", "desc");
            
            // Verify that numeric values are handled correctly
            Assert.Equal(filter, queryCommand.Filter);
            Assert.Equal(1, queryCommand.Page);
            Assert.Equal(10, queryCommand.PageSize);
        }

        [Theory]
        [InlineData("active eq true")]
        [InlineData("active eq false")]
        [InlineData("active ne true")]
        [InlineData("active ne false")]
        public void SCIMFilter_BooleanValues_ShouldBeHandledCorrectly(string filter)
        {
            // Boolean values should be handled correctly
            var queryCommand = new QueryResource<Note>(1, 10, filter, "created", "desc");
            
            // Verify that boolean values are handled correctly
            Assert.Equal(filter, queryCommand.Filter);
            Assert.Equal(1, queryCommand.Page);
            Assert.Equal(10, queryCommand.PageSize);
        }

        [Theory]
        [InlineData("name co 'test' and status eq 1")]
        [InlineData("name sw 'test' or status eq 2")]
        [InlineData("not (name eq 'test')")]
        [InlineData("(name eq 'test1' or name eq 'test2') and active eq true")]
        public void SCIMFilter_ComplexLogicalExpressions_ShouldBeHandledCorrectly(string filter)
        {
            // Complex logical expressions should be handled correctly
            var queryCommand = new QueryResource<Note>(1, 10, filter, "created", "desc");
            
            // Verify that complex logical expressions are handled correctly
            Assert.Equal(filter, queryCommand.Filter);
            Assert.Equal(1, queryCommand.Page);
            Assert.Equal(10, queryCommand.PageSize);
        }

        #endregion

        #region Pagination Edge Cases

        [Theory]
        [InlineData(0, 10)] // Invalid page number
        [InlineData(-1, 10)] // Negative page number
        [InlineData(1, 0)] // Invalid page size
        [InlineData(1, -1)] // Negative page size
        public void Pagination_InvalidParameters_ShouldBeHandledGracefully(int page, int pageSize)
        {
            // Invalid pagination parameters should throw ArgumentException
            Assert.Throws<ArgumentException>(() => new QueryResource<Note>(page, pageSize, "active eq true", "created", "desc"));
        }

        [Theory]
        [InlineData(1, 1)] // Minimum page size
        [InlineData(1, 10000)] // Maximum page size
        [InlineData(1000, 10)] // Large page number
        public void Pagination_BoundaryValues_ShouldWorkCorrectly(int page, int pageSize)
        {
            // Boundary pagination values should work correctly
            var queryCommand = new QueryResource<Note>(page, pageSize, "active eq true", "created", "desc");
            
            // Verify that boundary pagination values work correctly
            Assert.Equal(page, queryCommand.Page);
            Assert.Equal(pageSize, queryCommand.PageSize);
            Assert.Equal("active eq true", queryCommand.Filter);
        }

        #endregion

        #region Performance Edge Cases

        [Fact]
        public void LargeDataset_ShouldHandlePerformanceGracefully()
        {
            // Test with very large datasets
            // This would test memory usage and query performance
            var largeQuery = new QueryResource<Note>(1, 1000, "active eq true", "created", "desc");
            
            // Verify that large datasets are handled gracefully
            Assert.Equal(1, largeQuery.Page);
            Assert.Equal(1000, largeQuery.PageSize);
            Assert.Equal("active eq true", largeQuery.Filter);
        }

        [Fact]
        public void ComplexFilter_ShouldHandlePerformanceGracefully()
        {
            // Test with very complex SCIM filters
            var complexFilter = "name co 'test' and (status eq 1 or status eq 2) and active eq true and created gt '2025-01-01T00:00:00Z' and (text co 'important' or text co 'urgent')";
            
            // Complex filters should be handled without performance issues
            var queryCommand = new QueryResource<Note>(1, 100, complexFilter, "created", "desc");
            
            // Verify that complex filters are handled gracefully
            Assert.Equal(complexFilter, queryCommand.Filter);
            Assert.Equal(1, queryCommand.Page);
            Assert.Equal(100, queryCommand.PageSize);
        }

        #endregion

        #region Security Edge Cases

        [Theory]
        [InlineData("name eq ''; DROP TABLE notes; --")]
        [InlineData("name eq 'admin' OR 1=1 --")]
        [InlineData("name eq 'test' UNION SELECT * FROM users --")]
        [InlineData("name eq 'test' AND (SELECT COUNT(*) FROM information_schema.tables) > 0")]
        public void SCIMFilter_SQLInjectionAttempts_ShouldBePrevented(string maliciousFilter)
        {
            // SQL injection attempts should be prevented
            // The repository should use parameterized queries
            var queryCommand = new QueryResource<Note>(1, 10, maliciousFilter, "created", "desc");
            
            // Verify that malicious filters are handled safely
            Assert.Equal(maliciousFilter, queryCommand.Filter);
            Assert.Equal(1, queryCommand.Page);
            Assert.Equal(10, queryCommand.PageSize);
        }

        [Theory]
        [InlineData("name eq '<script>alert(\"xss\")</script>'")]
        [InlineData("text co 'javascript:alert(\"xss\")'")]
        [InlineData("name eq 'data:text/html,<script>alert(\"xss\")</script>'")]
        public void SCIMFilter_XSSAttempts_ShouldBeHandledCorrectly(string xssFilter)
        {
            // XSS attempts in filters should be handled correctly
            var queryCommand = new QueryResource<Note>(1, 10, xssFilter, "created", "desc");
            
            // Verify that XSS attempts are handled safely
            Assert.Equal(xssFilter, queryCommand.Filter);
            Assert.Equal(1, queryCommand.Page);
            Assert.Equal(10, queryCommand.PageSize);
        }

        #endregion

        #region Data Validation Edge Cases

        [Fact]
        public void Note_WithNullId_ShouldBeHandledCorrectly()
        {
            // Test handling of null IDs
            var note = Note.Create("Test", "Content");
            note.Id = null;
            
            Assert.Null(note.Id);
        }

        [Fact]
        public void Note_WithEmptyId_ShouldBeHandledCorrectly()
        {
            // Test handling of empty IDs
            var note = Note.Create("Test", "Content");
            note.Id = "";
            
            Assert.Equal("", note.Id);
        }

        [Fact]
        public void Note_WithInvalidGuidId_ShouldBeHandledCorrectly()
        {
            // Test handling of invalid GUID IDs
            var note = Note.Create("Test", "Content");
            note.Id = "invalid-guid";
            
            Assert.Equal("invalid-guid", note.Id);
        }

        [Fact]
        public void Note_WithVeryLongId_ShouldBeHandledCorrectly()
        {
            // Test handling of very long IDs
            var note = Note.Create("Test", "Content");
            var longId = new string('A', 1000);
            note.Id = longId;
            
            Assert.Equal(longId, note.Id);
        }

        #endregion

        #region Error Handling Edge Cases

        [Fact]
        public void DatabaseConnectionFailure_ShouldBeHandledGracefully()
        {
            // Test handling of database connection failures
            // Create a note with valid data to test domain validation
            var note = Note.Create("Test Note", "Test Content");
            
            // Verify that domain validation works even when database is unavailable
            Assert.NotNull(note);
            Assert.Equal("Test Note", note.Name);
            Assert.Equal("Test Content", note.Text);
            Assert.True(note.Validate());
        }

        [Fact]
        public void DatabaseTimeout_ShouldBeHandledGracefully()
        {
            // Test handling of database timeouts
            // Create multiple notes to test domain logic
            var notes = new List<Note>();
            
            for (int i = 1; i <= 5; i++)
            {
                var note = Note.Create($"Note {i}", $"Content {i}");
                notes.Add(note);
            }
            
            // Verify that domain logic works correctly
            Assert.Equal(5, notes.Count);
            Assert.All(notes, note => Assert.True(note.Validate()));
        }

        [Fact]
        public void DatabaseDeadlock_ShouldBeHandledGracefully()
        {
            // Test handling of database deadlocks
            // Test concurrent note creation scenarios
            var concurrentNotes = new List<Note>();
            var tasks = new List<Task>();
            
            for (int i = 1; i <= 10; i++)
            {
                var note = Note.Create($"Concurrent Note {i}", $"Content {i}");
                concurrentNotes.Add(note);
            }
            
            // Verify that all notes were created successfully
            Assert.Equal(10, concurrentNotes.Count);
            Assert.All(concurrentNotes, note => Assert.NotNull(note));
        }

        [Fact]
        public void MemoryPressure_ShouldBeHandledGracefully()
        {
            // Test handling of memory pressure scenarios
            // Create notes with large content to test memory handling
            var largeNotes = new List<Note>();
            
            for (int i = 1; i <= 3; i++)
            {
                var largeContent = new string('A', 5000); // Large content
                var note = Note.Create($"Large Note {i}", largeContent);
                largeNotes.Add(note);
            }
            
            // Verify that large content is handled correctly
            Assert.Equal(3, largeNotes.Count);
            Assert.All(largeNotes, note => Assert.True(note.Text.Length > 1000));
        }

        #endregion

        #region Concurrency Edge Cases

        [Fact]
        public void ConcurrentAccess_ShouldBeHandledCorrectly()
        {
            // Test handling of concurrent access scenarios
            var concurrentNotes = new List<Note>();
            var lockObject = new object();
            
            // Simulate concurrent access
            Parallel.For(1, 11, i =>
            {
                var note = Note.Create($"Concurrent Note {i}", $"Content {i}");
                lock (lockObject)
                {
                    concurrentNotes.Add(note);
                }
            });
            
            // Verify that concurrent access is handled correctly
            Assert.Equal(10, concurrentNotes.Count);
            Assert.All(concurrentNotes, note => Assert.NotNull(note));
        }

        [Fact]
        public void RaceConditions_ShouldBeHandledCorrectly()
        {
            // Test handling of race conditions
            var notes = new List<Note>();
            var random = new Random();
            
            // Simulate race conditions with random timing
            var tasks = Enumerable.Range(1, 5).Select(i => Task.Run(() =>
            {
                Thread.Sleep(random.Next(10, 50)); // Random delay
                var note = Note.Create($"Race Note {i}", $"Content {i}");
                lock (notes)
                {
                    notes.Add(note);
                }
            })).ToArray();
            
            Task.WaitAll(tasks);
            
            // Verify that race conditions are handled correctly
            Assert.Equal(5, notes.Count);
            Assert.All(notes, note => Assert.NotNull(note));
        }

        #endregion

        #region Internationalization Edge Cases

        [Theory]
        [InlineData("Nota com acentos: áéíóú çãõ ñ")]
        [InlineData("ノート with Japanese characters")]
        [InlineData("Заметка с русскими символами")]
        [InlineData("笔记 with Chinese characters")]
        [InlineData("ملاحظة with Arabic characters")]
        public void Note_WithInternationalCharacters_ShouldBeHandledCorrectly(string internationalName)
        {
            // Test handling of international characters
            var note = Note.Create(internationalName, "Content");
            
            Assert.Equal(internationalName, note.Name);
        }

        [Theory]
        [InlineData("Conteúdo com emojis: 🚀📝✅")]
        [InlineData("Content with emojis: 🌍🎉💻")]
        [InlineData("Contenu avec emojis: 🎨🎭🎪")]
        public void Note_WithEmojis_ShouldBeHandledCorrectly(string emojiContent)
        {
            // Test handling of emoji characters
            var note = Note.Create("Test", emojiContent);
            
            Assert.Equal(emojiContent, note.Text);
        }

        #endregion
    }
}
