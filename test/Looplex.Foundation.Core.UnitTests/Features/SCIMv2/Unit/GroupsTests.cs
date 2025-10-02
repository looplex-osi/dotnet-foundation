using System.Security.Claims;

using Looplex.Foundation.Core.Ports;
using Looplex.Foundation.Core.SCIMv2.Commands;
using Looplex.Foundation.Core.SCIMv2.Entities;
using Looplex.Foundation.Core.SCIMv2.Modules;
using Looplex.Foundation.Core.SCIMv2.Queries;
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
      _mockRepository.QueryAsync(1, 10, "filter", cancellationToken)
        .Returns((expectedGroups, expectedTotal));

      // Act
      var response = await _groups.Query(1, 10, "filter", "name", "asc", cancellationToken);

      // Assert
      Assert.IsNotNull(response);
      Assert.AreEqual(1, response.TotalResults);
      Assert.AreEqual(1, response.Resources.Count);
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
      var result = await _groups.Create(group, cancellationToken);

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
      var result = await _groups.Retrieve(Guid.Parse(id), cancellationToken);

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
      var result = await _groups.Delete(Guid.Parse(id), cancellationToken);

      // Assert
      Assert.IsTrue(result);
    }
  }
}
