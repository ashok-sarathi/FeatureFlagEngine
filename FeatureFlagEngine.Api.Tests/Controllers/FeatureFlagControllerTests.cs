using FeatureFlagEngine.Api.Controllers;
using FeatureFlagEngine.Application.Interfaces.Services;
using FeatureFlagEngine.Domain.Dtos.FeatureFlag;
using FeatureFlagEngine.Domain.Dtos.FeatureOverride;
using FeatureFlagEngine.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Api.Tests.Controllers
{
    public class FeatureFlagControllerTests
    {
        private readonly Mock<IFeatureFlagService> _serviceMock;
        private readonly FeatureFlagController _controller;

        public FeatureFlagControllerTests()
        {
            _serviceMock = new Mock<IFeatureFlagService>();
            _controller = new FeatureFlagController(_serviceMock.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk_WithFeatures()
        {
            var list = new List<FeatureFlagDto> { new() { Id = Guid.NewGuid(), Key = "f1" } };
            _serviceMock.Setup(s => s.GetAllAsync(true)).ReturnsAsync(list);

            var result = await _controller.GetAll(true);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(list);
        }

        [Fact]
        public async Task GetAll_WhenEmpty_ShouldReturnOkWithEmptyList()
        {
            _serviceMock.Setup(s => s.GetAllAsync(false)).ReturnsAsync(new List<FeatureFlagDto>());

            var result = await _controller.GetAll(false);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<IEnumerable<FeatureFlagDto>>().Which.Should().BeEmpty();
        }

        [Fact]
        public async Task GetById_WhenFound_ShouldReturnOk()
        {
            var id = Guid.NewGuid();
            var dto = new FeatureFlagDto { Id = id, Key = "f1" };
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(dto);

            var result = await _controller.GetById(id);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(dto);
        }

        [Fact]
        public async Task GetById_WhenNotFound_ShouldReturnNotFound()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FeatureFlagDto?)null);

            var result = await _controller.GetById(Guid.NewGuid());

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Create_ShouldReturnCreatedAtAction()
        {
            var dto = new FeatureFlagDto { Id = Guid.NewGuid(), Key = "new" };
            _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync(dto);

            var result = await _controller.Create(dto);

            var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(FeatureFlagController.GetById));
            created.Value.Should().Be(dto);
        }

        [Fact]
        public async Task Create_WhenServiceReturnsNull_ShouldThrow()
        {
            var dto = new FeatureFlagDto { Id = Guid.NewGuid(), Key = "f1" };
            _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync((FeatureFlagDto?)null);

            Func<Task> act = async () => await _controller.Create(dto);
            await act.Should().ThrowAsync<NullReferenceException>();
        }

        [Fact]
        public async Task Create_WithNullDto_ShouldThrow()
        {
            Func<Task> act = async () => await _controller.Create(null!);
            await act.Should().ThrowAsync<NullReferenceException>();
        }

        [Fact]
        public async Task Update_WhenIdMismatch_ShouldReturnBadRequest()
        {
            var dto = new FeatureFlagDto { Id = Guid.NewGuid() };

            var result = await _controller.Update(Guid.NewGuid(), dto);

            result.Should().BeOfType<BadRequestObjectResult>();
            _serviceMock.Verify(s => s.UpdateAsync(It.IsAny<FeatureFlagDto>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenValid_ShouldReturnNoContent()
        {
            var id = Guid.NewGuid();
            var dto = new FeatureFlagDto { Id = id };

            var result = await _controller.Update(id, dto);

            result.Should().BeOfType<NoContentResult>();
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Update_WhenServiceThrows_ShouldBubbleException()
        {
            var id = Guid.NewGuid();
            var dto = new FeatureFlagDto { Id = id };

            _serviceMock.Setup(s => s.UpdateAsync(dto)).ThrowsAsync(new Exception("DB error"));

            Func<Task> act = async () => await _controller.Update(id, dto);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
        }

        [Fact]
        public async Task Delete_ShouldReturnNoContent()
        {
            var id = Guid.NewGuid();

            var result = await _controller.Delete(id);

            result.Should().BeOfType<NoContentResult>();
            _serviceMock.Verify(s => s.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task Delete_WhenServiceThrows_ShouldBubbleException()
        {
            _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ThrowsAsync(new KeyNotFoundException());

            Func<Task> act = async () => await _controller.Delete(Guid.NewGuid());
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task UpdateGlobalState_ShouldReturnNoContent()
        {
            var result = await _controller.UpdateGlobalState("f1", true);

            result.Should().BeOfType<NoContentResult>();
            _serviceMock.Verify(s => s.UpdateGlobalStateAsync("f1", true), Times.Once);
        }

        [Fact]
        public async Task AddOverride_ShouldReturnNoContent()
        {
            var dto = new FeatureOverrideDto { TargetId = "u1", OverrideType = FeatureOverrideType.User };

            var result = await _controller.AddOverride("f1", dto);

            result.Should().BeOfType<NoContentResult>();
            _serviceMock.Verify(s => s.AddOverrideAsync("f1", dto), Times.Once);
        }

        [Fact]
        public async Task RemoveOverride_ShouldReturnNoContent()
        {
            var result = await _controller.RemoveOverride("f1", FeatureOverrideType.User, "u1");

            result.Should().BeOfType<NoContentResult>();
            _serviceMock.Verify(s => s.RemoveOverrideAsync("f1", FeatureOverrideType.User, "u1"), Times.Once);
        }

        [Fact]
        public async Task RemoveOverride_WithInvalidEnum_ShouldStillCallService()
        {
            var invalidEnum = (FeatureOverrideType)999;

            await _controller.RemoveOverride("f1", invalidEnum, "u1");

            _serviceMock.Verify(s => s.RemoveOverrideAsync("f1", invalidEnum, "u1"), Times.Once);
        }

        [Fact]
        public async Task Evaluate_WhenCacheHit_ShouldReturnOkAndSetHeader()
        {
            _serviceMock.Setup(s => s.EvaluateAsync("f1", "u1", "g1", "r1")).ReturnsAsync((true, true));

            var result = await _controller.Evaluate("f1", "u1", "g1", "r1");

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(true);
            _controller.Response.Headers["X-Cache"].ToString().Should().Be("HIT");
        }

        [Fact]
        public async Task Evaluate_WhenCacheMiss_ShouldReturnOkAndSetHeader()
        {
            _serviceMock.Setup(s => s.EvaluateAsync("f1", null, null, null)).ReturnsAsync((false, false));

            var result = await _controller.Evaluate("f1", null, null, null);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(false);
            _controller.Response.Headers["X-Cache"].ToString().Should().Be("MISS");
        }

        [Fact]
        public async Task Evaluate_ShouldSetOnlyOneCacheHeader()
        {
            _serviceMock.Setup(s => s.EvaluateAsync("f1", null, null, null)).ReturnsAsync((true, true));

            await _controller.Evaluate("f1", null, null, null);

            _controller.Response.Headers["X-Cache"].Count.Should().Be(1);
        }
    }
}
