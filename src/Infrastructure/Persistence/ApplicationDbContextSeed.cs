﻿//<auto-generated />
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DrWhistle.Application.Common.Interfaces;
using DrWhistle.Domain;
using DrWhistle.Domain.Entities;
using DrWhistle.Infrastructure.Identity;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DrWhistle.Infrastructure.Persistence
{
    public static class ApplicationDbContextSeed
    {
        /// <summary>
        /// Updates the database.
        /// </summary>
        /// <param name="scopeFactory">Service scope factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="seed">if set to <c>true</c> [seed].</param>
        public static async Task UpdateDatabases(IServiceScopeFactory scopeFactory, ILogger logger, bool seed = true)
        {
            using var serviceScope = scopeFactory.CreateScope();
            
            var tenantService = serviceScope.ServiceProvider.GetService<ITenantService<Tenant>>();

            var tenants = await tenantService.GetAllAsync();

            foreach (var tenant in tenants)
            {
                await UpdateTenant(scopeFactory, tenant, logger, seed);
            }
        }

        public static async Task UpdateTenant(IServiceScopeFactory scopeFactory, Tenant tenant, Microsoft.Extensions.Logging.ILogger logger, bool seed = true)
        {
            using var serviceScope = scopeFactory.CreateScope();

            var serviceProvider = serviceScope.ServiceProvider;

            var tenantResolver = serviceProvider.GetService<IMultiTenantContextAccessor<Tenant>>();

            tenantResolver.MultiTenantContext = new MultiTenantContext<Tenant>()
            {
                TenantInfo = tenant
            };

            using var tenantContext = serviceProvider.GetService<ApplicationDbContext>();

            if (tenantContext.Database.IsInMemory())
            {
                logger.LogInformation("Using InMemory Database.");
                tenantContext.Database.EnsureCreated();
            }
            else
            {
                logger.LogInformation("Executing Database Migrations.");

                tenantContext.Database.Migrate();
            }

            if (seed)
            {
                logger.LogInformation("Executing Database Seeder.");

                await ApplicationDbContextSeed.SeedRolesAsync(serviceProvider);

                await ApplicationDbContextSeed.SeedSampleDataAsync(tenantContext);

                await ApplicationDbContextSeed.SeedDefaultUserAsync(serviceProvider);
            }

            await Task.CompletedTask;
        }

        public static async Task SeedDefaultUserAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var adminUsername = $"admin@test.com";

            var adminPassword = "Password123!";

            var admin = await userManager.FindByEmailAsync(adminUsername);

            if (admin == null)
            {
                var identityUser = new ApplicationUser(adminUsername)
                {
                    Email = adminUsername,
                    EmailConfirmed = true,
                    TwoFactorEnabled = false
                };

                var res = await userManager.CreateAsync(identityUser, adminPassword);

                if (!res.Succeeded)
                {
                    var sb = new StringBuilder();

                    foreach (var err in res.Errors)
                    {
                        sb.AppendLine($"{err.Code} : {err.Description}");
                    }

                    throw new ApplicationException(sb.ToString());
                }

                admin = identityUser;
            }
             
            await userManager.AddToRoleAsync(admin, Constants.AdministratorRole);
        }

        public static async Task SeedSampleDataAsync(IApplicationDbContext context)
        {
            await Task.CompletedTask;
        }

        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var systemRoles = new HashSet<string>(Constants.SystemRoles, StringComparer.InvariantCultureIgnoreCase);
        
            var identityService = serviceProvider.GetService<IIdentityService>();

            var tenantRoles = new HashSet<string>((await identityService.GetRolesAsync()).Select(r => r.Name), StringComparer.InvariantCultureIgnoreCase);

            if (!systemRoles.IsSubsetOf(tenantRoles))
            {
                var tempRoles = new HashSet<string>(systemRoles, StringComparer.InvariantCultureIgnoreCase);
                tempRoles.ExceptWith(tenantRoles);

                foreach (var role in tempRoles)
                {
                    await identityService.CreateRoleAsync(role);
                }
            }
        }
    }
}