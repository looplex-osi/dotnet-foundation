using System.Security.Claims;

using Looplex.Foundation.Ports;
using Looplex.SCIMv2.Commands;
using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2;
using Looplex.SCIMv2.Queries;
using Looplex.OpenForExtension.Abstractions.Plugins;

using MediatR;

using Microsoft.AspNetCore.Http;

using NSubstitute;

namespace Looplex.Foundation.Core.UnitTests.SCIMv2.Entities
{
  [TestClass]
  public class GroupsTests
  {
    private GroupService _groups = null!;
    private IResourceRepository<Group> _mockRepository = null!;

    [TestInitialize]
    public void Setup()
    {
      // Create mock repository for GroupService
      _mockRepository = Substitute.For<IResourceRepository<Group>>();
      _groups = new GroupService(_mockRepository);
    }

    [TestMethod]
    public async Task Query_ShouldReturnListResponse()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedGroups = new List<Group> { new Group() };
      int expectedTotal = 1;

      // Configure mock repository to return expected data
      _mockRepository.QueryAsync(1, 10, "filter", "name", "ascending", "", "", cancellationToken)
        .Returns((expectedGroups, expectedTotal));

      // Act
      var (resources, totalCount) = await _groups.QueryAsync(1, 10, "filter", "name", "ascending", "", "", cancellationToken);

      // Assert
      Assert.IsNotNull(resources);
      Assert.AreEqual(1, totalCount);
      Assert.AreEqual(1, resources.Count);
    }

    [TestMethod]
    public async Task Create_ShouldReturnGuid()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var group = new Group();
      var expectedGroup = new Group { Id = Guid.NewGuid().ToString() };

      // Configure mock repository to return expected group
      _mockRepository.CreateAsync(group, cancellationToken)
        .Returns(expectedGroup);

      // Act
      var result = await _groups.CreateAsync(group, cancellationToken);

      // Assert
      Assert.AreEqual(Guid.Parse(expectedGroup.Id), result);
    }

    [TestMethod]
    public async Task Retrieve_ShouldReturnGroup()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedGroup = new Group();
      var id = Guid.NewGuid().ToString();

      // Configure mock repository to return expected group
      _mockRepository.GetByIdAsync(id, cancellationToken)
        .Returns(expectedGroup);

      // Act
      var result = await _groups.RetrieveAsync(Guid.Parse(id), cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(expectedGroup, result);
    }


    [TestMethod]
    public async Task Delete_ShouldReturnTrue_WhenRowsAffected()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var id = Guid.NewGuid().ToString();

      // Configure mock repository to return success
      _mockRepository.DeleteAsync(id, cancellationToken)
        .Returns(true);

      // Act
      var result = await _groups.DeleteAsync(Guid.Parse(id), cancellationToken);

      // Assert
      Assert.IsTrue(result);
    }
  }
}
