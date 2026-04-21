global using System.ComponentModel.DataAnnotations;
global using System.Text.Json;
global using System.Text;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.DependencyInjection;
global using HotChocolate;
global using HotChocolate.Subscriptions;
global using Stripe;
global using Stripe.Checkout;
global using EnterpriseAgentOs.Domain.Models;
global using EnterpriseAgentOs.Domain.Primitives;
global using EnterpriseAgentOs.Domain.Interfaces;
// Note: EaosDbContext / Infrastructure.Persistence intentionally NOT imported.
// All data access goes through Domain repository interfaces.
global using EnterpriseAgentOs.Infrastructure.Configuration;
global using EnterpriseAgentOs.Infrastructure.Security;
global using EnterpriseAgentOs.Infrastructure.Adapters;
global using EnterpriseAgentOs.Infrastructure.Persistence;
global using Microsoft.EntityFrameworkCore;
global using System.Net.Http.Json;
