using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesOrder.API.Controllers;
using SalesOrder.Application.DTOs;
using SalesOrder.Application.Services;
using Xunit;

namespace SalesOrder.Tests
{
    public class SalesOrdersControllerTests
    {
        private readonly Mock<ISalesOrderApplicationService> _serviceMock;
        private readonly SalesOrdersController _controller;
        private readonly InternalSalesOrdersController _internalController;

        public SalesOrdersControllerTests()
        {
            _serviceMock = new Mock<ISalesOrderApplicationService>();
            _controller = new SalesOrdersController(_serviceMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "cust-101")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _internalController = new InternalSalesOrdersController(_serviceMock.Object);
        }

        [Fact]
        public async Task Create_ValidRequest_ReturnsCreatedAtAction()
        {
            var orderId = Guid.NewGuid();
            var response = new SalesOrderResponse
            {
                Id = orderId,
                OrderNumber = "SO-2026-1234",
                CustomerId = "cust-101",
                GrandTotal = 50000m
            };

            _serviceMock.Setup(s => s.CreateAsync("cust-101", It.IsAny<CreateSalesOrderRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var result = await _controller.Create(new CreateSalesOrderRequest(), CancellationToken.None);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var order = Assert.IsType<SalesOrderResponse>(createdResult.Value);
            Assert.Equal(orderId, order.Id);
        }

        [Fact]
        public async Task Get_ExistingOrder_ReturnsOkResult()
        {
            var orderId = Guid.NewGuid();
            var response = new SalesOrderResponse
            {
                Id = orderId,
                OrderNumber = "SO-2026-1234"
            };

            _serviceMock.Setup(s => s.GetAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var result = await _controller.Get(orderId, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var order = Assert.IsType<SalesOrderResponse>(okResult.Value);
            Assert.Equal(orderId, order.Id);
        }

        [Fact]
        public async Task Get_NonExistingOrder_ReturnsNotFound()
        {
            var orderId = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SalesOrderResponse?)null);

            var result = await _controller.Get(orderId, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Cancel_ValidRequest_ReturnsNoContent()
        {
            var orderId = Guid.NewGuid();
            _serviceMock.Setup(s => s.CancelAsync(orderId, "cust-101", "Changed order", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Cancel(orderId, new CancelSalesOrderRequest { Reason = "Changed order" }, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
            _serviceMock.Verify(s => s.CancelAsync(orderId, "cust-101", "Changed order", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InternalController_GetOrder_Existing_ReturnsOk()
        {
            var orderId = Guid.NewGuid();
            var response = new SalesOrderResponse { Id = orderId, OrderNumber = "SO-INTERNAL-01" };

            _serviceMock.Setup(s => s.GetByPublicIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var result = await _internalController.GetOrder(orderId, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var order = Assert.IsType<SalesOrderResponse>(okResult.Value);
            Assert.Equal("SO-INTERNAL-01", order.OrderNumber);
        }

        [Fact]
        public async Task InternalController_GetOrder_NotFound_ReturnsNotFound()
        {
            var orderId = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetByPublicIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SalesOrderResponse?)null);

            var result = await _internalController.GetOrder(orderId, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
