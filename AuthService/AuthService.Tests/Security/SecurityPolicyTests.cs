using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using VehicleEcom.Security.Authorization;
using VehicleEcom.Security.Constants;
using VehicleEcom.Security.UserContext;
using Xunit;

namespace AuthService.Tests.Security
{
    public class SecurityPolicyTests
    {
        [Fact]
        public async Task PermissionAuthorizationHandler_DirectPermissionClaim_Succeeds()
        {
            var handler = new PermissionAuthorizationHandler();
            var requirement = new PermissionAuthorizationRequirement(VehiclePermissions.OrdersCreate);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(VehicleClaimTypes.Subject, "user-1"),
                new Claim(VehicleClaimTypes.Permission, VehiclePermissions.OrdersCreate)
            }, "Bearer"));

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task PermissionAuthorizationHandler_RoleFallback_CustomerCanCreateOrders()
        {
            var handler = new PermissionAuthorizationHandler();
            var requirement = new PermissionAuthorizationRequirement(VehiclePermissions.OrdersCreate);

            // User has Customer role (no explicit permission claims)
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(VehicleClaimTypes.Subject, "cust-1"),
                new Claim(ClaimTypes.Role, VehicleRoles.Customer)
            }, "Bearer"));

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task PermissionAuthorizationHandler_RoleFallback_CustomerCannotManageDealers()
        {
            var handler = new PermissionAuthorizationHandler();
            var requirement = new PermissionAuthorizationRequirement(VehiclePermissions.DealersManage);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(VehicleClaimTypes.Subject, "cust-1"),
                new Claim(ClaimTypes.Role, VehicleRoles.Customer)
            }, "Bearer"));

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            await handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task PermissionAuthorizationHandler_Admin_SatisfiesAllPermissions()
        {
            var handler = new PermissionAuthorizationHandler();
            var requirement = new PermissionAuthorizationRequirement("any.custom.sensitive.permission");

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(VehicleClaimTypes.Subject, "admin-1"),
                new Claim(ClaimTypes.Role, VehicleRoles.Admin)
            }, "Bearer"));

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task PermissionAuthorizationHandler_ServiceAccountInternalScope_Succeeds()
        {
            var handler = new PermissionAuthorizationHandler();
            var requirement = new PermissionAuthorizationRequirement(VehiclePermissions.OrdersManage);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(VehicleClaimTypes.Subject, "salesorder-service"),
                new Claim("scope", VehicleScopes.InternalAll)
            }, "Bearer"));

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public void CurrentUserService_ExtractsRolesAndPermissionsCorrectly()
        {
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-456"),
                new Claim(ClaimTypes.Email, "customer@example.com"),
                new Claim(ClaimTypes.Role, VehicleRoles.Customer),
                new Claim(VehicleClaimTypes.Permission, VehiclePermissions.OrdersCreate),
                new Claim(VehicleClaimTypes.ClientId, "vehicle-mobile-app")
            }, "Bearer");

            httpContext.User = new ClaimsPrincipal(identity);

            var accessorMock = new Mock<IHttpContextAccessor>();
            accessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            var currentUserService = new CurrentUserService(accessorMock.Object);

            Assert.True(currentUserService.IsAuthenticated);
            Assert.Equal("user-456", currentUserService.UserId);
            Assert.Equal("customer@example.com", currentUserService.Email);
            Assert.Equal("vehicle-mobile-app", currentUserService.ClientId);
            Assert.Contains(VehicleRoles.Customer, currentUserService.Roles);
            Assert.True(currentUserService.IsInRole(VehicleRoles.Customer));
            Assert.True(currentUserService.HasPermission(VehiclePermissions.OrdersCreate));
            Assert.True(currentUserService.HasPermission(VehiclePermissions.OrdersRead)); // from role mapping
        }

        [Fact]
        public void RolePermissionDefaults_ContainsCorePlatformPermissions()
        {
            var customerPerms = RolePermissionDefaults.GetPermissionsForRole(VehicleRoles.Customer);
            Assert.Contains(VehiclePermissions.OrdersCreate, customerPerms);
            Assert.Contains(VehiclePermissions.OrdersRead, customerPerms);
            Assert.Contains(VehiclePermissions.OrdersCancel, customerPerms);

            var dealerPerms = RolePermissionDefaults.GetPermissionsForRole(VehicleRoles.Dealer);
            Assert.Contains(VehiclePermissions.InventoryManage, dealerPerms);
            Assert.Contains(VehiclePermissions.InventoryRead, dealerPerms);

            var adminPerms = RolePermissionDefaults.GetPermissionsForRole(VehicleRoles.Admin);
            Assert.Equal(VehiclePermissions.All.Count, adminPerms.Count);
        }
    }
}

