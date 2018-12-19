using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyBlog2.Areas.Identity.Data;
using TinyBlog2.Models;

[assembly: HostingStartup(typeof(TinyBlog2.Areas.Identity.IdentityHostingStartup))]
namespace TinyBlog2.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<TinyBlog2Context>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("TinyBlog2ContextConnection")));

                services.AddDefaultIdentity<TinyBlog2User>()
                    .AddEntityFrameworkStores<TinyBlog2Context>();
            });
        }
    }
}